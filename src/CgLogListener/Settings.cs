using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CgLogListener
{
    public sealed class Settings
    {
        static Settings instance;
        const string settingsFileName = "settings.ini";
        const string settingsBaseSection = "base";
        const string settingsStandardTipsSection = "standard tips";
        const string custmizeFileName = "custmize.dat";

        public List<FormMain.CustomNotifyType> CustomNotifyTypes { get; private set; } = new List<FormMain.CustomNotifyType>();
        public bool PlaySound { get; private set; }
        public int SoundVol { get; private set; }
        public string CgLogPath { get; private set; }
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

            var baseData = iniData[settingsBaseSection];
            CgLogPath = baseData[nameof(CgLogPath)];
            PlaySound = baseData[nameof(PlaySound)] == "1";
            SoundVol = int.Parse(baseData[nameof(SoundVol)]);
            CustomNotifyTypes = baseData[nameof(CustomNotifyTypes)]
                .Split(',')
                .Select(s =>
                    Enum.TryParse(s, out FormMain.CustomNotifyType result) ? result : FormMain.CustomNotifyType.None)
                .Where(t => t != FormMain.CustomNotifyType.None)
                .ToList();

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
            baseSection[nameof(CustomNotifyTypes)] = string.Empty;

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
            baseSection[nameof(CustomNotifyTypes)] = string.Join(",", CustomNotifyTypes.Select(t => t.ToString()));

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

        internal void SetCustomNotify(FormMain.CustomNotifyType notifyType)
        {
            if (CustomNotifyTypes.Contains(notifyType))
            {
                return;
            }
            CustomNotifyTypes.Add(notifyType);
            UpdateConfig();
        }
        
        internal void RemoveCustomNotify(FormMain.CustomNotifyType notifyType)
        {
            if (!CustomNotifyTypes.Contains(notifyType))
            {
                return;
            }
            CustomNotifyTypes.Remove(notifyType);
            UpdateConfig();
        }
    }
}
