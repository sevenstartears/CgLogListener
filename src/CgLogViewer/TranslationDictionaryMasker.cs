using System;
using System.Collections.Generic;
using System.Linq;

namespace CgLogViewer
{
    public sealed class TranslationDictionaryMaskResult
    {
        public string MaskedText { get; set; } = string.Empty;
        public IReadOnlyList<TranslationDictionaryEntry> MatchedEntries { get; set; } = Array.Empty<TranslationDictionaryEntry>();
        public IReadOnlyDictionary<string, string> PlaceholderMap { get; set; } = new Dictionary<string, string>();
    }

    public static class TranslationDictionaryMasker
    {
        public static TranslationDictionaryMaskResult Mask(string text, IEnumerable<TranslationDictionaryEntry> entries)
        {
            string current = text ?? string.Empty;
            var matchedEntries = new List<TranslationDictionaryEntry>();
            var placeholderMap = new Dictionary<string, string>(StringComparer.Ordinal);
            int index = 0;

            foreach (var entry in (entries ?? Array.Empty<TranslationDictionaryEntry>())
                .Where(entry => entry != null &&
                    !string.IsNullOrWhiteSpace(entry.SourceTerm) &&
                    !string.IsNullOrWhiteSpace(entry.TargetTerm))
                .OrderByDescending(entry => entry.SourceTerm.Length))
            {
                if (current.IndexOf(entry.SourceTerm, StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                string placeholder = $"__CGTERM_{index:0000}__";
                current = current.Replace(entry.SourceTerm, placeholder);
                placeholderMap[placeholder] = entry.TargetTerm;
                matchedEntries.Add(entry);
                index++;
            }

            return new TranslationDictionaryMaskResult
            {
                MaskedText = current,
                MatchedEntries = matchedEntries,
                PlaceholderMap = placeholderMap
            };
        }

        public static string Restore(string text, IReadOnlyDictionary<string, string> placeholderMap)
        {
            string current = text ?? string.Empty;
            if (placeholderMap == null)
            {
                return current;
            }

            foreach (var pair in placeholderMap)
            {
                current = current.Replace(pair.Key, pair.Value);
            }

            return current;
        }
    }
}
