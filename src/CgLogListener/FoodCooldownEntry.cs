using System;

namespace CgLogListener
{
    public sealed class FoodCooldownEntry
    {
        public FoodCooldownEntry(string characterName, DateTime detectedAt, string sourceLine, TimeSpan cooldown)
        {
            CharacterName = characterName;
            DetectedAt = detectedAt;
            SourceLine = sourceLine;
            AvailableAt = detectedAt.Add(cooldown);
        }

        public string CharacterName { get; }
        public DateTime DetectedAt { get; }
        public DateTime AvailableAt { get; }
        public string SourceLine { get; }
    }
}
