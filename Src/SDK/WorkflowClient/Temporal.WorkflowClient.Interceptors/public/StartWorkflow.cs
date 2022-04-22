using System;
using System.Threading;
using Temporal.Util;
using Temporal.WorkflowClient.OperationConfigurations;
using GrpcStatusCode = Grpc.Core.StatusCode;

namespace Temporal.WorkflowClient.Interceptors
{
    public static class StartWorkflow
    {
        public static class Arguments
        {
            public abstract record Base<TWfArg>(string Namespace,
                                                string WorkflowId,
                                                string WorkflowTypeName,
                                                string TaskQueue,
                                                TWfArg WorkflowArg,
                                                StartWorkflowConfiguration WorkflowConfig,
                                                CancellationToken CancelToken) : IWorkflowOperationArguments
            {
                public string WorkflowChainId
                {
                    get { throw new NotSupportedException($"{nameof(WorkflowChainId)} is not supported by {GetType().Name}."); }
                }

                public string WorkflowRunId
                {
                    get { throw new NotSupportedException($"{nameof(WorkflowRunId)} is not supported by {GetType().Name}."); }
                }
            }

            public record StartOnly<TWfArg>(string Namespace,
                                            string WorkflowId,
                                            string WorkflowTypeName,
                                            string TaskQueue,
                                            TWfArg WorkflowArg,
                                            StartWorkflowConfiguration WorkflowConfig,
                                            bool ThrowIfWorkflowChainAlreadyExists,
                                            CancellationToken CancelToken)
                : Base<TWfArg>(Namespace,
                               WorkflowId,
                               WorkflowTypeName,
                               TaskQueue,
                               WorkflowArg,
                               WorkflowConfig,
                               CancelToken);

            public record WithSignal<TWfArg, TSigArg>(string Namespace,
                                                      string WorkflowId,
                                                      string WorkflowTypeName,
                                                      string TaskQueue,
                                                      TWfArg WorkflowArg,
                                                      string SignalName,
                                                      TSigArg SignalArg,
                                                      StartWorkflowConfiguration WorkflowConfig,
                                                      CancellationToken CancelToken)
        : Base<TWfArg>(Namespace,
                               WorkflowId,
                               WorkflowTypeName,
                               TaskQueue,
                               WorkflowArg,
                               WorkflowConfig,
                               CancelToken);
        }

        public class Result : IWorkflowOperationResult
        {
            public Result(string workflowRunId)
            {
                Validate.NotNullOrWhitespace(workflowRunId);

                WorkflowRunId = workflowRunId;
                StatusCode = GrpcStatusCode.OK;
            }

            public Result(GrpcStatusCode statusCode)
            {
                if (statusCode == GrpcStatusCode.OK)
                {
                    throw new ArgumentException($"Use this ctor overload for non-OK gRPC status codes."
                                              + $" For OK, use the other ctor overload and specify a {nameof(WorkflowRunId)}.");
                }

                WorkflowRunId = null;
                StatusCode = statusCode;
            }

            public string WorkflowRunId { get; }
            public GrpcStatusCode StatusCode { get; }

            public bool TryGetBoundWorkflowChainId(out string workflowChainId)
            {
                if (StatusCode == GrpcStatusCode.OK)
                {
                    workflowChainId = WorkflowRunId;
                    return true;
                }

                workflowChainId = null;
                return false;
            }
        }
    }
}
