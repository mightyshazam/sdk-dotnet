using System;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Api.Enums.V1;
using Temporal.Api.WorkflowService.V1;

namespace Temporal.WorkflowClient
{
    public interface IWorkflowRunHandle
    {
        string Namespace { get; }
        string WorkflowId { get; }
        string WorkflowRunId { get; }

        Task<IWorkflowHandle> GetOwnerWorkflowAsync(CancellationToken cancelToken = default);

        #region --- APIs to describe the workflow run ---

        Task<string> GetWorkflowTypeNameAsync(CancellationToken cancelToken = default);

        Task<WorkflowExecutionStatus> GetStatusAsync(CancellationToken cancelToken = default);

        Task<bool> ExistsAsync(CancellationToken cancelToken = default);

        Task<DescribeWorkflowExecutionResponse> DescribeAsync(CancellationToken cancelToken = default);

        #endregion --- APIs to describe the workflow run ---

        #region --- APIs to interact with the chain ---

        /// <summary>The returned task completes when this chain finishes (incl. any runs not yet started). Performs long poll.</summary>
        Task<TResult> GetResultAsync<TResult>(CancellationToken cancelToken = default);

        Task<IWorkflowRunResult> AwaitConclusionAsync(CancellationToken cancelToken = default);

        Task SignalAsync(string signalName,
                         CancellationToken cancelToken = default);

        Task SignalAsync<TSigArg>(string signalName,
                                  TSigArg signalArg,
                                  CancellationToken cancelToken = default);

        Task<TResult> QueryAsync<TResult>(string queryName,
                                          CancellationToken cancelToken = default);

        Task<TResult> QueryAsync<TQryArg, TResult>(string queryName,
                                                   TQryArg queryArg,
                                                   CancellationToken cancelToken = default);

        Task RequestCancellationAsync(CancellationToken cancelToken = default);

        Task TerminateAsync(string reason = null,
                            CancellationToken cancelToken = default);

        Task TerminateAsync<TTermArg>(string reason,
                                      TTermArg details,
                                      CancellationToken cancelToken = default);
        #endregion --- APIs to interact with the chain ---
    }
}
