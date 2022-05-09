using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Temporal.WorkflowClient
{
    public partial record TemporalClientConfiguration
    {
        /// <summary>
        /// <para>Different version of .NET use different gRPC libraries (Net Fx uses gRPC Core C-Lib, and Net Core and Net modern use
        /// a managed gRPC.Net implementation). Unfortunately, the hadling of certificates is different in those libraries.
        /// Among other things, gRPC Core C-Lib does not natively support <c>X509Certificate2</c> and requires PEM formatted data.
        /// Conversely, <c>X509Certificate2</c> is de-facto standard for Windows / .NET users. Unfortunetely, in some scenarios it
        /// is not easy to turn key from a <c>X509Certificate2</c> into a format accepted by the gRPC Core C-Lib.<br/>
        /// In the long term, and if users request, we will find a way to work around this and make it convenient. In the interim,
        /// however, we will simply require PEM-formatted input on classic Net-Fx. On Net Core, we will support both, PEM and
        /// <c>X509Certificate2</c>.</para>
        /// <para>This type (<c>TemporalClientConfiguration.TlsCertificate</c>) is an abstraction of a configured certificate / key.
        /// It can be created from PEM, a <c>X509Certificate2</c>, or both depending on the platform.</para>
        /// </summary>
        public record TlsCertificate(string PublicCertPemData,
                                     string PrivateKeyPemData
#if NETCOREAPP3_1_OR_GREATER
                                     , X509Certificate2 X509Cert
#endif
                                    )
        {
            internal string PublicCertPemData { get; init; } = PublicCertPemData;
            internal string PrivateKeyPemData { get; init; } = PrivateKeyPemData;

#if NETCOREAPP3_1_OR_GREATER
            internal X509Certificate2 X509Cert { get; init; } = X509Cert;
#else
            private X509Certificate2 X509Cert { get { return null; } }
#endif

            public bool HasPemData { get { return (PublicCertPemData != null || PrivateKeyPemData != null); } }
            public bool HasX509Cert { get { return (X509Cert != null); } }

#if NETCOREAPP3_1_OR_GREATER
            public TlsCertificate(string publicCertPemData, string privateKeyPemData)
                : this(publicCertPemData, privateKeyPemData, X509Cert: null)
            {
            }

            public TlsCertificate(X509Certificate2 x509Cert)
                : this(PublicCertPemData: null, PrivateKeyPemData: null, x509Cert)
            {
            }
#endif

            public static TlsCertificate FromPemFile(string publicCertPemFilePath)
            {
                return FromPemFile(publicCertPemFilePath, privateKeyPemFilePath: null);
            }

            public static TlsCertificate FromPemFile(string publicCertPemFilePath, string privateKeyPemFilePath)
            {
                string publicCertPemData = String.IsNullOrWhiteSpace(publicCertPemFilePath)
                                            ? null
                                            : File.ReadAllText(publicCertPemFilePath);

                string privateKeyPemData = String.IsNullOrWhiteSpace(privateKeyPemFilePath)
                                            ? null
                                            : File.ReadAllText(privateKeyPemFilePath);

                return FromPemData(publicCertPemData, privateKeyPemData);
            }

            public static TlsCertificate FromPemData(string publicCertPemData)
            {
                return FromPemData(publicCertPemData, privateKeyPemData: null);
            }

            public static TlsCertificate FromPemData(string publicCertPemData, string privateKeyPemData)
            {
                if (publicCertPemData == null && privateKeyPemData == null)
                {
                    throw new ArgumentException("No PEM Data provided.");
                }

#if NETCOREAPP3_1_OR_GREATER
                X509Certificate2 x509Cert = Temporal.WorkflowClient.X509Cert.CreateFromPemData(publicCertPemData, privateKeyPemData);
                return new TemporalClientConfiguration.TlsCertificate(publicCertPemData, privateKeyPemData, x509Cert);
#else
                return new TemporalClientConfiguration.TlsCertificate(publicCertPemData, privateKeyPemData);
#endif
            }

#if NETCOREAPP3_1_OR_GREATER
            public static TlsCertificate FromX509Cert(X509Certificate2 x509Cert)
            {
                Temporal.Util.Validate.NotNull(x509Cert);

                return new TemporalClientConfiguration.TlsCertificate(x509Cert);
            }
#endif

            internal static bool AreEquivalent(TemporalClientConfiguration.TlsCertificate cert1, TemporalClientConfiguration.TlsCertificate cert2)
            {
                if (Object.ReferenceEquals(cert1, cert2))
                {
                    return true;
                }

                if (cert1 == null || cert2 == null || cert1.HasPemData != cert2.HasPemData || cert1.HasX509Cert != cert2.HasX509Cert)
                {
                    return false;
                }

                if (cert1.HasPemData)
                {
                    if (!Object.ReferenceEquals(cert1.PublicCertPemData, cert2.PublicCertPemData)
                        && !cert1.PublicCertPemData.Equals(cert2.PublicCertPemData, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (!Object.ReferenceEquals(cert1.PrivateKeyPemData, cert2.PrivateKeyPemData)
                        && !cert1.PrivateKeyPemData.Equals(cert2.PrivateKeyPemData, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                if (cert1.HasX509Cert)
                {
                    // When we apply this to the TLS Cert of the for CA, the certs technically do not need to BE the same;
                    // they just need to be IN the same (valid) cert chain (in the right order) in order to be considered
                    // equivalent. However, for now a direct comparison is good enough. The only drawback is that we might
                    // end up having 2 equivalent channels where one would have been enough, but that is likely rare and
                    // certainly benign beyond the respective resource use.
                    if (!Temporal.WorkflowClient.X509Cert.AreEqual(cert1.X509Cert, cert2.X509Cert))
                    {
                        return false;
                    }
                }

                return true;
            }

            internal static void Validate(TemporalClientConfiguration.TlsCertificate configTlsCertificate)
            {
                Temporal.Util.Validate.NotNull(configTlsCertificate);

                if (!configTlsCertificate.HasPemData && !configTlsCertificate.HasX509Cert)
                {
                    throw new ArgumentException($"A `{configTlsCertificate.GetType().Name}` must encapsulate either PEM Data,"
                                              + $" or a X509 Certificate, or both. However, the specified instance encapsulates none.");
                }
            }
        }
    }
}
