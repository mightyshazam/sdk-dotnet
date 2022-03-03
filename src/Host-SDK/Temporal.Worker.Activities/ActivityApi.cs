using System;
using System.Threading.Tasks;

using Temporal.Common.DataModel;
using Temporal.Serialization;

namespace Temporal.Worker.Activities
{
    public class ActivityAPi
    {
    }

    // =========== Basic activity abstractions ===========

    public interface IBasicActivity
    {
        string ActivityTypeName { get; }

        Task<PayloadsCollection> RunAsync(PayloadsCollection input, WorkflowActivityContext workflowCtx);        
    }

    public abstract class BasicActivityBase : IBasicActivity
    {
        private string _activityTypeName = null;

        public virtual string ActivityTypeName
        {
            get
            {
                if (_activityTypeName == null)
                {
                    string implTypeName = this.GetType().Name;
                    _activityTypeName = implTypeName.EndsWith("Activity") ? implTypeName.Substring(implTypeName.Length - "Activity".Length) : implTypeName;
                }

                return _activityTypeName;                
            }
        }

        public abstract Task<PayloadsCollection> RunAsync(PayloadsCollection input, WorkflowActivityContext activityCtx);
    }

    /// <summary>Wanted to name it <c>ActivityContext</c>, but there is a likely annoying
    /// clash with <c>System.Diagnostics.ActivityContext</c>.</summary>
    public class WorkflowActivityContext
    {
        public string ActivityTypeName { get; }

        /// <summary>PREVIOUS: Get the serializer for the specified payload.
        /// If metadata specifies an available serializer - get that one;
        /// If metadata specifies an unavailable serializer - throw;
        /// If metadata specified nothing - get the default form the config.
        /// NOW: Redesign DI for this.</summary>        
        public IDataConverter GetDataConverter() { return null; }
    }
}
