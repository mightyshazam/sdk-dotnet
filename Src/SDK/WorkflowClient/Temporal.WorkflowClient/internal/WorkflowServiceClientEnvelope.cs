using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Temporal.Api.WorkflowService.V1;
using Temporal.Util;

namespace Temporal.WorkflowClient
{
    internal sealed class WorkflowServiceClientEnvelope
    {
        private readonly WorkflowService.WorkflowServiceClient _grpcWorkflowServiceClient;
        private readonly ChannelBase _grpcChannel;
        private readonly TemporalClientConfiguration.Connection _connectionConfig;

        private readonly object _refCountLock = new();  // Held for very short periods and relatively seldom.

        private int _refCount = 0;

        public WorkflowServiceClientEnvelope(WorkflowService.WorkflowServiceClient grpcWorkflowServiceClient)
            : this(grpcWorkflowServiceClient, grpcChannel: null, connectionConfig: null)
        {
        }

        public WorkflowServiceClientEnvelope(WorkflowService.WorkflowServiceClient grpcWorkflowServiceClient,
                                             ChannelBase grpcChannel,
                                             TemporalClientConfiguration.Connection connectionConfig)
        {
            _grpcWorkflowServiceClient = grpcWorkflowServiceClient;
            _grpcChannel = grpcChannel;
            _connectionConfig = connectionConfig;
        }

        public WorkflowService.WorkflowServiceClient GrpcWorkflowServiceClient
        {
            get { return _grpcWorkflowServiceClient; }
        }

        public TemporalClientConfiguration.Connection ConnectionConfig
        {
            get { return _connectionConfig; }
        }

        public bool IsLastRefReleased
        {
            get { return (_refCount < 0); }
        }

        public void Release()
        {
            bool lastRefReleased;

            lock (_refCountLock)
            {
                int remainingRefCount = --_refCount;
                lastRefReleased = (remainingRefCount == 0);

                if (_refCount <= 0)
                {
                    _refCount = -1;
                }
            }

            if (lastRefReleased)
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        public bool TryAddRef()
        {
            lock (_refCountLock)
            {
                if (IsLastRefReleased)
                {
                    return false;
                }

                ++_refCount;
                return true;
            }
        }

        public void AddRef()
        {
            if (!TryAddRef())
            {
                throw new ObjectDisposedException($"Cannot {nameof(AddRef)}() on this {this.GetType().Name}. Instance already disposed.");
            }
        }

        ~WorkflowServiceClientEnvelope()
        {
            Dispose(disposing: false);
        }

        private void Dispose(bool disposing)
        {
            ExceptionAggregator exAgg = new();

            try
            {
                if (_grpcWorkflowServiceClient != null && _grpcWorkflowServiceClient is IDisposable disposableClient)
                {
                    disposableClient.Dispose();
                }
            }
            catch (Exception ex)
            {
                exAgg.Add(ex);
            }

            try
            {
                if (_grpcChannel != null && _grpcChannel is IDisposable disposableChannel)
                {
                    disposableChannel.Dispose();
                }
            }
            catch (Exception ex)
            {
                exAgg.Add(ex);
            }

            try
            {
                if (_connectionConfig != null && _connectionConfig is IDisposable disposableConfig)
                {
                    disposableConfig.Dispose();
                }
            }
            catch (Exception ex)
            {
                exAgg.Add(ex);
            }


            if (disposing)
            {
                exAgg.ThrowIfNotEmpty();  // Only rethrow on non-finalizer thread
            }
        }
    }
}
