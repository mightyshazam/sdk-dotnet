using System;
using System.Threading;
using Temporal.Util;
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
                                                StartWorkflowChainConfiguration WorkflowConfig,
                                                CancellationToken CancelToken);

            public record StartOnly<TWfArg>(string Namespace,
                                            string WorkflowId,
                                            string WorkflowTypeName,
                                            string TaskQueue,
                                            TWfArg WorkflowArg,
                                            StartWorkflowChainConfiguration WorkflowConfig,
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
                                                      StartWorkflowChainConfiguration WorkflowConfig,
                                                      CancellationToken CancelToken)
        : Base<TWfArg>(Namespace,
                               WorkflowId,
                               WorkflowTypeName,
                               TaskQueue,
                               WorkflowArg,
                               WorkflowConfig,
                               CancelToken);
        }

        public class Result : IWorkflowChainBindingResult
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
