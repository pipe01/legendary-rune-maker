using Anotar.Log4Net;
using Legendary_Rune_Maker.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;

namespace Legendary_Rune_Maker.Data
{
    public class Config
    {
        public static Config Current { get => Container.Value; set => Container.Value = value; }
        public static Container<Config> Container { get; } = new Container<Config>(Load());

        private const string FilePath = "config.json";
        private const int LatestVersion = 14;

        public static readonly string[] AvailableLanguages = new[]
        {
            "en-US",
            "es-ES"
        };

        public int ConfigVersion { get; set; } = LatestVersion;

        public bool CheckUpdatesBeforeStartup { get; set; } = true;
        public string CultureName { get; set; }
        public bool MinimizeToTaskbar { get; set; } = true;

        public bool AutoAccept { get; set; } = true;

        public bool UploadOnLock { get; set; } = true;
        public bool LoadOnLock { get; set; } = true;
        public string LockLoadProvider { get; set; } = "U.GG";

        public bool AutoPickChampion { get; set; }
        public bool DisablePickChampion { get; set; } = true;

        public bool AutoBanChampion { get; set; }
        public bool DisableBanChampion { get; set; } = true;

        public bool AutoPickSumms { get; set; }
        public bool DisablePickSumms { get; set; } = true;

        public bool SetItemSet { get; set; } = true;
        public string ItemSetProvider { get; set; } = "U.GG";
        public bool KeepItemSets { get; set; }
        public string LastItemSetUid { get; set; }

        public int LastRunePageId { get; set; }

        public bool ShowSkillOrder { get; set; } = true;
        public string SkillOrderProvider { get; set; } = "U.GG";

        public int DelayBeforeAction { get; set; } = 3000;
        public int DelayBeforeAcceptReady { get; set; } = 4000;
        public int DelayBeforeIntentSet { get; set; } = 4000;


        public Dictionary<Position, int> ChampionsToPick { get; set; } = new Dictionary<Position, int>
        {
            [Position.Fill] = 0,
            [Position.Top] = 0,
            [Position.Jungle] = 0,
            [Position.Mid] = 0,
            [Position.Bottom] = 0,
            [Position.Support] = 0
        };
        public Dictionary<Position, int> ChampionsToBan { get; set; } = new Dictionary<Position, int>
        {
            [Position.Fill] = 0,
            [Position.Top] = 0,
            [Position.Jungle] = 0,
            [Position.Mid] = 0,
            [Position.Bottom] = 0,
            [Position.Support] = 0
        };
        public Dictionary<Position, int[]> SpellsToPick { get; set; } = new Dictionary<Position, int[]>
        {
            [Position.Fill] = new int[2],
            [Position.Top] = new int[2],
            [Position.Jungle] = new int[2],
            [Position.Mid] = new int[2],
            [Position.Bottom] = new int[2],
            [Position.Support] = new int[2]
        };

        [JsonIgnore]
        public CultureInfo Culture => new CultureInfo(this.CultureName);


        public void Save()
        {
            LogTo.Info("Saving config");
            LogTo.Debug(() => JsonConvert.SerializeObject(this));

            try
            {
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
            }
            catch
            {
                if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                    throw;
            }
        }

        public Config Clone()
        {
            return JsonConvert.DeserializeObject<Config>(JsonConvert.SerializeObject(this));
        }

        public static void Reload() => Current = Load();

        private static Config Load()
        {
            Config c;

            try
            {
                if (!File.Exists(FilePath) || MainWindow.InDesigner)
                {
                    c = new Config();
                }
                else
                {
                    c = JsonConvert.DeserializeObject<Config>(File.ReadAllText(FilePath));
                }
            }
            catch
            {
                if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                    return new Config();

                throw;
            }

            if (c.CultureName == null)
                c.CultureName = CultureInfo.CurrentCulture.Name;

            if (c.ConfigVersion < LatestVersion)
                c.ConfigVersion = LatestVersion;

            c.Save();
            return c;
        }
    }
}
