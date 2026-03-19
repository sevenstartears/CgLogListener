using System.Collections.Generic;
using System.Linq;

namespace CgLogListener
{
    public sealed class NotificationPreset
    {
        public NotificationPreset(string key, string label, string pattern, LogCategory? requiredCategory = null)
        {
            Key = key;
            Label = label;
            Pattern = pattern;
            RequiredCategory = requiredCategory;
        }

        public string Key { get; }
        public string Label { get; }
        public string Pattern { get; }
        public LogCategory? RequiredCategory { get; }
        public string DisplayLabel => (Label ?? string.Empty).Replace("通知", string.Empty);
    }

    public static class NotificationPresets
    {
        public static IReadOnlyList<NotificationPreset> All { get; } = new[]
        {
            new NotificationPreset("Health", "採集怪我通知", "在工作時不小心受傷了。"),
            new NotificationPreset("ItemFull", "カバン空きなし通知", "物品欄沒有空位。"),
            new NotificationPreset("MP0", "FP切れ通知", "魔力不足。"),
            new NotificationPreset("PlayerJoin", "パーティ加入通知", "加入了(你|您)的隊伍。"),
            new NotificationPreset("Sell", "露店販売通知", "您順利賣掉了一個.*，(收入|獲得).*魔幣！"),
            new NotificationPreset("ReMaze", "ダンジョン再構築通知", "你感覺到一股不可思議的力量，而『.*』好像快(要?)消失了。"),
            new NotificationPreset("AnzanBreak", "安産壊れ", "^(?=.*安產)(?=.*壞掉了).+$"),
            new NotificationPreset("MmReturn", "MM帰還", "回來了。", LogCategory.System),
        };

        public static NotificationPreset Find(string key)
        {
            return All.FirstOrDefault(p => p.Key == key);
        }
    }
}
