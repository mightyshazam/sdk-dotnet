using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Temporal.WorkflowClient
{
    public partial record TemporalClientConfiguration
    {
        public record Connection(string ServerHost,
                                 int ServerPort,
                                 bool IsTlsEnabled,
                                 TemporalClientConfiguration.TlsCertificate ClientIdentityCert,
                                 bool SkipServerCertValidation,
                                 TemporalClientConfiguration.TlsCertificate ServerCertAuthority)
        {
            #region Static APIs

            internal static class Defaults
            {
                public const string LocalHost = "127.0.0.1";  // Do not use "localhost" to avoid the IPv4 resolution wait
                public const string CloudHostTemplate = "{0}.tmprl.cloud";
                public const int ServerPort = 7233;

                public static string CloudHost(string @namespace)
                {
                    Temporal.Util.Validate.NotNullOrWhitespace(@namespace);
                    return String.Format(CloudHostTemplate, @namespace);
                }
            }

            public static TemporalClientConfiguration.Connection TlsDisabled(string serverHost)
            {
                return TlsDisabled(serverHost, Defaults.ServerPort);
            }

            public static TemporalClientConfiguration.Connection TlsDisabled(string serverHost, int serverPort)
            {
                return new TemporalClientConfiguration.Connection(serverHost,
                                                                  serverPort,
                                                                  isTlsEnabled: false);
            }

            public static TemporalClientConfiguration.Connection TlsEnabled(string serverHost)
            {
                return TlsEnabled(serverHost, Defaults.ServerPort);
            }

            public static TemporalClientConfiguration.Connection TlsEnabled(string serverHost, int serverPort)
            {
                return new TemporalClientConfiguration.Connection(serverHost,
                                                                  serverPort,
                                                                  isTlsEnabled: true);
            }

            /// <summary>
            /// </summary>
            /// <remarks>
            /// This overload is only available on Net Core and Net 5+. See <see cref="TemporalClientConfiguration.TlsCertificate"/>
            /// for details on this topic.
            /// </remarks>
            public static TemporalClientConfiguration.Connection ForTemporalCloud(string @namespace,
                                                                                  string clientCertPemFilePath,
                                                                                  string clientKeyPemFilePath)
            {
                return TemporalClientConfiguration.Connection.TlsEnabled(Defaults.CloudHost(@namespace),
                                                                         Defaults.ServerPort) with
                {
                    ClientIdentityCert = TemporalClientConfiguration.TlsCertificate.FromPemFile(clientCertPemFilePath, clientKeyPemFilePath)
                };
            }

#if NETCOREAPP3_1_OR_GREATER
            /// <summary>
            /// </summary>
            /// <remarks>
            /// This overload is only available on Net Core and Net 5+. See <see cref="TemporalClientConfiguration.TlsCertificate"/>
            /// for details on this topic.
            /// </remarks>
            public static TemporalClientConfiguration.Connection ForTemporalCloud(string @namespace, X509Certificate2 clientCert)
            {
                return TemporalClientConfiguration.Connection.TlsEnabled(Defaults.CloudHost(@namespace),
                                                                         Defaults.ServerPort) with
                {
                    ClientIdentityCert = TemporalClientConfiguration.TlsCertificate.FromX509Cert(clientCert)
                };
            }
#endif

            public static void Validate(TemporalClientConfiguration.Connection configServiceConnection)
            {
                Temporal.Util.Validate.NotNull(configServiceConnection);
                Temporal.Util.Validate.NotNullOrWhitespace(configServiceConnection.ServerHost);

                if (configServiceConnection.ServerPort <= 0)
                {
                    throw new ArgumentException($"{nameof(configServiceConnection)}.{nameof(configServiceConnection.ServerPort)} must"
                                              + $" be a posivite value, but {configServiceConnection.ServerPort} was specified.");
                }

                if (configServiceConnection.ClientIdentityCert != null)
                {
                    TemporalClientConfiguration.TlsCertificate.Validate(configServiceConnection.ClientIdentityCert);
                }

                if (configServiceConnection.ServerCertAuthority != null)
                {
                    TemporalClientConfiguration.TlsCertificate.Validate(configServiceConnection.ServerCertAuthority);
                }
            }

            #endregion Static APIs

            public Connection()
                : this(Defaults.LocalHost,
                       Defaults.ServerPort,
                       IsTlsEnabled: false,
                       ClientIdentityCert: null,
                       SkipServerCertValidation: false,
                       ServerCertAuthority: null)
            {
            }

            private Connection(string serverHost, int serverPort, bool isTlsEnabled)
                : this(serverHost,
                       serverPort,
                       isTlsEnabled,
                       ClientIdentityCert: null,
                       SkipServerCertValidation: false,
                       ServerCertAuthority: null)
            {
            }

            public bool IsEquivalent(Connection other)
            {
                if (other == null)
                {
                    return false;
                }

                if (Object.ReferenceEquals(this, other))
                {
                    return true;
                }

                if (!Object.ReferenceEquals(ServerHost, other.ServerHost)
                        && !ServerHost.Equals(other.ServerHost, StringComparison.Ordinal))
                {
                    return false;
                }

                if (ServerPort != other.ServerPort)
                {
                    return false;
                }

                if (IsTlsEnabled != other.IsTlsEnabled)
                {
                    return false;
                }

                // If TLS is disabled for both, then the TLS settings do not matter.
                if (IsTlsEnabled == false)
                {
                    return true;
                }

                if (SkipServerCertValidation != other.SkipServerCertValidation)
                {
                    return false;
                }

                if (!TemporalClientConfiguration.TlsCertificate.AreEquivalent(ClientIdentityCert, other.ClientIdentityCert))
                {
                    return false;
                }

                if (!TemporalClientConfiguration.TlsCertificate.AreEquivalent(ServerCertAuthority, other.ServerCertAuthority))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
