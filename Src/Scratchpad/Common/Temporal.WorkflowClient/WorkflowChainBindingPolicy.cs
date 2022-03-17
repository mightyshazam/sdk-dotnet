using System;
using System.ComponentModel;

namespace Temporal.WorkflowClient
{
    public enum WorkflowChainBindingPolicy
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        Unspecified = 0,

        /// <summary>            
        /// Binding initiators: ANY chain API can be called on an unbound chain handle to cause binding.
        /// Strategy: The underlying gRPC call will only contain the workflowId, and thus the latest run (active or not)
        ///   will be addressed. The gRPC API will return the workflowChainId of the run that has been addressed.
        ///   The chain handle will be bound to that chain and all subsequent gRPC API calls will include that workflowChainId
        ///   to always address the latest run within that chain, but never a run beyond that chain.
        /// </summary>
        LatestChain = 1,

        /// <summary>            
        /// Binding initiators: ONLY Start-Workflow APIs (or the Main Workflow Method Stub) can be called on an unbound handle.
        ///   Calling other APIs *before* binding will throw.
        /// Requirements: Data required to create workflows must be available (workflow type, queue, etc.)
        /// Strategy: Call the Start Workflow gRPC API. If it succeeds, bind to the workflowChainId returned by that API.
        ///   If the Start Workflow gRPC API call fails with "workflow already exists", then look up the latest chain 
        ///   by using Describe Run API and specifying only workflowId. Otherwise binding failed - propagate the error.
        /// </summary>
        NewOrLatestChain = 2,

        /// <summary>
        /// Binding initiators: ONLY Start-Workflow APIs (or the Main Workflow Method Stub) can be called on an unbound handle.
        ///   Calling other APIs *before* binding will throw.
        /// Requirements: Unbound handle must be initialized with data required to create workflows (workflow type, queue, etc.)
        /// Strategy: Call the Start Workflow gRPC API. Bind to the workflowChainId returned by that API.
        ///   If the Start Workflow gRPC API call above fails, then binding failed - propagate the error.
        /// </summary>
        NewChainOnly = 3
    }
}
