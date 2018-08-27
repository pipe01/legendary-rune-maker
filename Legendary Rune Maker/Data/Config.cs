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
        private const int LatestVersion = 1;

        public int ConfigVersion { get; set; } = LatestVersion;

        public bool CheckUpdatesBeforeStartup { get; set; } = true;
        public bool LoadCacheBeforeStartup { get; set; }
        public bool AutoAccept { get; set; }
        public bool UploadOnLock { get; set; } = true;

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
                c.Save();

            return c;
        }
    }
}
