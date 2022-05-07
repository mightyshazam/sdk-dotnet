using Temporal.Api.Common.V1;

namespace Temporal.WorkflowClient.OperationConfigurations
{
    public record SignalWorkflowConfiguration(Header Header)
    {
        private static readonly SignalWorkflowConfiguration s_default = new();

        public static SignalWorkflowConfiguration Default
        {
            get { return s_default; }
        }

        public SignalWorkflowConfiguration()
            : this(Header: null)
        {
        }
    }
}
