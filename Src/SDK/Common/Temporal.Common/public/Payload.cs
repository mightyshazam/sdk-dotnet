using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Candidly.Util;

using Temporal.Common.Payloads;

namespace Temporal.Common
{
    public static partial class Payload
    {
        public static readonly IPayload.Void Void = IPayload.Void.Instance;
        public static readonly Task<IPayload.Void> VoidTask = IPayload.Void.CompletedTask;

        #region Unnamed(..)

        public static object Named<T>(params object[] namedBalues)
        {
            throw new NotImplementedException("@ToDo");
        }

        #endregion Unnamed(..)

        #region Unnamed(..)

        public static PayloadContainers.ForUnnamedValues.InstanceBacked<T> Unnamed<T>(params T[] values)
        {
            return Payload.Unnamed((IReadOnlyList<T>) values);
        }

        public static PayloadContainers.ForUnnamedValues.InstanceBacked<T> Unnamed<T>(IEnumerable<T> values)
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

        public static PayloadContainers.ForUnnamedValues.InstanceBacked<T> Unnamed<T>(IReadOnlyList<T> values)
        {
            return new PayloadContainers.ForUnnamedValues.InstanceBacked<T>(values);
        }

        #endregion Unnamed(..)
    }
}
