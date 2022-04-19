using System;
using System.Threading;
using System.Threading.Tasks;

namespace Temporal.WorkflowClient.Interceptors
{
    public interface ITemporalClientInterceptor : IDisposable
    {
        void Init(ITemporalClientInterceptor nextInterceptor);

        Task<StartWorkflow.Result> StartWorkflowAsync<TWfArg>(StartWorkflow.Arguments.StartOnly<TWfArg> opArgs);
        Task<StartWorkflow.Result> StartWorkflowWithSignalAsync<TWfArg, TSigArg>(StartWorkflow.Arguments.WithSignal<TWfArg, TSigArg> opArgs);
        Task<IWorkflowRunResult> AwaitConclusionAsync(AwaitConclusion.Arguments opArgs);
        Task<GetWorkflowChainId.Result> GetWorkflowChainIdAsync(GetWorkflowChainId.Arguments opArgs);
        Task<DescribeWorkflow.Result> DescribeWorkflowAsync(DescribeWorkflow.Arguments opArgs);
        Task<SignalWorkflow.Result> SignalWorkflowAsync<TSigArg>(SignalWorkflow.Arguments<TSigArg> opArgs);
        Task<QueryWorkflow.Result> QueryWorkflowAsync<TQryArg>(QueryWorkflow.Arguments<TQryArg> opArgs);
        Task<RequestCancellation.Result> RequestCancellationAsync(RequestCancellation.Arguments opArgs);
        Task<TerminateWorkflow.Result> TerminateWorkflowAsync<TTermArg>(TerminateWorkflow.Arguments<TTermArg> opArgs);
    }
}
