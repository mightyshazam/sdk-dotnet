using System;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Api.Enums.V1;
using Temporal.Api.WorkflowService.V1;
using Temporal.Util;

namespace Temporal.WorkflowClient
{
    internal class WorkflowRunHandle : IWorkflowRunHandle
    {
        private IWorkflowHandle _ownerWorkflow = null;

        public WorkflowRunHandle(string workflowId, string workflowRunId)
        {
            Validate.NotNull(workflowId);
            Validate.NotNull(workflowRunId);

            WorkflowId = workflowId;
            WorkflowRunId = workflowRunId;
        }

        public WorkflowRunHandle(IWorkflowHandle workflow, string workflowRunId)
        {
            Validate.NotNull(workflow);
            Validate.NotNull(workflowRunId);

            WorkflowId = workflow.WorkflowId;
            WorkflowRunId = workflowRunId;
            _ownerWorkflow = workflow;
        }

        #region --- APIs to access basic workflow run details ---

        public string Namespace { get; }
        public string WorkflowId { get; }
        public string WorkflowRunId { get; }

        public Task<IWorkflowHandle> GetOwnerWorkflowAsync(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        #endregion --- APIs to access basic workflow run details ---

        #region --- APIs to describe the workflow run ---

        public Task<string> GetWorkflowTypeNameAsync(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        public Task<WorkflowExecutionStatus> GetStatusAsync(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        public Task<bool> ExistsAsync(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        public Task<DescribeWorkflowExecutionResponse> DescribeAsync(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        #endregion --- APIs to describe the workflow run ---

        #region --- APIs to interact with the workflow run ---

        /// <summary>The returned task completes when this chain finishes (incl. any runs not yet started). Performs long poll.</summary>
        public Task<TResult> GetResultAsync<TResult>(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        public Task<IWorkflowRunResult> AwaitConclusionAsync(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        public Task SignalAsync(string signalName,
                                CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        public Task SignalAsync<TSigArg>(string signalName,
                                         TSigArg signalArg,
                                         CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        public Task<TResult> QueryAsync<TResult>(string queryName,
                                                 CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        public Task<TResult> QueryAsync<TQryArg, TResult>(string queryName,
                                                          TQryArg queryArg,
                                                          CancellationToken cancelToken = default)

        {
            throw new NotImplementedException("@ToDo");
        }

        public Task RequestCancellationAsync(CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        public Task TerminateAsync(string reason = null,
                                   CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }

        public Task TerminateAsync<TTermArg>(string reason,
                                             TTermArg details,
                                             CancellationToken cancelToken = default)
        {
            throw new NotImplementedException("@ToDo");
        }
        #endregion --- APIs to interact with the workflow run ---
    }
}
