using System;
using System.Threading;
using Temporal.Api.WorkflowService.V1;
using Temporal.Util;

using GrpcStatusCode = Grpc.Core.StatusCode;

namespace Temporal.WorkflowClient.Interceptors
{
    public static class DescribeWorkflowRun
    {
        public record Arguments(string Namespace,
                                string WorkflowId,
                                string WorkflowChainId,
                                string WorkflowRunId,
                                bool ThrowIfWorkflowNotFound,
                                CancellationToken CancelToken);

        public class Result : IWorkflowChainBindingResult
        {
            public Result(DescribeWorkflowExecutionResponse describeWorkflowExecutionResponse)
            {
                Validate.NotNull(describeWorkflowExecutionResponse);

                DescribeWorkflowExecutionResponse = describeWorkflowExecutionResponse;
                StatusCode = GrpcStatusCode.OK;
            }

            public Result(GrpcStatusCode statusCode)
            {
                if (statusCode == GrpcStatusCode.OK)
                {
                    throw new ArgumentException($"Use this ctor overload for non-OK gRPC status codes."
                                              + $" For OK, use the other ctor overload and specify a {nameof(DescribeWorkflowExecutionResponse)}.");
                }

                DescribeWorkflowExecutionResponse = null;
                StatusCode = statusCode;
            }

            public DescribeWorkflowExecutionResponse DescribeWorkflowExecutionResponse { get; }
            public GrpcStatusCode StatusCode { get; }

            public bool TryGetBoundWorkflowChainId(out string workflowChainId)
            {
                throw new NotImplementedException($"@ToDo. Once supported by the server, this will be extracted"
                                                + $" from {nameof(DescribeWorkflowExecutionResponse)}."
                                                + " See https://github.com/temporalio/temporal/issues/2691");
            }
        }
    }
}
