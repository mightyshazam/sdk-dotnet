using System;
using System.Text;

namespace Temporal.WorkflowClient
{
    /// <summary>
    /// Indicates that the SDK did not expect to find something particular in a payload from the server.
    /// Typically, this indicates an SDK bug. These are thrown as a form of defensive programming.
    /// Please report if you encounter this in a production release:
    /// https://github.com/temporalio/sdk-dotnet/issues
    /// </summary>
    public class MalformedServerResponseException : Exception
    {
        private static string FormatMessage(string serverCall, string scenario, string problemDescription)
        {
            static void AppendSentenceEndIfRequired(StringBuilder text)
            {
                if (text.Length > 0)
                {
                    if (text[text.Length - 1] == '.')
                    {
                        text.Append(' ');
                    }
                    else
                    {
                        text.Append(". ");
                    }
                }
            }

            StringBuilder message = new();

            if (!String.IsNullOrWhiteSpace(problemDescription))
            {
                message.Append(problemDescription);
            }

            if (!String.IsNullOrWhiteSpace(serverCall))
            {
                AppendSentenceEndIfRequired(message);
                message.Append("Server Call: \"");
                message.Append(serverCall);
                message.Append("\".");
            }

            if (!String.IsNullOrWhiteSpace(scenario))
            {
                AppendSentenceEndIfRequired(message);
                message.Append("Scenario: \"");
                message.Append(scenario);
                message.Append("\".");
            }

            return message.ToString();
        }

        internal MalformedServerResponseException(string serverCall, string scenario, string problemDescription)
            : base(FormatMessage(serverCall, scenario, problemDescription))
        {
        }

        internal MalformedServerResponseException(string serverCall, string scenario, Exception unexpectedException)
            : base(FormatMessage(serverCall, scenario, $"Unexpected exception occurred ({unexpectedException.GetType().Name})"))
        {
        }
    }
}
