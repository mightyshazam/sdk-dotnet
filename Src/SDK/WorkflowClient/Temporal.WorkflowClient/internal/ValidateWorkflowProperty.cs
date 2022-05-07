using System;
using Temporal.Util;

namespace Temporal.WorkflowClient
{
    internal class ValidateWorkflowProperty
    {
        public static void WorkflowId(string workflowId)
        {
            if (String.IsNullOrWhiteSpace(workflowId))
            {
                throw new ArgumentException($"{nameof(workflowId)} must be a non-empty-or-whitespace string."
                                          + $" However, {Format.QuoteOrNull(workflowId)} was specified.");
            }
        }

        public static class ChainId
        {
            public static void BoundOrUnbound(string workflowChainId)
            {
                if (workflowChainId != null && String.IsNullOrWhiteSpace(workflowChainId))
                {
                    throw new ArgumentException($"{nameof(workflowChainId)} must be either null (if unbound),"
                                              + $" or a non-empty-or-whitespace string (if bound)."
                                              + $" However, {Format.QuoteOrNull(workflowChainId)} was specified.");
                }
            }

            public static void Bound(string workflowChainId)
            {
                if (String.IsNullOrWhiteSpace(workflowChainId))
                {
                    throw new ArgumentException($"A bound {nameof(workflowChainId)} must be a non-empty-or-whitespace string."
                                              + $" However, {Format.QuoteOrNull(workflowChainId)} was specified.");
                }
            }

            public static void Unbound(string workflowChainId)
            {
                if (workflowChainId != null)
                {
                    throw new ArgumentException($"An unbound {nameof(workflowChainId)} must be a null."
                                              + $" However, {Format.QuoteOrNull(workflowChainId)} was specified.");
                }
            }
        }

        public static class RunId
        {
            public static void SpecifiedOrUnspecified(string workflowRunId)
            {
                if (workflowRunId != null && String.IsNullOrWhiteSpace(workflowRunId))
                {
                    throw new ArgumentException($"{nameof(workflowRunId)} must be either null (if unspecified),"
                                              + $" or a non-empty-or-whitespace string (if specified)."
                                              + $" However, {Format.QuoteOrNull(workflowRunId)} was specified.");
                }
            }

            public static void Specified(string workflowRunId)
            {
                if (String.IsNullOrWhiteSpace(workflowRunId))
                {
                    throw new ArgumentException($"A specified {nameof(workflowRunId)} must be a non-empty-or-whitespace string."
                                              + $" However, {Format.QuoteOrNull(workflowRunId)} was specified.");
                }
            }

            public static void Unspecified(string workflowRunId)
            {
                if (workflowRunId != null)
                {
                    throw new ArgumentException($"An unspecified {nameof(workflowRunId)} must be a null."
                                              + $" However, {Format.QuoteOrNull(workflowRunId)} was specified.");
                }
            }
        }
    }
}
