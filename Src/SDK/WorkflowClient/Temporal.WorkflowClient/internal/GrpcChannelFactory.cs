using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Temporal.Util;
using Grpc.Core;

namespace Temporal.WorkflowClient
{
    internal class GrpcChannelFactory
    {
        private record ChannelInfo(string ServiceHost, int ServicePort)
        {
            public int CountUsers { get; set; }

            public bool IsEquivalent(ChannelInfo other)
            {
                return (other != null)
                            && (ServicePort == other.ServicePort)
                            && ((ServiceHost == null && other.ServiceHost == null)
                                    || (ServiceHost != null && other.ServiceHost != null
                                            && ServiceHost.Trim().Equals(other.ServiceHost.Trim(), StringComparison.Ordinal)));
            }

            public bool IsEquivalent(TemporalClientConfiguration other)
            {
                return (other != null)
                            && (ServicePort == other.ServicePort)
                            && ((ServiceHost == null && other.ServiceHost == null)
                                    || (ServiceHost != null && other.ServiceHost != null
                                            && ServiceHost.Trim().Equals(other.ServiceHost.Trim(), StringComparison.Ordinal)));
            }
        };  // record ChannelInfo

        public static GrpcChannelFactory SingletonInstance = new GrpcChannelFactory();

        private readonly List<KeyValuePair<ChannelInfo, ChannelBase>> _existingChannels = new List<KeyValuePair<ChannelInfo, ChannelBase>>(capacity: 1);

        private GrpcChannelFactory()
        {
        }

        public ChannelBase GetOrCreateChannel(TemporalClientConfiguration config)
        {
            Validate.NotNull(config);

            lock (_existingChannels)
            {
                // We expect a very small number of channels per app. The following scan is more efficient than a lookup.
                for (int i = 0; i < _existingChannels.Count; i++)
                {
                    KeyValuePair<ChannelInfo, ChannelBase> channelInfo = _existingChannels[i];
                    if (channelInfo.Key.IsEquivalent(config))
                    {
                        channelInfo.Key.CountUsers = channelInfo.Key.CountUsers + 1;
                        return channelInfo.Value;
                    }
                }

                ChannelBase channel = CreateNewChannel(config);
                ChannelInfo info = new ChannelInfo(config.ServiceHost, config.ServicePort)
                {
                    CountUsers = 1
                };

                _existingChannels.Add(new KeyValuePair<ChannelInfo, ChannelBase>(info, channel));

                return channel;
            }
        }

        public void ReleaseChannel(ChannelBase channel)
        {
            Validate.NotNull(channel);

            lock (_existingChannels)
            {
                // We expect a very small number of channels per app. The following scan is more efficient than a lookup.
                for (int i = 0; i < _existingChannels.Count; i++)
                {
                    KeyValuePair<ChannelInfo, ChannelBase> channelInfo = _existingChannels[i];
                    if (Object.ReferenceEquals(channelInfo.Value, channel))
                    {
                        channelInfo.Key.CountUsers = channelInfo.Key.CountUsers - 1;

                        if (channelInfo.Key.CountUsers == 0)
                        {
                            _existingChannels.RemoveAt(i);

                            if (channel is IDisposable disposableChannel)
                            {
                                disposableChannel.Dispose();
                            }
                        }

                        return;
                    }
                }

                throw new ArgumentException($"Cannot release this {nameof(channel)} because it"
                                          + $" is not tracked by this {nameof(GrpcChannelFactory)}.");
            }
        }

        private ChannelBase CreateNewChannel(TemporalClientConfiguration config)
        {
            // We use GRPC Core for Net Fx and GRPC.Net for Net Core.
            // https://docs.microsoft.com/en-us/aspnet/core/grpc/netstandard?view=aspnetcore-6.0#grpc-c-core-library
#if NETFRAMEWORK
            return CreateNewChannelNetFx(config);
#else
            return CreateNewChannelNetCore(config);
#endif
        }

#if !NETFRAMEWORK
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Only invoked for some targets.")]
#endif
        private ChannelBase CreateNewChannelNetFx(TemporalClientConfiguration config)
        {
#if NETFRAMEWORK
            Grpc.Core.Channel channel = new Grpc.Core.Channel(config.ServiceHost, config.ServicePort, ChannelCredentials.Insecure);
            return channel;
#else
            throw new NotSupportedException("This routine is only supported on Net Fx.");
#endif
        }

#if !NETCOREAPP
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Only invoked for some targets.")]
#endif
        private ChannelBase CreateNewChannelNetCore(TemporalClientConfiguration config)
        {
#if NETCOREAPP
            // On Net Core 3, it was required to set this flag to make unsecured GRPC connections:
            if (config.IsHttpsEnabled == false && RuntimeEnvironmentInfo.SingletonInstance.RuntimeVersion.StartsWith("3"))
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            }

            string address = $"http://{config.ServiceHost}:{config.ServicePort}";

            Grpc.Net.Client.GrpcChannel channel = Grpc.Net.Client.GrpcChannel.ForAddress(address);
            return channel;
#else
            throw new NotSupportedException("This routine is only supported on Net Core and Net 5+.");
#endif
        }
    }
}
