using System;

namespace CgLogListener
{
    public sealed class DisplayedLogGroup
    {
        public DisplayedLogGroup(LogLine initialLog)
        {
            Message = initialLog.Message;
            Category = initialLog.Category;
            DisplayTimestamp = initialLog.Timestamp;
            LastTimestamp = initialLog.Timestamp;
            Count = 1;
        }

        public string Message { get; }
        public LogCategory Category { get; }
        public DateTime DisplayTimestamp { get; }
        public DateTime LastTimestamp { get; private set; }
        public int Count { get; private set; }
        public string DisplayLine => $"{DisplayTimestamp:HH:mm:ss} {Message}";

        public void Merge(LogLine log)
        {
            LastTimestamp = log.Timestamp;
            Count++;
        }
    }
}
