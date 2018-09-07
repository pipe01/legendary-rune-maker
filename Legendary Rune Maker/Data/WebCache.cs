using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data
{
    internal static class WebCache
    {
        private class CacheData
        {
            public string GameVersion;
            public string CultureName;
            public IDictionary<string, string> FileCache = new Dictionary<string, string>();
            public IDictionary<string, object> ObjectCache = new Dictionary<string, object>();
        }

        private static CacheData Data = new CacheData();

        private static HttpClient Client => new HttpClient();

        private const string CachePath = "cache/data.json";
        
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public static string CacheGameVersion
        {
            get => Data.GameVersion;
            set => Data.GameVersion = value;
        }
        
        public static string CacheLocale
        {
            get => Data.CultureName;
            set => Data.CultureName = value;
        }

        static WebCache()
        {
            if (!File.Exists(CachePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CachePath));
                Save();
            }
            else
            {
                string text = Encoding.UTF8.GetString(Convert.FromBase64String(File.ReadAllText(CachePath)));

                Data = JsonConvert.DeserializeObject<CacheData>(text, JsonSettings);
            }
        }

        private static void Save()
        {
            string text = JsonConvert.SerializeObject(Data, JsonSettings);

            File.WriteAllText(CachePath, Convert.ToBase64String(Encoding.UTF8.GetBytes(text)));
        }

        public static void Clear()
        {
            Data = new CacheData();
            Save();
        }

        public static async Task<string> String(string url, HttpClient client = null)
        {
            if (!Data.FileCache.TryGetValue(url, out var value))
            {
                Data.FileCache[url] = value = await (client ?? Client).GetStringAsync(url).ConfigureAwait(false);
                Save();
            }

            return value;
        }

        public static async Task<T> Json<T>(string url, HttpClient client = null)
        {
            if (!Data.ObjectCache.TryGetValue(url, out var value))
            {
                Data.ObjectCache[url] = value = JsonConvert.DeserializeObject<T>(await String(url, client));
                Save();
            }

            return (T)value;
        }

        public static async Task<T> CustomJson<T>(string url, Func<JObject, T> converter, HttpClient client = null)
        {
            if (!Data.ObjectCache.TryGetValue(url, out var value))
            {
                string json = await String(url, client);

                Data.ObjectCache[url] = value = converter(JObject.Parse(json));
                Save();
            }

            return (T)value;
        }
    }
}
