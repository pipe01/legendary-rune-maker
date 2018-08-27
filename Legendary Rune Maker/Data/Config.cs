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

            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(FilePath));
        }
    }
}
