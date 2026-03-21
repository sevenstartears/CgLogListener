using System.Drawing;

namespace CgLogViewer
{
    public static class LogCategoryPalette
    {
        public static string GetLabel(LogCategory category)
        {
            switch (category)
            {
                case LogCategory.Guild:
                    return "ギルド";
                case LogCategory.System:
                    return "システム";
                case LogCategory.Party:
                    return "パーティ";
                case LogCategory.Normal:
                    return "通常";
                case LogCategory.Npc:
                    return "NPC";
                default:
                    return "その他";
            }
        }

        public static Color GetAccentColor(LogCategory category)
        {
            switch (category)
            {
                case LogCategory.Guild:
                    return Color.FromArgb(40, 132, 92);
                case LogCategory.System:
                    return Color.FromArgb(20, 84, 140);
                case LogCategory.Party:
                    return Color.FromArgb(177, 123, 24);
                case LogCategory.Normal:
                    return Color.FromArgb(150, 77, 47);
                case LogCategory.Npc:
                    return Color.FromArgb(133, 65, 179);
                default:
                    return Color.FromArgb(101, 111, 123);
            }
        }

        public static Color GetSurfaceColor(LogCategory category)
        {
            switch (category)
            {
                case LogCategory.Guild:
                    return Color.FromArgb(237, 249, 243);
                case LogCategory.System:
                    return Color.FromArgb(236, 244, 255);
                case LogCategory.Party:
                    return Color.FromArgb(255, 247, 232);
                case LogCategory.Normal:
                    return Color.FromArgb(253, 241, 236);
                case LogCategory.Npc:
                    return Color.FromArgb(247, 239, 255);
                default:
                    return Color.FromArgb(244, 247, 251);
            }
        }
    }
}
