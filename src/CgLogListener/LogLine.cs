using System;
using System.Text.RegularExpressions;

namespace CgLogListener
{
    public enum LogCategory
    {
        Guild,
        System,
        Party,
        Normal,
        Other
    }

    public sealed class LogLine
    {
        static readonly Regex guildPrefixRegex = new Regex(@"^\[家族(?: [^\]]+)?\]", RegexOptions.Compiled);
        static readonly Regex partyPrefixRegex = new Regex(@"^\[GP\]", RegexOptions.Compiled);
        static readonly Regex speakerRegex = new Regex(@"^(?<speaker>[^\[\]:\s][^:\s]{0,31}):\s+", RegexOptions.Compiled);
        static readonly Regex asciiSystemLeadRegex = new Regex(@"^[A-Za-z0-9._-]{2,}[\u3000-\u30FF\u3400-\u9FFF\uF900-\uFAFF\uFF00-\uFFEF]", RegexOptions.Compiled);

        public LogLine(DateTime timestamp, string message, string sourceFile, LogCategory category)
        {
            Timestamp = timestamp;
            Message = message;
            SourceFile = sourceFile;
            Category = category;
        }

        public DateTime Timestamp { get; }
        public string Message { get; }
        public string SourceFile { get; }
        public LogCategory Category { get; }
        public string DisplayLine => $"{Timestamp:HH:mm:ss} {Message}";

        public static LogCategory ClassifyMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return LogCategory.Other;
            }

            if (guildPrefixRegex.IsMatch(message))
            {
                return LogCategory.Guild;
            }

            if (partyPrefixRegex.IsMatch(message))
            {
                return LogCategory.Party;
            }

            if (speakerRegex.IsMatch(message))
            {
                return LogCategory.Normal;
            }

            if (LooksLikeSystemMessage(message))
            {
                return LogCategory.System;
            }

            return LogCategory.Other;
        }

        static bool LooksLikeSystemMessage(string message)
        {
            char first = message[0];
            return message.IndexOf('：') >= 0 ||
                asciiSystemLeadRegex.IsMatch(message) ||
                IsCjkOrFullWidth(first) ||
                IsSystemSymbol(first);
        }

        static bool IsCjkOrFullWidth(char c)
        {
            return (c >= '\u3000' && c <= '\u30FF') ||
                (c >= '\u3400' && c <= '\u9FFF') ||
                (c >= '\uF900' && c <= '\uFAFF') ||
                (c >= '\uFF00' && c <= '\uFFEF');
        }

        static bool IsSystemSymbol(char c)
        {
            return (c >= '\u2500' && c <= '\u257F') ||
                c == '■' ||
                c == '『' ||
                c == '「' ||
                c == '【' ||
                c == '（' ||
                c == '(';
        }
    }

    public sealed class LogLineEventArgs : EventArgs
    {
        public LogLineEventArgs(LogLine logLine)
        {
            LogLine = logLine;
        }

        public LogLine LogLine { get; }
    }
}
