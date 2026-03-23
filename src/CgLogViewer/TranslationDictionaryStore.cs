using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CgLogViewer
{
    public sealed class TranslationDictionaryStore
    {
        public const string DefaultFileName = "translation_dictionary.tsv";
        public const string UserFileName = "translation_dictionary.user.tsv";
        static readonly Lazy<TranslationDictionaryStore> instance = new Lazy<TranslationDictionaryStore>(() => new TranslationDictionaryStore());

        public static TranslationDictionaryStore Instance => instance.Value;

        public string FilePath => Path.Combine(Directory.GetCurrentDirectory(), UserFileName);
        public string DefaultFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultFileName);

        public IReadOnlyList<TranslationDictionaryEntry> LoadEntries()
        {
            EnsureFileExists();

            var entries = new List<TranslationDictionaryEntry>();
            foreach (var line in File.ReadAllLines(FilePath, Encoding.UTF8).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = line.Split('\t');
                if (parts.Length < 2)
                {
                    continue;
                }

                var entry = new TranslationDictionaryEntry
                {
                    SourceTerm = (parts.ElementAtOrDefault(0) ?? string.Empty).Trim(),
                    TargetTerm = (parts.ElementAtOrDefault(1) ?? string.Empty).Trim(),
                    Category = (parts.ElementAtOrDefault(2) ?? string.Empty).Trim(),
                };

                if (!string.IsNullOrWhiteSpace(entry.SourceTerm) && !string.IsNullOrWhiteSpace(entry.TargetTerm))
                {
                    entries.Add(entry);
                }
            }

            return entries
                .GroupBy(entry => entry.SourceTerm, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderByDescending(entry => entry.SourceTerm.Length)
                .ThenBy(entry => entry.SourceTerm, StringComparer.Ordinal)
                .ToList();
        }

        public void SaveEntries(IEnumerable<TranslationDictionaryEntry> entries)
        {
            EnsureFileExists();

            var normalizedEntries = (entries ?? Array.Empty<TranslationDictionaryEntry>())
                .Where(entry => entry != null)
                .Select(entry => new TranslationDictionaryEntry
                {
                    SourceTerm = (entry.SourceTerm ?? string.Empty).Trim(),
                    TargetTerm = (entry.TargetTerm ?? string.Empty).Trim(),
                    Category = (entry.Category ?? string.Empty).Trim(),
                })
                .Where(entry => !string.IsNullOrWhiteSpace(entry.SourceTerm) && !string.IsNullOrWhiteSpace(entry.TargetTerm))
                .GroupBy(entry => entry.SourceTerm, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(entry => entry.Category, StringComparer.Ordinal)
                .ThenBy(entry => entry.SourceTerm, StringComparer.Ordinal)
                .ToList();

            var lines = new List<string> { "source\ttarget\tcategory" };
            lines.AddRange(normalizedEntries.Select(entry =>
                string.Join("\t",
                    Escape(entry.SourceTerm),
                    Escape(entry.TargetTerm),
                    Escape(entry.Category))));

            File.WriteAllLines(FilePath, lines, new UTF8Encoding(false));
        }

        public string ComputeSignature(IEnumerable<TranslationDictionaryEntry> entries)
        {
            return string.Join("\n", (entries ?? Array.Empty<TranslationDictionaryEntry>())
                .Select(entry => $"{entry.SourceTerm}\t{entry.TargetTerm}")
                .OrderBy(line => line, StringComparer.Ordinal));
        }

        void EnsureFileExists()
        {
            if (File.Exists(FilePath))
            {
                return;
            }

            if (File.Exists(DefaultFilePath))
            {
                File.Copy(DefaultFilePath, FilePath, false);
                return;
            }

            File.WriteAllLines(FilePath, new[] { "source\ttarget\tcategory" }, new UTF8Encoding(false));
        }

        static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\t", " ")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }
    }
}
