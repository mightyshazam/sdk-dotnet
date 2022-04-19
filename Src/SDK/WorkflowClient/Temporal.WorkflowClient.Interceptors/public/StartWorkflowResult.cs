using System;
using System.Threading;
using Temporal.Util;
using GrpcStatusCode = Grpc.Core.StatusCode;

namespace Temporal.WorkflowClient.Interceptors
{
    public static class StartWorkflow
    {
        public record Arguments<TWfArg>(string Namespace,
                                        string WorkflowId,
                                        string WorkflowTypeName,
                                        string TaskQueue,
                                        TWfArg WorkflowArg,
                                        StartWorkflowChainConfiguration WorkflowConfig,
                                        bool ThrowOnAlreadyExists,
                                        CancellationToken CancelToken);
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
