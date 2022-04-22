using System;
using System.Text;

namespace Temporal.WorkflowClient.Errors
{
    internal static class ExceptionMessage
    {
        private const string NextInfoItemPrefix = "; ";
        private const string FirstInfoItemPrefix = " (";
        private const string InfoItemsPostfix = ")";

        public static StringBuilder GetBasis<TEx>(string userMessage, Exception innerException, out int basisLength) where TEx : Exception
        {
            StringBuilder message = new();

            message.Append(String.IsNullOrWhiteSpace(userMessage)
                                ? typeof(TEx).Name
                                : userMessage.Trim());

            if (message.Length > 0 && message[message.Length - 1] != '.')
            {
                message.Append('.');
            }

            if (innerException != null)
            {
                message.Append(" Inner Exception may have additional details.");
            }

            basisLength = message.Length;
            return message;
        }

        public static bool StartNextInfoItemIfRequired(StringBuilder messageBuilder, string userMessage, string infoTag, int basisLength)
        {
            bool isRequired = (userMessage == null) || userMessage.IndexOf(infoTag, StringComparison.OrdinalIgnoreCase) == -1;
            if (!isRequired)
            {
                return false;
            }

            bool isFirstInfoItem = messageBuilder.Length == basisLength;
            messageBuilder.Append(isFirstInfoItem ? FirstInfoItemPrefix : NextInfoItemPrefix);
            return true;
        }

        public static void CompleteInfoItems(StringBuilder messageBuilder, int basisLength)
        {
            if (messageBuilder.Length > basisLength)
            {
                messageBuilder.Append(InfoItemsPostfix);
            }
        }
    }
}
