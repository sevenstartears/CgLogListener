using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CgLogListener
{
    public sealed class FoodCooldownTracker
    {
        static readonly Regex assistedFoodRegex = new Regex(@"^(?<helper>.+?)替(?<target>.+?)恢復了\d+點?魔力$", RegexOptions.Compiled);
        static readonly Regex selfFoodRegex = new Regex(@"^(?<target>.+?)恢復了\d+點?魔力$", RegexOptions.Compiled);

        static readonly TimeSpan readyRetention = TimeSpan.FromSeconds(20);
        readonly Dictionary<string, FoodCooldownEntry> entries = new Dictionary<string, FoodCooldownEntry>(StringComparer.OrdinalIgnoreCase);
        readonly object syncRoot = new object();

        public static readonly TimeSpan CooldownDuration = TimeSpan.FromMinutes(3);

        public event EventHandler Changed;

        public bool TryRegister(LogLine logLine)
        {
            if (logLine == null || !TryExtractTargetCharacter(logLine.Message, out var targetCharacter))
            {
                return false;
            }

            lock (syncRoot)
            {
                entries[targetCharacter] = new FoodCooldownEntry(targetCharacter, logLine.Timestamp, logLine.DisplayLine, CooldownDuration);
                PruneExpiredEntries(logLine.Timestamp);
            }

            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public IReadOnlyList<FoodCooldownEntry> GetSnapshot(DateTime now)
        {
            lock (syncRoot)
            {
                PruneExpiredEntries(now);
                return entries.Values
                    .OrderBy(entry => entry.AvailableAt)
                    .ThenBy(entry => entry.CharacterName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        public void Reset()
        {
            lock (syncRoot)
            {
                entries.Clear();
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        static bool TryExtractTargetCharacter(string message, out string targetCharacter)
        {
            targetCharacter = null;
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var assistedMatch = assistedFoodRegex.Match(message);
            if (assistedMatch.Success)
            {
                targetCharacter = assistedMatch.Groups["target"].Value.Trim();
                return !string.IsNullOrWhiteSpace(targetCharacter);
            }

            var selfMatch = selfFoodRegex.Match(message);
            if (!selfMatch.Success)
            {
                return false;
            }

            targetCharacter = selfMatch.Groups["target"].Value.Trim();
            return !string.IsNullOrWhiteSpace(targetCharacter);
        }

        void PruneExpiredEntries(DateTime now)
        {
            var expiredKeys = entries
                .Where(pair => pair.Value.AvailableAt.Add(readyRetention) < now)
                .Select(pair => pair.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                entries.Remove(key);
            }
        }
    }
}
