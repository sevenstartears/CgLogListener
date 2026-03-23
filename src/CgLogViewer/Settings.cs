using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace CgLogViewer
{
    public sealed class Settings
    {
        static Settings instance;
        const string settingsFileName = "settings.ini";
        const string settingsBaseSection = "base";
        const string settingsStandardTipsSection = "standard tips";
        const string custmizeFileName = "custmize.dat";
        const int defaultDeduplicationSeconds = 2;
        const int defaultRealtimeDisplayCount = 250;
        const int defaultSimpleViewWidth = 860;
        const int defaultSimpleViewHeight = 620;
        const int defaultSimpleViewRowHeight = 40;
        const float defaultSimpleViewFontSize = 10.5F;
        const bool defaultNpcDialogueAutoAdd = false;
        static readonly OpenAIReasoningEffort defaultOpenAIReasoningEffort = OpenAIReasoningEffort.Medium;

        public bool PlaySound { get; private set; }
        public int SoundVol { get; private set; }
        public string CgLogPath { get; private set; }
        public int DeduplicationSeconds { get; private set; }
        public int RealtimeDisplayCount { get; private set; }
        public bool DiscordNotificationEnabled { get; private set; }
        public string DiscordWebhookUrl { get; private set; }
        public TranslationProvider TranslationProvider { get; private set; }
        public string DeepLApiKey { get; private set; }
        public string DeepLGlossaryId { get; private set; }
        public string GoogleApiKey { get; private set; }
        public string OpenAIApiKey { get; private set; }
        public OpenAIReasoningEffort OpenAIReasoningEffort { get; private set; }
        public int SimpleViewX { get; private set; }
        public int SimpleViewY { get; private set; }
        public int SimpleViewWidth { get; private set; }
        public int SimpleViewHeight { get; private set; }
        public int SimpleViewRowHeight { get; private set; }
        public float SimpleViewFontSize { get; private set; }
        public bool NpcDialogueAutoAdd { get; private set; }
        public Dictionary<string, bool> StandardTips { get; private set; } = new Dictionary<string, bool>();
        public List<string> CustomizeTips { get; private set; } = new List<string>();

        public static Settings GetInstance()
        {
            if (instance == null)
            {
                instance = new Settings();
                instance.Load();
            }

            return instance;
        }

        public void Load()
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), settingsFileName)))
            {
                // gen new conf file
                GenConfigFile();
            }

            // load settings
            LoadSettings();
        }

        private void LoadSettings()
        {
            var fileIniDataParser = new FileIniDataParser();
            var iniData = fileIniDataParser.ReadFile(settingsFileName);
            StandardTips.Clear();
            CustomizeTips.Clear();

            var baseData = iniData[settingsBaseSection];
            CgLogPath = baseData[nameof(CgLogPath)];
            PlaySound = baseData[nameof(PlaySound)] == "1";
            SoundVol = int.Parse(baseData[nameof(SoundVol)]);
            DeduplicationSeconds = int.TryParse(baseData[nameof(DeduplicationSeconds)], out int dedupSeconds)
                ? Math.Max(0, dedupSeconds)
                : defaultDeduplicationSeconds;
            RealtimeDisplayCount = int.TryParse(baseData[nameof(RealtimeDisplayCount)], out int realtimeDisplayCount)
                ? Math.Max(20, realtimeDisplayCount)
                : defaultRealtimeDisplayCount;
            TranslationProvider = Enum.TryParse(baseData[nameof(TranslationProvider)], out TranslationProvider provider)
                ? provider
                : TranslationProvider.DeepL;
            var legacyCustomNotifyTypes = (baseData[nameof(LegacyCustomNotifyTypes)] ?? string.Empty)
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
            DiscordNotificationEnabled = baseData[nameof(DiscordNotificationEnabled)] == "1" ||
                (baseData[nameof(DiscordNotificationEnabled)] == null && legacyCustomNotifyTypes.Contains("Discord"));
            DiscordWebhookUrl = baseData[nameof(DiscordWebhookUrl)] ?? TryLoadLegacyDiscordWebhookUrl();
            DeepLApiKey = baseData[nameof(DeepLApiKey)] ?? string.Empty;
            DeepLGlossaryId = baseData[nameof(DeepLGlossaryId)] ?? string.Empty;
            GoogleApiKey = baseData[nameof(GoogleApiKey)] ?? string.Empty;
            OpenAIApiKey = baseData[nameof(OpenAIApiKey)] ?? string.Empty;
            OpenAIReasoningEffort = Enum.TryParse(baseData[nameof(OpenAIReasoningEffort)], out OpenAIReasoningEffort reasoningEffort)
                ? reasoningEffort
                : defaultOpenAIReasoningEffort;
            SimpleViewX = int.TryParse(baseData[nameof(SimpleViewX)], out int simpleViewX) ? simpleViewX : -1;
            SimpleViewY = int.TryParse(baseData[nameof(SimpleViewY)], out int simpleViewY) ? simpleViewY : -1;
            SimpleViewWidth = int.TryParse(baseData[nameof(SimpleViewWidth)], out int simpleViewWidth)
                ? Math.Max(520, simpleViewWidth)
                : defaultSimpleViewWidth;
            SimpleViewHeight = int.TryParse(baseData[nameof(SimpleViewHeight)], out int simpleViewHeight)
                ? Math.Max(360, simpleViewHeight)
                : defaultSimpleViewHeight;
            SimpleViewRowHeight = int.TryParse(baseData[nameof(SimpleViewRowHeight)], out int simpleViewRowHeight)
                ? Math.Max(28, Math.Min(88, simpleViewRowHeight))
                : defaultSimpleViewRowHeight;
            SimpleViewFontSize = float.TryParse(baseData[nameof(SimpleViewFontSize)], out float simpleViewFontSize)
                ? Math.Max(8F, Math.Min(18F, simpleViewFontSize))
                : defaultSimpleViewFontSize;
            NpcDialogueAutoAdd = baseData[nameof(NpcDialogueAutoAdd)] == null
                ? defaultNpcDialogueAutoAdd
                : baseData[nameof(NpcDialogueAutoAdd)] == "1";

            var standardTipData = iniData[settingsStandardTipsSection];
            foreach (var kd in standardTipData)
            {
                StandardTips.Add(kd.KeyName, kd.Value == "1");
            }

            if (File.Exists(custmizeFileName))
            {
                foreach (var s in File.ReadAllLines(custmizeFileName))
                {
                    CustomizeTips.Add(s);
                }
            }
        }

        private void GenConfigFile()
        {
            var iniData = new IniData();
            var baseSection = iniData[settingsBaseSection];
            baseSection[nameof(CgLogPath)] = string.Empty;
            baseSection[nameof(PlaySound)] = "1";
            baseSection[nameof(SoundVol)] = "5";
            baseSection[nameof(DeduplicationSeconds)] = defaultDeduplicationSeconds.ToString();
            baseSection[nameof(RealtimeDisplayCount)] = defaultRealtimeDisplayCount.ToString();
            baseSection[nameof(DiscordNotificationEnabled)] = "0";
            baseSection[nameof(DiscordWebhookUrl)] = string.Empty;
            baseSection[nameof(TranslationProvider)] = TranslationProvider.DeepL.ToString();
            baseSection[nameof(DeepLApiKey)] = string.Empty;
            baseSection[nameof(DeepLGlossaryId)] = string.Empty;
            baseSection[nameof(GoogleApiKey)] = string.Empty;
            baseSection[nameof(OpenAIApiKey)] = string.Empty;
            baseSection[nameof(OpenAIReasoningEffort)] = defaultOpenAIReasoningEffort.ToString();
            baseSection[nameof(SimpleViewX)] = "-1";
            baseSection[nameof(SimpleViewY)] = "-1";
            baseSection[nameof(SimpleViewWidth)] = defaultSimpleViewWidth.ToString();
            baseSection[nameof(SimpleViewHeight)] = defaultSimpleViewHeight.ToString();
            baseSection[nameof(SimpleViewRowHeight)] = defaultSimpleViewRowHeight.ToString();
            baseSection[nameof(SimpleViewFontSize)] = defaultSimpleViewFontSize.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            baseSection[nameof(NpcDialogueAutoAdd)] = defaultNpcDialogueAutoAdd ? "1" : "0";

            var fileIniDataParser = new FileIniDataParser();
            fileIniDataParser.WriteFile(settingsFileName, iniData);
        }

        private void UpdateConfig()
        {
            var fileIniDataParser = new FileIniDataParser();
            var iniData = new IniData();

            var baseSection = iniData[settingsBaseSection];
            baseSection[nameof(CgLogPath)] = CgLogPath;
            baseSection[nameof(PlaySound)] = PlaySound ? "1" : "0";
            baseSection[nameof(SoundVol)] = SoundVol.ToString();
            baseSection[nameof(DeduplicationSeconds)] = DeduplicationSeconds.ToString();
            baseSection[nameof(RealtimeDisplayCount)] = RealtimeDisplayCount.ToString();
            baseSection[nameof(DiscordNotificationEnabled)] = DiscordNotificationEnabled ? "1" : "0";
            baseSection[nameof(DiscordWebhookUrl)] = DiscordWebhookUrl ?? string.Empty;
            baseSection[nameof(TranslationProvider)] = TranslationProvider.ToString();
            baseSection[nameof(DeepLApiKey)] = DeepLApiKey ?? string.Empty;
            baseSection[nameof(DeepLGlossaryId)] = DeepLGlossaryId ?? string.Empty;
            baseSection[nameof(GoogleApiKey)] = GoogleApiKey ?? string.Empty;
            baseSection[nameof(OpenAIApiKey)] = OpenAIApiKey ?? string.Empty;
            baseSection[nameof(OpenAIReasoningEffort)] = OpenAIReasoningEffort.ToString();
            baseSection[nameof(SimpleViewX)] = SimpleViewX.ToString();
            baseSection[nameof(SimpleViewY)] = SimpleViewY.ToString();
            baseSection[nameof(SimpleViewWidth)] = SimpleViewWidth.ToString();
            baseSection[nameof(SimpleViewHeight)] = SimpleViewHeight.ToString();
            baseSection[nameof(SimpleViewRowHeight)] = SimpleViewRowHeight.ToString();
            baseSection[nameof(SimpleViewFontSize)] = SimpleViewFontSize.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            baseSection[nameof(NpcDialogueAutoAdd)] = NpcDialogueAutoAdd ? "1" : "0";

            var standardTipData = iniData[settingsStandardTipsSection];
            foreach (var kv in StandardTips)
            {
                standardTipData[kv.Key] = kv.Value ? "1" : "0";
            }

            fileIniDataParser.WriteFile(settingsFileName, iniData);

            File.WriteAllLines(custmizeFileName, CustomizeTips);
        }

        internal void SetCgLogPath(string cgLogPath)
        {
            CgLogPath = cgLogPath;
            UpdateConfig();
        }

        internal void SetStandardTip(string nameInSetting, bool @checked)
        {
            StandardTips[nameInSetting] = @checked;
            UpdateConfig();
        }

        internal void SetPlaySound(bool @checked)
        {
            PlaySound = @checked;
            UpdateConfig();
        }

        internal void SetSoundVol(int value)
        {
            SoundVol = value;
            UpdateConfig();
        }

        internal void SetDeduplicationSeconds(int value)
        {
            DeduplicationSeconds = Math.Max(0, value);
            UpdateConfig();
        }

        internal void SetRealtimeDisplayCount(int value)
        {
            RealtimeDisplayCount = Math.Max(20, value);
            UpdateConfig();
        }

        internal void SetDiscordNotificationEnabled(bool value)
        {
            DiscordNotificationEnabled = value;
            UpdateConfig();
        }

        internal void SetDiscordWebhookUrl(string value)
        {
            DiscordWebhookUrl = value?.Trim() ?? string.Empty;
            UpdateConfig();
        }

        internal void SetDeepLApiKey(string value)
        {
            DeepLApiKey = value?.Trim() ?? string.Empty;
            UpdateConfig();
        }

        internal void SetDeepLGlossaryId(string value)
        {
            DeepLGlossaryId = value?.Trim() ?? string.Empty;
            UpdateConfig();
        }

        internal void SetGoogleApiKey(string value)
        {
            GoogleApiKey = value?.Trim() ?? string.Empty;
            UpdateConfig();
        }

        internal void SetOpenAIApiKey(string value)
        {
            OpenAIApiKey = value?.Trim() ?? string.Empty;
            UpdateConfig();
        }

        internal void SetOpenAIReasoningEffort(OpenAIReasoningEffort value)
        {
            OpenAIReasoningEffort = value;
            UpdateConfig();
        }

        internal void SetTranslationProvider(TranslationProvider provider)
        {
            TranslationProvider = provider;
            UpdateConfig();
        }

        internal void AddCustmizeTip(string value)
        {
            CustomizeTips.Add(value);
            UpdateConfig();
        }

        internal void RemoveCustmizeTip(string value)
        {
            CustomizeTips.Remove(value);
            UpdateConfig();
        }

        internal void SetSimpleViewWindowBounds(Rectangle bounds)
        {
            SimpleViewX = bounds.X;
            SimpleViewY = bounds.Y;
            SimpleViewWidth = Math.Max(520, bounds.Width);
            SimpleViewHeight = Math.Max(360, bounds.Height);
            UpdateConfig();
        }

        internal void SetSimpleViewAppearance(int rowHeight, float fontSize)
        {
            SimpleViewRowHeight = Math.Max(28, Math.Min(88, rowHeight));
            SimpleViewFontSize = Math.Max(8F, Math.Min(18F, fontSize));
            UpdateConfig();
        }

        internal void SetNpcDialogueAutoAdd(bool value)
        {
            NpcDialogueAutoAdd = value;
            UpdateConfig();
        }

        static string TryLoadLegacyDiscordWebhookUrl()
        {
            const string legacyIniFilename = "DiscordNotifier.ini";
            if (!File.Exists(legacyIniFilename))
            {
                return string.Empty;
            }

            try
            {
                var ini = new FileIniDataParser().ReadFile(legacyIniFilename);
                return ini.TryGetKey("webhookUrl", out var webhookUrl)
                    ? webhookUrl ?? string.Empty
                    : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        const string LegacyCustomNotifyTypes = "CustomNotifyTypes";
    }
}
