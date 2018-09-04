using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data
{
    internal class Config
    {
        public static Config Default { get; } = Load();

        private const string FilePath = "config.json";
        private const int LatestVersion = 10;

        public static readonly string[] AvailableLanguages = new[]
        {
            "en-US",
            "es-ES"
        };

        public int ConfigVersion { get; set; } = LatestVersion;

        public bool CheckUpdatesBeforeStartup { get; set; } = true;
        public bool LoadCacheBeforeStartup { get; set; } = true;
        public string CultureName { get; set; } = null;
        public bool AutoAccept { get; set; }
        public bool UploadOnLock { get; set; } = true;
        public bool LoadOnLock { get; set; }
        public string LockLoadProvider { get; set; }
        public bool AutoPickChampion { get; set; }
        public bool DisablePickChampion { get; set; }
        public bool AutoBanChampion { get; set; }
        public bool DisableBanChampion { get; set; }
        public bool AutoPickSumms { get; set; }
        public bool DisablePickSumms { get; set; }

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
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        private static Config Load()
        {
            Config c;

            if (!File.Exists(FilePath))
            {
                c = new Config();
            }
            else
            {
                c = JsonConvert.DeserializeObject<Config>(File.ReadAllText(FilePath));
            }

            if (c.CultureName == null)
            {
                c.CultureName = CultureInfo.CurrentCulture.Name;
            }

            if (c.ConfigVersion < LatestVersion)
            {
                c.ConfigVersion = LatestVersion;
            }

            c.Save();
            return c;
        }
    }
}
