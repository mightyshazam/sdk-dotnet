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
        Task<DescribeWorkflowRun.Result> DescribeWorkflowRunAsync(DescribeWorkflowRun.Arguments opArgs);
    }
}
