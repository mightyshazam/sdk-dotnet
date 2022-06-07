using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

#if NETCOREAPP3_1
using System.Security.Cryptography;
using Temporal.Util;
#endif

namespace Temporal.WorkflowClient
{
    internal static class X509Cert
    {
        public static bool AreEqual(X509Certificate2 cert1, X509Certificate2 cert2)
        {
            if (Object.ReferenceEquals(cert1, cert2))
            {
                return true;
            }

            if (cert1 == null || cert2 == null)
            {
                return false;
            }

            if (true != cert1.Thumbprint?.Equals(cert2.Thumbprint))
            {
                return false;
            }

            return DataEqual(cert1.RawData, cert2.RawData);
        }

        public static X509Certificate2 CreateFromPemFile(string certPemFilePath)
        {
            return CreateFromPemFile(certPemFilePath, keyPemFilePath: null);
        }

        public static X509Certificate2 CreateFromPemFile(string certPemFilePath, string keyPemFilePath)
        {
            string certData = String.IsNullOrWhiteSpace(certPemFilePath)
                                        ? null
                                        : File.ReadAllText(certPemFilePath);

            string keyData = String.IsNullOrWhiteSpace(keyPemFilePath)
                                        ? null
                                        : File.ReadAllText(keyPemFilePath);

            return CreateFromPemData(certData, keyData);
        }

        public static X509Certificate2 CreateFromPemData(string certMarkedUpPemData)
        {
            return CreateFromPemData(certMarkedUpPemData, keyMarkedUpPemData: null);
        }

        public static X509Certificate2 CreateFromPemData(string certMarkedUpPemData, string keyMarkedUpPemData)
        {
            const string Password = "password";
            // Get the ephemeral (in-memory) cert:

            using X509Certificate2 ephemeralCert = CreateX509EphemeralCertFromPemData(certMarkedUpPemData, keyMarkedUpPemData);

            // Work around Windows ephemeral cert bugs:
            // (https://github.com/natemcmaster/LettuceEncrypt/pull/110)
            // (https://stackoverflow.com/questions/55456807/create-x509certificate2-from-cert-and-key-without-making-a-pfx-file)
            // (https://github.com/dotnet/runtime/issues/23749)
            // @ToDo: Review this for other OSes when supported.

            if (!ephemeralCert.HasPrivateKey)
            {
                return new(ephemeralCert.Export(X509ContentType.Cert));
            }

            // We use password here because some operating systems will fail when exporting a pfx
            // without a password. The password used has no value outside of this method
            byte[] ephemeralCertBytes = ephemeralCert.Export(X509ContentType.Pfx, Password);
            X509Certificate2 certificate = new(ephemeralCertBytes, Password);

            // Done:
            return certificate;
        }

        /// <summary>
        /// The `X509Certificate2.CreateFromPem(..)` API was not available before Net 6.
        /// This method implements the respective logic for older Framework versions and delegates to the Framework API where possible.
        /// Inspired by 
        ///   https://github.com/grpc/grpc-dotnet/blob/dd72d6a38ab2984fd224aa8ed53686dc0153b9da/testassets/InteropTestsClient/InteropClient.cs#L898-L918
        /// and
        ///   https://stackoverflow.com/questions/7400500/how-to-get-private-key-from-pem-file/10498045#10498045
        /// </summary>        
        private static X509Certificate2 CreateX509EphemeralCertFromPemData(string certMarkedUpPemData, string keyMarkedUpPemData)
        {
#if NET6_0_OR_GREATER
            return (keyMarkedUpPemData == null)
                        ? X509Certificate2.CreateFromPem(certMarkedUpPemData)
                        : X509Certificate2.CreateFromPem(certMarkedUpPemData, keyMarkedUpPemData);
#elif NETCOREAPP3_1_OR_GREATER
            // Create public cert:

            byte[] certBytes = GetPemSectionContent(certMarkedUpPemData, "CERTIFICATE");
            X509Certificate2 pubCert = new(certBytes);

            if (keyMarkedUpPemData == null)
            {
                return pubCert;
            }

            // Add private key to get complete cert:

            try
            {
                ExceptionAggregator exAggr = new();
                using RSA rsa = RSA.Create();
                bool privKeyLoaded = false;

                try
                {
                    byte[] keyBytes = GetPemSectionContent(keyMarkedUpPemData, "PRIVATE KEY");
                    rsa.ImportPkcs8PrivateKey(keyBytes, out _);
                    privKeyLoaded = true;
                }
                catch (Exception ex)
                {
                    exAggr.Add(ex);
                }

                if (!privKeyLoaded)
                {
                    try
                    {
                        byte[] keyBytes = GetPemSectionContent(keyMarkedUpPemData, "RSA PRIVATE KEY");
                        rsa.ImportRSAPrivateKey(keyBytes, out _);
                        privKeyLoaded = true;
                    }
                    catch (Exception ex)
                    {
                        exAggr.Add(ex);
                    }
                }

                if (!privKeyLoaded)
                {
                    try
                    {
                        byte[] keyBytes = GetPemSectionContent(keyMarkedUpPemData, "EC PRIVATE KEY");
                        using ECDsa ec = ECDsa.Create();
                        ec.ImportECPrivateKey(keyBytes, out _);
                        return pubCert.CopyWithPrivateKey(ec);
                    }
                    catch (Exception ex)
                    {
                        exAggr.Add(ex);
                    }
                }

                if (!privKeyLoaded)
                {
                    exAggr.ThrowIfNotEmpty();
                    throw new InvalidOperationException($"Could not read the private key from {nameof(keyMarkedUpPemData)}.");
                }

                X509Certificate2 ephemeralCert = pubCert.CopyWithPrivateKey(rsa);
                return ephemeralCert;
            }
            finally
            {
                pubCert.Dispose();
            }
#else
            throw new NotSupportedException("This method is not supported under the targeted .NET version.");
#endif
        }

#if NETCOREAPP3_1_OR_GREATER && !NET6_0_OR_GREATER
        private static byte[] GetPemSectionContent(string markedUpPem, string sectionName)
        {
            const string SectionStartMarkerTemplate = "-----BEGIN {0}-----";
            const string SectionEndMarkerTemplate = "-----END {0}-----";

            string sectionStartMarker = String.Format(SectionStartMarkerTemplate, sectionName);
            string sectionEndMarker = String.Format(SectionEndMarkerTemplate, sectionName);

            int sectionDataStartIndex = markedUpPem.IndexOf(sectionStartMarker, StringComparison.Ordinal);
            if (sectionDataStartIndex < 0)
            {
                throw new FormatException($"Cannot find the start of the specified section (Marker=\"{sectionStartMarker}\").");
            }

            sectionDataStartIndex += sectionStartMarker.Length;

            int sectionDataEndIndex = markedUpPem.IndexOf(sectionEndMarker, sectionDataStartIndex, StringComparison.Ordinal)
                                    - sectionDataStartIndex;

            if (sectionDataEndIndex < 0)
            {
                throw new FormatException($"Cannot find the end of the specified section"
                                        + $" (SearchStartIndex={sectionDataStartIndex}; Marker=\"{sectionEndMarker}\").");
            }

            string sectionContentEncodedBytes = markedUpPem.Substring(sectionDataStartIndex, sectionDataEndIndex);
            return Convert.FromBase64String(sectionContentEncodedBytes);
        }
#endif

        private static bool DataEqual(byte[] data1, byte[] data2)
        {
            if (Object.ReferenceEquals(data1, data2))
            {
                return true;
            }

            if (data1 == null || data2 == null || data1.Length != data2.Length)
            {
                return false;
            }

            for (int i = 0; i < data1.Length; i++)
            {
                if (data1[i] != data2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}