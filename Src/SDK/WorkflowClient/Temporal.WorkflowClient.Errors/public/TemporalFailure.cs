using System;
using Candidly.Util;
using Temporal.Api.Failure.V1;

namespace Temporal.WorkflowClient.Errors
{
    public static class TemporalFailure
    {
        public static Exception FromMessage(Failure failure)
        {
            throw new NotImplementedException(@"ToDo");
        }

        //public static ITemporalFailure GetInnerTemporalFailure(this RemoteTemporalException rtEx)
        //{
        //    Validate.NotNull(rtEx);
        //    return rtEx.InnerException.Cast<Exception, ITemporalFailure>();
        //}

        public static ITemporalFailure GetInnerTemporalFailure(this WorkflowConcludedAbnormallyException rtEx)
        {
            Validate.NotNull(rtEx);
            return rtEx.InnerException.Cast<Exception, ITemporalFailure>();
        }
    }
}
