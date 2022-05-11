using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Temporal.Util;
using Grpc.Core;
using Temporal.Api.WorkflowService.V1;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Net.Security;

namespace Temporal.WorkflowClient
{
    /// <summary>
    /// The factory is responsible for creation of gRPC channels based on settings provided via
    /// <see cref="TemporalClientConfiguration.Connection"/>.
    /// <para>
    /// The conceptual layering is:<br />
    /// <code>
    ///     [User-facing Temporal workflow client (`Temporal.WorkflowClient . TemporalClient`)]
    ///             ^
    ///             |
    ///     [Raw gRPC service client (`Temporal.Api.WorkflowService.V1 . WorkflowService.WorkflowServiceClient`)]
    ///             ^
    ///             |
    ///     [gRPC channel (`Grpc.Core . ChannelBase`)] 
    /// </code>
    /// The raw gRPC is a very lightweight object.
    /// The Temporal workflow client encapsulates a few more objects (e.g., the service invocation pipeline with all
    /// the Temporal interceptors, Payload Converters, etc.). However, it is still leightweight and does not directly
    /// use any significant ressources.
    /// But, the gRPC channel is a heavyweight object: it encapsulates the underlying network connection.
    /// </para>
    /// <para>
    /// To accouhnt for that, when a client is created, it will ask this factory to for an underlying channel (except
    /// if the user provided a channel they constructed explicitly). The factory will check whether is already has a
    /// channel that is compatible with the settings of the client. If yes, it will return the existing channel.
    /// Otherwise, it will construct a new channel, add it to an internal list, and return it to the client.
    /// </para>
    /// <para>
    /// To make sure what channels are discarded as soon as they are no longer needed, they are wrapped into instances
    /// of <see cref="WorkflowServiceClientEnvelope"/> (along with the raw gRPC service clients for convenience).
    /// The <c>WorkflowServiceClientEnvelope</c> relies on ref counting to know when it is used and when it can safely
    /// dispose the underlying channel.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>On the classic Net Fx, a <see cref="PlatformNotSupportedException"/> may be thrown if <c>SkipServerCertValidation</c> is set
    /// for the respective configuration. This is because on Net Fx, we use <c>Grpc.Core.Channel</c> which is based on the gRPC Core C-Lib.
    /// <c>SkipServerCertValidation</c> requires custom code to run for server cert validation, but that gRPC library only allows such
    /// custom code to run AFTER server validation has been performed.</para>
    /// <para>This goes in tandem with the fact that on some exotic platforms running Net Core, providing a custom <c>ServerCertAuthority</c>
    /// may also result in a <see cref="PlatformNotSupportedException"/>. That is because that feature requires setting
    /// <c>HttpClientHandler.ServerCertificateCustomValidationCallback</c> which is not supported on all plattforms.<br/>
    /// https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclienthandler.servercertificatecustomvalidationcallback <br/>
    /// https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclienthandler.dangerousacceptanyservercertificatevalidator</para>
    /// </remarks>
    internal class WorkflowServiceClientFactory
    {
        public static WorkflowServiceClientFactory SingletonInstance = new WorkflowServiceClientFactory();

        private readonly List<WeakReference<WorkflowServiceClientEnvelope>> _existingClients = new(capacity: 1);

        private WorkflowServiceClientFactory()
        {
        }

        public WorkflowServiceClientEnvelope GetOrCreateClient(TemporalClientConfiguration.Connection connectionConfig)
        {
            TemporalClientConfiguration.Connection.Validate(connectionConfig);

            if (TryGetClient(connectionConfig, out WorkflowServiceClientEnvelope client))
            {
                return client;
            }

            client = CreateChannelAndClient(connectionConfig);

            if (!TryInsertNewClient(client, out WorkflowServiceClientEnvelope prevExistingClient))
            {
                client.AddRef();                // Add-Release to cause 
                client.Release();               // the client to dispose itself.
                client = prevExistingClient;
            }

            return client;
        }

        private bool TryGetClient(TemporalClientConfiguration.Connection connectionConfig, out WorkflowServiceClientEnvelope client)
        {
            lock (_existingClients)
            {
                // We expect a very small number of channels per app. The following scan is more efficient than a lookup.
                int i = 0;
                while (i < _existingClients.Count)
                {
                    WeakReference<WorkflowServiceClientEnvelope> clientRef = _existingClients[i];

                    // If the client is no longer available, we can remove its entry from the list.
                    if (!clientRef.TryGetTarget(out client))
                    {
                        _existingClients.RemoveAt(i);
                        continue;
                    }

                    // Possibly the client was already released, but the GC did not yet collect it, so instance is still available.
                    // In that case we still need to remove it:
                    if (client.IsLastRefReleased)
                    {
                        _existingClients.RemoveAt(i);
                        continue;
                    }

                    if (client.ConnectionConfig.IsEquivalent(connectionConfig))
                    {
                        return true;
                    }

                    i++;
                }

                client = null;
                return false;
            }
        }

        private bool TryInsertNewClient(WorkflowServiceClientEnvelope newClient, out WorkflowServiceClientEnvelope prevExistingClient)
        {
            lock (_existingClients)
            {
                // We expect a very small number of channels per app. The following scan is more efficient than a lookup.
                int i = 0;
                while (i < _existingClients.Count)
                {
                    WeakReference<WorkflowServiceClientEnvelope> clientRef = _existingClients[i];

                    if (!clientRef.TryGetTarget(out prevExistingClient))
                    {
                        _existingClients.RemoveAt(i);
                        continue;
                    }

                    if (prevExistingClient.ConnectionConfig.IsEquivalent(newClient.ConnectionConfig))
                    {
                        return false;
                    }

                    i++;
                }

                _existingClients.Add(new WeakReference<WorkflowServiceClientEnvelope>(newClient));
                prevExistingClient = null;
                return true;
            }
        }

        private WorkflowServiceClientEnvelope CreateChannelAndClient(TemporalClientConfiguration.Connection connectionConfig)
        {
            ChannelBase channel = CreateNewChannel(connectionConfig);
            WorkflowService.WorkflowServiceClient client = new(channel);

            WorkflowServiceClientEnvelope envelope = new(client, channel, connectionConfig);
            return envelope;
        }

        private ChannelBase CreateNewChannel(TemporalClientConfiguration.Connection connectionConfig)
        {
            // We use GRPC Core for Net Fx and GRPC.Net for Net Core.
            // https://docs.microsoft.com/en-us/aspnet/core/grpc/netstandard?view=aspnetcore-6.0#grpc-c-core-library
#if NETFRAMEWORK
            return CreateNewChannelNetFx(connectionConfig);
#else
            return CreateNewChannelNetCore(connectionConfig);
#endif
        }

#if !NETFRAMEWORK
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Only invoked for some targets.")]
#endif
        private ChannelBase CreateNewChannelNetFx(TemporalClientConfiguration.Connection config)
        {
#if !NETFRAMEWORK
            throw new PlatformNotSupportedException("This routine is only supported on Net Fx.");
#else
            if (!config.IsTlsEnabled)
            {
                Grpc.Core.Channel plainChannel = new Grpc.Core.Channel(config.ServerHost, config.ServerPort, ChannelCredentials.Insecure);
                return plainChannel;
            }

            if (config.SkipServerCertValidation)
            {
                throw new PlatformNotSupportedException($"{nameof(config.SkipServerCertValidation)} is not supported on Net Fx."
                                + $" (Background: On the classic Net Fx we use `Grpc.Core.Channel` for gRPC connections,"
                                + $" which is based on the gRPC C-Lib. That only allows executing a custom"
                                + $" `VerifyPeerCallback` AFTER a successfull CA validation.)");
            }

            KeyCertificatePair clientIdentity = null;
            string rootCertsData = null;

            if (config.ClientIdentityCert != null)
            {
                if (!config.ClientIdentityCert.HasPemData)
                {
                    throw new ArgumentException($"A TLS client identity has been provided, but the specified {nameof(TemporalClientConfiguration)}"
                                + $".{nameof(TemporalClientConfiguration.Connection)}"
                                + $".{nameof(TemporalClientConfiguration.Connection.ClientIdentityCert)}"
                                + $" does not have any PEM Data. PEM Data is required to configure a TLS certificate on this version of Net Fx.");
                }

                clientIdentity = new KeyCertificatePair(config.ClientIdentityCert.PublicCertPemData, config.ClientIdentityCert.PrivateKeyPemData);
            }

            if (config.ServerCertAuthority != null)
            {
                if (!config.ServerCertAuthority.HasPemData)
                {
                    throw new ArgumentException($"A custom Certificate Authority public cert for validating the remote server"
                                + $" TLS cert has been provided, but the specified {nameof(TemporalClientConfiguration)}"
                                + $".{nameof(TemporalClientConfiguration.Connection)}"
                                + $".{nameof(TemporalClientConfiguration.Connection.ServerCertAuthority)}"
                                + $" does not have any PEM Data. PEM Data is required to configure a TLS certificate on this version of Net Fx.");
                }

                rootCertsData = config.ServerCertAuthority.PublicCertPemData;
            }

            SslCredentials sslCreds = new(rootCertsData, clientIdentity);
            Grpc.Core.Channel sslChannel = new Grpc.Core.Channel(config.ServerHost, config.ServerPort, sslCreds);
            return sslChannel;
#endif
        }

#if !NETCOREAPP
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Only invoked for some targets.")]
#endif
        private ChannelBase CreateNewChannelNetCore(TemporalClientConfiguration.Connection config)
        {
#if NETCOREAPP
            // On Net Core 3, it was required to set this flag to make unsecured GRPC connections:
            if (config.IsTlsEnabled == false && RuntimeEnvironmentInfo.SingletonInstance.RuntimeVersion.StartsWith("3"))
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            }

            // @ToDo: In the future, when we support logging, we will pipe the configured logger through to the gRPC channal.
            // In the interim, while we are in an early development stage, we regularly need a custom logger during SDK development
            // activities. We keen this code around for convenience and will remove it once full-on logging is supported.
            Microsoft.Extensions.Logging.ILoggerFactory logFactory = null;
            //ILoggerFactory logFactory = Microsoft.Extensions.Logging.LoggerFactory.Create((logBuilder) =>
            //{
            //    ConsoleLoggerExtensions.AddConsole(logBuilder);
            //    logBuilder.SetMinimumLevel(LogLevel.Trace);
            //});

            if (!config.IsTlsEnabled)
            {
                string plainAddress = $"http://{config.ServerHost}:{config.ServerPort}";

                Grpc.Net.Client.GrpcChannelOptions plainChannelOptions = new()
                {
                    LoggerFactory = logFactory
                };

                Grpc.Net.Client.GrpcChannel plainChannel = Grpc.Net.Client.GrpcChannel.ForAddress(plainAddress, plainChannelOptions);
                return plainChannel;
            }

            HttpClientHandler httpClientHandler = new();

            if (config.SkipServerCertValidation)
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            else
            {
                if (config.ServerCertAuthority != null)
                {
                    bool ownCaCert = false;
                    X509Certificate2 caCert = config.ServerCertAuthority.X509Cert;
                    if (caCert == null && config.ServerCertAuthority.HasPemData)
                    {
                        ownCaCert = true;
                        caCert = X509Cert.CreateFromPemData(config.ServerCertAuthority.PublicCertPemData,
                                                            config.ServerCertAuthority.PrivateKeyPemData);
                    }

                    if (caCert == null)
                    {
                        throw new ArgumentException($"A custom Certificate Authority public cert for validating the remote server"
                                        + $" TLS cert has been provided, but the specified {nameof(TemporalClientConfiguration)}"
                                        + $".{nameof(TemporalClientConfiguration.Connection)}"
                                        + $".{nameof(TemporalClientConfiguration.Connection.ServerCertAuthority)}"
                                        + $" does not have any certificate in any form. A certificate is required.");
                    }

                    CustomServerCertificateValidator customValidator = new(caCert);
                    try
                    {
                        httpClientHandler.ServerCertificateCustomValidationCallback = customValidator.ServerCertificateCustomValidationCallback;
                    }
                    catch (NotSupportedException nsEx)
                    {
                        if (ownCaCert)
                        {
                            customValidator.Dispose();
                        }

                        throw new PlatformNotSupportedException($"A custom Certificate Authority is not supported on this Platform, becasue"
                                        + $" .NET does not support setting a custom "
                                        + $"{nameof(HttpClientHandler.ServerCertificateCustomValidationCallback)} on the current platform.",
                                        nsEx);
                    }
                }
            }

            if (config.ClientIdentityCert != null)
            {
                X509Certificate2 clientCert = config.ClientIdentityCert.X509Cert;
                if (clientCert == null && config.ClientIdentityCert.HasPemData)
                {
                    clientCert = X509Cert.CreateFromPemData(config.ClientIdentityCert.PublicCertPemData,
                                                            config.ClientIdentityCert.PrivateKeyPemData);
                }

                if (clientCert == null)
                {
                    throw new ArgumentException($"A TLS client identity has been provided, but the specified {nameof(TemporalClientConfiguration)}"
                                + $".{nameof(TemporalClientConfiguration.Connection)}"
                                + $".{nameof(TemporalClientConfiguration.Connection.ClientIdentityCert)}"
                                + $" does not have any certificate in any form. A certificate is required.");
                }

                httpClientHandler.ClientCertificates.Add(clientCert);
            }


            string sslAddress = $"https://{config.ServerHost}:{config.ServerPort}";

            Grpc.Net.Client.GrpcChannelOptions sslChannelOptions = new()
            {
                LoggerFactory = logFactory,
                HttpHandler = httpClientHandler,
            };

            Grpc.Net.Client.GrpcChannel sslChannel = Grpc.Net.Client.GrpcChannel.ForAddress(sslAddress, sslChannelOptions);
            return sslChannel;
#else
            throw new NotSupportedException("This routine is only supported on Net Core and Net 5+.");
#endif
        }

        #region class CustomServerCertificateValidator
#if NETCOREAPP
        private class CustomServerCertificateValidator : IDisposable
        {
            private X509Certificate2 _caCert;

            public CustomServerCertificateValidator(X509Certificate2 caCert)
            {
                Validate.NotNull(caCert);
                _caCert = caCert;
            }

            private X509Certificate2 CaCert
            {
                get
                {
                    X509Certificate2 caCert = _caCert;
                    if (caCert == null)
                    {
                        throw new ObjectDisposedException($"This {nameof(CustomServerCertificateValidator)} has been disposed.");
                    }

                    return caCert;
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                X509Certificate2 caCert = Interlocked.Exchange(ref _caCert, null);
                if (caCert != null)
                {
                    try
                    {
                        caCert.Dispose();
                    }
                    catch (Exception ex)
                    {
                        if (disposing)
                        {
                            throw ex.Rethrow(); // Only rethrow on non-finalizer thread
                        }
                    }
                }
            }

            ~CustomServerCertificateValidator()
            {
                Dispose(disposing: false);
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            [SuppressMessage("Style",
                             "IDE0060:Remove unused parameter",
                             Justification = "All parameters are required to match the required callback signature.")]
            public bool ServerCertificateCustomValidationCallback(HttpRequestMessage httpRequestMessage,
                                                                  X509Certificate2 cert,
                                                                  X509Chain chain,
                                                                  SslPolicyErrors policyErrors)
            {
                X509Certificate2 caCert = CaCert;

                // Note that caCert could be disposed concurrently, resulting in an error.
                // However, that can only happen by an explicit Dispose call, since a finalizer invocation is prevented by the 
                // reference we hold. An explicit Dispose invocation in not expected - it would indicate a bug in our
                // channel-related code.

                chain = chain ?? new X509Chain();

                chain.ChainPolicy.ExtraStore.Add(caCert);

                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationFlags |= X509VerificationFlags.AllowUnknownCertificateAuthority;

                bool isValidBuild = chain.Build(cert);
                if (!isValidBuild)
                {
                    return false;
                }

                X509Certificate2 chainEnd = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;

                bool caMatch = X509Cert.AreEqual(caCert, chainEnd);
                return caMatch;
            }
        }
#endif
        #endregion class CustomServerCertificateValidator
    }
}
