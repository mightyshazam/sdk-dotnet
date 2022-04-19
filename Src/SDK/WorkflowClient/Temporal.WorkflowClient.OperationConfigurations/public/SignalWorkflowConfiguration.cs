using Temporal.Api.Common.V1;

namespace Temporal.WorkflowClient.OperationConfigurations
{
    public class SignalWorkflowConfiguration
    {
        private static readonly SignalWorkflowConfiguration s_default = new SignalWorkflowConfiguration()
        {
            Header = null
        };

        public static SignalWorkflowConfiguration Default
        {
            get { return s_default; }
        }

        public Header Header { get; init; }
    }
}
