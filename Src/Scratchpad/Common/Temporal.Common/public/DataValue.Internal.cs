using System;
using System.Collections.Generic;

namespace Temporal.Common
{
    public static partial class DataValue
    {
        private static Type GetTypeOfValue<T>(T value)
        {
            return (value == null) ? typeof(T) : value.GetType();
        }

        private static void ValidateNameValuePair<T>(int pairIndex,
                                                     IReadOnlyDictionary<string, T> prevNames,
                                                     object nameObject,
                                                     object valueObject,
                                                     out string name,
                                                     out T value)
        {
            const string NamedValuesParamName = "namedValues";

            if (nameObject == null)
            {
                throw new ArgumentException($"{NamedValuesParamName}[{pairIndex * 2}] is expected to be a String denoting"
                                          + $" the name of the value #{pairIndex}, but it is null.");
            }

            if (nameObject is not string nm)
            {
                throw new ArgumentException($"{NamedValuesParamName}[{pairIndex * 2}] is expected to be a String denoting"
                                          + $" the name of the value #{pairIndex}, but it is an instance"
                                          + $" of type \"{nameObject.GetType().FullName}\", which is not a String.");
            }

            name = nm;

            if (valueObject is not T val)
            {
                throw new ArgumentException($"{NamedValuesParamName}[{pairIndex * 2 + 1}] is expected to be an instance of"
                                          + $" type \"{typeof(T).FullName}\" representing the value #{pairIndex},"
                                          + $" but instead it is "
                                          + (valueObject == null ? "null" : $"an instance of type \"{valueObject.GetType().FullName}\"")
                                          + ".");
            }

            value = val;

            ValidateNameValuePair(pairIndex, prevNames, name);
        }

        private static void ValidateNameValuePair<T>(int pairIndex,
                                                     IReadOnlyDictionary<string, T> prevNames,
                                                     string name)
        {
            const string NamedValuesParamName = "namedValues";

            if (name == null)
            {
                throw new ArgumentException($"{NamedValuesParamName}[{pairIndex * 2}] is expected to be a String denoting"
                                          + $" the name of the value #{pairIndex}, but it is null.");
            }

            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"{NamedValuesParamName}[{pairIndex * 2}] is expected to be a not empty-or-whitespace"
                                          + $" String denoting the name of the value #{pairIndex}, but \"{name}\" was found.");
            }

            if (prevNames != null && prevNames.ContainsKey(name))
            {
                throw new ArgumentException($"{NamedValuesParamName} must not have any duplicate names, but the"
                                          + $" name \"{name}\" occurs more than once (position {pairIndex * 2}.");
            }
        }
    }
}
