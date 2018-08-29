using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        private const int LatestVersion = 6;

        public int ConfigVersion { get; set; } = LatestVersion;

        public bool CheckUpdatesBeforeStartup { get; set; } = true;
        public bool LoadCacheBeforeStartup { get; set; } = true;
        public bool AutoAccept { get; set; }
        public bool UploadOnLock { get; set; } = true;
        public bool LoadOnLock { get; set; }
        public bool AutoPickChampion { get; set; }
        public bool AutoBanChampion { get; set; }
        public Dictionary<Position, int> PickChampions { get; set; } = new Dictionary<Position, int>
        {
            [Position.Fill] = 0,
            [Position.Top] = 0,
            [Position.Jungle] = 0,
            [Position.Mid] = 0,
            [Position.Bottom] = 0,
            [Position.Support] = 0,
        };
        public Dictionary<Position, int> BanChampions { get; set; } = new Dictionary<Position, int>
        {
            [Position.Fill] = 0,
            [Position.Top] = 0,
            [Position.Jungle] = 0,
            [Position.Mid] = 0,
            [Position.Bottom] = 0,
            [Position.Support] = 0,
        };

        public void Save()
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        private static Config Load()
        {
            if (!File.Exists(FilePath))
                return new Config();

            var c = JsonConvert.DeserializeObject<Config>(File.ReadAllText(FilePath));

            if (c.ConfigVersion < LatestVersion)
            {
                c.ConfigVersion = LatestVersion;
                c.Save();
            }

            return c;
        }
    }
}
