using System;

namespace CgLogViewer
{
    public enum OpenAIReasoningEffort
    {
        Low,
        Medium,
        High
    }

    public static class OpenAIReasoningEffortHelper
    {
        public static string ToApiValue(OpenAIReasoningEffort effort)
        {
            switch (effort)
            {
                case OpenAIReasoningEffort.High:
                    return "high";
                case OpenAIReasoningEffort.Low:
                    return "low";
                default:
                    return "medium";
            }
        }

        public static TimeSpan GetTimeout(OpenAIReasoningEffort effort)
        {
            switch (effort)
            {
                case OpenAIReasoningEffort.High:
                    return TimeSpan.FromSeconds(75);
                case OpenAIReasoningEffort.Low:
                    return TimeSpan.FromSeconds(25);
                default:
                    return TimeSpan.FromSeconds(45);
            }
        }

        public static string GetLabel(OpenAIReasoningEffort effort)
        {
            switch (effort)
            {
                case OpenAIReasoningEffort.High:
                    return "High";
                case OpenAIReasoningEffort.Low:
                    return "Low";
                default:
                    return "Medium";
            }
        }
    }
}
