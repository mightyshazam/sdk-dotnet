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
                                 bool SkipServerValidation,
                                 TemporalClientConfiguration.TlsCertificate ServerCertAuthority)
        {
            internal static class Defaults
            {
                public static class Local
                {
                    public const string ServerHost = "127.0.0.1";  // Do not use "localhost" to avoid the IPv4 resolution wait
                    public const int ServerPort = 7233;
                }

                public static class TemporalCloud
                {
                    public const string ServerHost = "???";
                    public const int ServerPort = -1;
                }
            }

            public static TemporalClientConfiguration.Connection TlsDisabled(string serverHost, int serverPort)
            {
                return new TemporalClientConfiguration.Connection(serverHost,
                                                                  serverPort,
                                                                  isTlsEnabled: false);
            }

            public static TemporalClientConfiguration.Connection TlsEnabled(string serverHost, int serverPort)
            {
                return new TemporalClientConfiguration.Connection(serverHost,
                                                                  serverPort,
                                                                  isTlsEnabled: true);
            }

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

            public Connection()
                : this(Defaults.Local.ServerHost,
                       Defaults.Local.ServerPort,
                       IsTlsEnabled: false,
                       ClientIdentityCert: null,
                       SkipServerValidation: false,
                       ServerCertAuthority: null)
            {
            }

            private Connection(string serverHost, int serverPort, bool isTlsEnabled)
                : this(serverHost,
                       serverPort,
                       isTlsEnabled,
                       ClientIdentityCert: null,
                       SkipServerValidation: false,
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

                if (SkipServerValidation != other.SkipServerValidation)
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
