using System;

namespace CgLogViewer
{
    public enum TranslationState
    {
        None,
        InProgress,
        Ready,
        Failed
    }

    public sealed class DisplayedLogGroup
    {
        public DisplayedLogGroup(LogLine initialLog)
        {
            Message = initialLog.Message;
            DeduplicationKey = initialLog.DeduplicationKey;
            Category = initialLog.Category;
            DisplayTimestamp = initialLog.Timestamp;
            LastTimestamp = initialLog.EffectiveTimestamp;
            Count = 1;
        }

        public string Message { get; }
        public string DeduplicationKey { get; }
        public LogCategory Category { get; }
        public DateTime DisplayTimestamp { get; }
        public DateTime LastTimestamp { get; private set; }
        public int Count { get; private set; }
        public string TranslatedMessage { get; private set; }
        public TranslationState TranslationState { get; private set; }
        public bool IsShowingTranslation { get; private set; }
        public bool IsExpandedInSimpleView { get; private set; }
        public string VisibleMessage => IsShowingTranslation && TranslationState == TranslationState.Ready && !string.IsNullOrWhiteSpace(TranslatedMessage)
            ? TranslatedMessage
            : Message;
        public string DisplayLine => $"{DisplayTimestamp:HH:mm:ss} {VisibleMessage}";
        public bool CanToggleTranslation => TranslationState == TranslationState.Ready && !string.IsNullOrWhiteSpace(TranslatedMessage);

        public void Merge(LogLine log)
        {
            LastTimestamp = log.EffectiveTimestamp;
            Count++;
        }

        public void StartTranslation()
        {
            TranslationState = TranslationState.InProgress;
        }

        public void SetTranslation(string translatedMessage)
        {
            TranslatedMessage = translatedMessage?.Trim() ?? string.Empty;
            TranslationState = TranslationState.Ready;
            IsShowingTranslation = true;
        }

        public void MarkTranslationFailed()
        {
            TranslationState = TranslationState.Failed;
            IsShowingTranslation = false;
        }

        public void ToggleTranslation()
        {
            if (!CanToggleTranslation)
            {
                return;
            }

            IsShowingTranslation = !IsShowingTranslation;
        }

        public void ToggleSimpleExpanded()
        {
            IsExpandedInSimpleView = !IsExpandedInSimpleView;
        }
    }
}
