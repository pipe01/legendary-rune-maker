using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data
{
    internal static class WebCache
    {
        private static IDictionary<string, string> FileCache = new Dictionary<string, string>();
        private static IDictionary<string, object> ObjectCache = new Dictionary<string, object>();

        private static WebClient Client => new WebClient { Encoding = Encoding.UTF8 };

        public static async Task<string> String(string url, WebClient client = null)
        {
            if (!FileCache.TryGetValue(url, out var value))
            {
                FileCache[url] = value = await (client ?? Client).DownloadStringTaskAsync(url);
            }

            return value;
        }

        public static async Task<T> Json<T>(string url, WebClient client = null)
        {
            if (!ObjectCache.TryGetValue(url, out var value))
            {
                ObjectCache[url] = value = JsonConvert.DeserializeObject<T>(await String(url, client));
            }

            return (T)value;
        }

        public static async Task<T> CustomJson<T>(string url, Func<JObject, T> converter, WebClient client = null)
        {
            if (!ObjectCache.TryGetValue(url, out var value))
            {
                string json = await String(url, client);
                
                ObjectCache[url] = value = converter(JObject.Parse(json));
            }

            return (T)value;
        }
    }
}
