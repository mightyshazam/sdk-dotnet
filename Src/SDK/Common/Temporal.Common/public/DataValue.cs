using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Candidly.Util;

namespace Temporal.Common
{
    public static partial class DataValue
    {
        public static readonly IDataValue.Void Void = IDataValue.Void.Instance;
        public static readonly Task<IDataValue.Void> VoidTask = IDataValue.Void.CompletedTask;


        #region Named(..)

        public static DataValue.NamedContainers.For1<T1> Named<T1>(string name1, T1 value1)
        {
            return new DataValue.NamedContainers.For1<T1>(name1, value1);
        }

        public static DataValue.NamedContainers.For2<T1, T2> Named<T1, T2>(string name1, T1 value1, string name2, T2 value2)
        {
            return new DataValue.NamedContainers.For2<T1, T2>(name1, value1, name2, value2);
        }

        public static DataValue.NamedContainers.ForN<object> Named(params object[] namedValues)
        {
            return Named<object>(namedValues);
        }

        public static DataValue.NamedContainers.ForN<T> Named<T>(params object[] namedValues)
        {
            Validate.NotNull(namedValues);

            if (namedValues is KeyValuePair<string, T>[] namedValuePairs)
            {
                return Named<T>(namedValuePairs);
            }

            if (namedValues.Length % 2 != 0)
            {
                throw new ArgumentException($"{nameof(namedValues)} must have an even number of elements,"
                                          + $" but it has {namedValues.Length} elements.");
            }

            int count = namedValues.Length / 2;
            Dictionary<string, T> valuesTable = new(capacity: count);

            int v = 0;
            while (v < count)
            {
                object nameObject = namedValues[v * 2];
                object valueObject = namedValues[v * 2 + 1];

                ValidateNameValuePair<T>(v, valuesTable, nameObject, valueObject, out string name, out T value);
                valuesTable.Add(name, value);
                v++;
            }

            return Named<T>(valuesTable);
        }

        public static DataValue.NamedContainers.ForN<object> Named(IEnumerable namedValues)
        {
            return Named<object>(namedValues);
        }

        public static DataValue.NamedContainers.ForN<T> Named<T>(IEnumerable namedValues)
        {
            Validate.NotNull(namedValues);

            if (namedValues is IEnumerable<KeyValuePair<string, T>> namedValuePairs)
            {
                return Named<T>(namedValuePairs);
            }

            Dictionary<string, T> valuesTable = new();

            int v = 0;
            IEnumerator namedValuesEnum = namedValues.GetEnumerator();
            while (namedValuesEnum.MoveNext())
            {
                object nameObject = namedValuesEnum.Current;

                if (!namedValuesEnum.MoveNext())
                {
                    throw new ArgumentException($"{nameof(namedValues)} must have an even number of elements,"
                                              + $" but it has {v * 2 + 1} elements.");
                }

                object valueObject = namedValuesEnum.Current;

                ValidateNameValuePair<T>(v, valuesTable, nameObject, valueObject, out string name, out T value);
                valuesTable.Add(name, value);
                v++;
            }

            return Named<T>(valuesTable);
        }

        public static DataValue.NamedContainers.ForN<T> Named<T>(params KeyValuePair<string, T>[] namedValues)
        {
            Validate.NotNull(namedValues);

            int count = namedValues.Length;
            Dictionary<string, T> valuesTable = new(capacity: count);

            int v = 0;
            while (v < count)
            {
                string name = namedValues[v].Key;
                T value = namedValues[v].Value;

                ValidateNameValuePair<T>(v, valuesTable, name);
                valuesTable.Add(name, value);
                v++;
            }

            return Named<T>(valuesTable);
        }

        public static DataValue.NamedContainers.ForN<T> Named<T>(IEnumerable<KeyValuePair<string, T>> namedValues)
        {
            Validate.NotNull(namedValues);

            if (namedValues is IReadOnlyDictionary<string, T> readyValuesTable)
            {
                return Named<T>(readyValuesTable);
            }

            Dictionary<string, T> valuesTable = new();

            int v = 0;
            IEnumerator<KeyValuePair<string, T>> namedValuesEnum = namedValues.GetEnumerator();
            while (namedValuesEnum.MoveNext())
            {
                string name = namedValuesEnum.Current.Key;
                T value = namedValuesEnum.Current.Value;

                ValidateNameValuePair<T>(v, valuesTable, name);
                valuesTable.Add(name, value);
                v++;
            }

            return Named<T>(valuesTable);
        }

        public static DataValue.NamedContainers.ForN<T> Named<T>(IReadOnlyDictionary<string, T> namedValues)
        {
            return new DataValue.NamedContainers.ForN<T>(namedValues);
        }

        #endregion Named(..)

        #region Unamed(..)

        public static DataValue.UnnamedContainers.For1<T1> Unnamed<T1>(T1 value1)
        {
            return new DataValue.UnnamedContainers.For1<T1>(value1);
        }

        public static DataValue.UnnamedContainers.For2<T1, T2> Unnamed<T1, T2>(T1 value1, T2 value2)
        {
            return new DataValue.UnnamedContainers.For2<T1, T2>(value1, value2);
        }

        public static DataValue.UnnamedContainers.ForN<T> Unnamed<T>(params T[] values)
        {
            return new DataValue.UnnamedContainers.ForN<T>(values);
        }

        public static DataValue.UnnamedContainers.ForN<T> Unnamed<T>(IEnumerable<T> values)
        {
            Validate.NotNull(values);

            if (values is IReadOnlyList<T> readyList)
            {
                return Unnamed<T>(readyList);
            }

            List<T> valsList = new();
            foreach (T val in values)
            {
                valsList.Add(val);
            }

            return Unnamed<T>(valsList);
        }

        public static DataValue.UnnamedContainers.ForN<T> Unnamed<T>(IReadOnlyList<T> values)
        {
            return new DataValue.UnnamedContainers.ForN<T>(values);
        }

        #endregion Unamed(..)
    }
}
