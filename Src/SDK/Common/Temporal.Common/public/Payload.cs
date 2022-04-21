using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Temporal.Util;

using Temporal.Common.Payloads;

namespace Temporal.Common
{
    public static partial class Payload
    {
        public static readonly IPayload.Void Void = IPayload.Void.Instance;
        public static readonly Task<IPayload.Void> VoidTask = IPayload.Void.CompletedTask;

        #region Named(..)

        public static object Named<T>(params object[] namedValues)
        {
            // This method is currently used in nameof(..) clauses when generating descriptive messages.
            // @ToDo: A full implementation will be added in later iterations.
            throw new NotImplementedException("@ToDo");
        }

        #endregion Named(..)

        #region Unnamed(..)

        public static PayloadContainers.Unnamed.InstanceBacked<object> Unnamed(params object[] values)
        {
            return Payload.Unnamed<object>(values);
        }

        public static PayloadContainers.Unnamed.InstanceBacked<T> Unnamed<T>(params T[] values)
        {
            return Payload.Unnamed((IReadOnlyList<T>) values);
        }

        public static PayloadContainers.Unnamed.InstanceBacked<T> Unnamed<T>(IEnumerable<T> values)
        {
            Validate.NotNull(values);

            if (values is IReadOnlyList<T> readyList)
            {
                return Payload.Unnamed<T>(readyList);
            }

            List<T> valsList = new();
            foreach (T val in values)
            {
                valsList.Add(val);
            }

            return Payload.Unnamed<T>(valsList);
        }

        public static PayloadContainers.Unnamed.InstanceBacked<T> Unnamed<T>(IReadOnlyList<T> values)
        {
            return new PayloadContainers.Unnamed.InstanceBacked<T>(values);
        }

        #endregion Unnamed(..)
    }
}
