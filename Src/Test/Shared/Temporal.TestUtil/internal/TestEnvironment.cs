using System;
using System.IO;

namespace Temporal.TestUtil
{
    internal static class TestEnvironment
    {
        private const string CaCertificate = "ca.pem";
        private const string ClientCertificate = "client.pem";
        private const string ClientKey = "client-key.pem";
        private const string ServerCertificate = "server.pem";
        private const string ServerKey = "server-key.pem";

        internal static string CaCertificatePath
        {
            get
            {
                return GetCertificatePath(CaCertificate);
            }
        }

        internal static string ClientCertificatePath
        {
            get
            {
                return GetCertificatePath(ClientCertificate);
            }
        }

        internal static string ClientKeyPath
        {
            get
            {
                return GetCertificatePath(ClientKey);
            }
        }

        internal static string ServerCertificatePath
        {
            get
            {
                return GetCertificatePath(ServerCertificate);
            }
        }

        internal static string ServerKeyPath
        {
            get
            {
                return GetCertificatePath(ServerKey);
            }
        }

        public static bool IsWindows
        {
            get { return (Environment.OSVersion.Platform.ToString().IndexOf("Win") == 0); }
        }

        public static string GetBinaryRootDirPath()
        {
            const string BinaryRootDirName = "_build";

            string currentFolder = Environment.CurrentDirectory;
            int offset = currentFolder.IndexOf(BinaryRootDirName, StringComparison.Ordinal);
            if (offset != -1)
            {
                return currentFolder.Substring(0, offset + BinaryRootDirName.Length);
            }

            throw new Exception($"Cannot find Binary Root Dir \"{BinaryRootDirName}\" above the current working dir \"{currentFolder}\"/");
        }

        public static string GetEnvironmentRootDirPath()
        {
            string binaryRootDirPath = TestEnvironment.GetBinaryRootDirPath();
            return Path.GetDirectoryName(binaryRootDirPath);
        }

        private static string GetCertificatePath(string certificate)
        {
            return Path.Combine("Certificates", certificate);
        }
    }
}