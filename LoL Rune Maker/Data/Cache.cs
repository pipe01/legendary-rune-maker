using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data
{
    internal class Cache<TKey, TValue>
    {
        private const string CachePath = "cache";

        private readonly Func<TKey, TValue> Getter;
        private readonly string CacheFileName;

        private string CacheFilePath => Path.Combine(CachePath, CacheFileName);

        private IDictionary<TKey, TValue> Dicc = new Dictionary<TKey, TValue>();

        public Cache(Func<TKey, TValue> getter, string cacheFileName = null)
        {
            this.Getter = getter;
            this.CacheFileName = cacheFileName;

            if (cacheFileName != null)
                Load();
        }

        private void Load()
        {
            if (File.Exists(CacheFilePath))
            {
                this.Dicc = JsonConvert.DeserializeObject<Dictionary<TKey, TValue>>(File.ReadAllText(CacheFilePath));
            }
        }

        private void Save()
        {
            if (CacheFileName != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CacheFilePath));
                File.WriteAllText(CacheFilePath, JsonConvert.SerializeObject(Dicc));
            }
        }

        public TValue Get(TKey key)
        {
            if (!Dicc.TryGetValue(key, out var val))
            {
                Dicc[key] = Getter(key);
                Save();
            }

            return Dicc[key];
        }
    }
}
