using Anotar.Log4Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
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

            [JsonIgnore]
            public IDictionary<string, string> SoftFileCache = new Dictionary<string, string>();
            public IDictionary<string, object> SoftObjectCache = new Dictionary<string, object>();
        }

        private const string CachePath = "cache/data.json";

        private static CacheData Data = new CacheData();
        private static HttpClient Client => new HttpClient();
        private static object WriteLock = new object();
        
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
            LogTo.Debug("Saving web cache");

            string text;

            lock (Data)
                text = JsonConvert.SerializeObject(Data, JsonSettings);

            lock (WriteLock)
                File.WriteAllText(CachePath, Convert.ToBase64String(Encoding.UTF8.GetBytes(text)));
        }

        public static void Clear()
        {
            LogTo.Debug("Clearing web cache");

            Data = new CacheData();
            Save();
        }

        public static async Task<string> String(string url, HttpClient client = null, bool soft = false)
        {
            LogTo.Debug("Cache string requested (Soft={0}): {1}", soft, url);

            var dic = soft ? Data.SoftFileCache : Data.FileCache;

            if (!dic.TryGetValue(url, out var value))
            {
                var response = await (client ?? Client).SendAsync(new HttpRequestMessage(HttpMethod.Get, url));

                if (!response.IsSuccessStatusCode)
                    return null;

                value = await response.Content.ReadAsStringAsync();

                lock (Data)
                    dic[url] = value;

                Save();
            }
            else
            {
                LogTo.Debug("Cache hit");
            }

            return value;
        }
        
        public static async Task<T> Json<T>(string url, HttpClient client = null, bool soft = false)
        {
            LogTo.Debug("Cache json object requested (Soft={0}): {1}", soft, url);

            var dic = soft ? Data.SoftObjectCache : Data.ObjectCache;

            if (!dic.TryGetValue(url, out var value))
            {
                value = JsonConvert.DeserializeObject<T>(await String(url, client));

                lock (Data)
                    dic[url] = value;

                Save();
            }
            else
            {
                LogTo.Debug("Cache hit");
            }

            return (T)value;
        }

        public static async Task<T> CustomJson<T>(string url, Func<JObject, T> converter, HttpClient client = null, bool soft = false)
        {
            LogTo.Debug("Cache custom json object requested (Soft={0}): {1}", soft, url);

            var dic = soft ? Data.SoftObjectCache : Data.ObjectCache;

            if (!dic.TryGetValue(url, out var value))
            {
                string json = await String(url, client);

                lock (Data)
                    dic[url] = value = converter(JObject.Parse(json));

                Save();
            }
            else
            {
                LogTo.Debug("Cache hit");
            }

            return (T)value;
        }
    }
}
