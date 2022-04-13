using System;
using System.Threading;
using System.Threading.Tasks;

namespace Temporal.WorkflowClient.Interceptors
{
    public interface ITemporalClientInterceptor : IDisposable
    {
        void Init(ITemporalClientInterceptor nextInterceptor);

        Task<StartWorkflowResult> StartWorkflowAsync<TWfArg>(string @namespace,
                                                             string workflowId,
                                                             string workflowTypeName,
                                                             string taskQueue,
                                                             TWfArg workflowArg,
                                                             StartWorkflowChainConfiguration workflowConfig,
                                                             bool throwOnAlreadyExists,
                                                             CancellationToken cancelToken);

        Task<IWorkflowRunResult> AwaitConclusionAsync(string @namespace,
                                                      string workflowId,
                                                      string workflowChainId,
                                                      string workflowRunId,
                                                      bool followChain,
                                                      CancellationToken cancelToken);

        Task<string> GetLatestWorkflowChainId(string @namespace,
                                              string workflowId,
                                              CancellationToken cancelToken);
    }
}
