using Temporal.Api.Common.V1;
using Temporal.Api.Enums.V1;

namespace Temporal.WorkflowClient.OperationConfigurations
{
    public record QueryWorkflowConfiguration(Header Header,
                                             QueryRejectCondition QueryRejectCondition)
    {
        private static readonly QueryWorkflowConfiguration s_default = new();

        public static QueryWorkflowConfiguration Default
        {
            get { return s_default; }
        }

        public QueryWorkflowConfiguration()
            : this(Header: null,
                   QueryRejectCondition.None)
        {
        }
    }
}
