using Newtonsoft.Json;
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

        private static WebClient Client => new WebClient { Encoding = Encoding.UTF8 };

        public static async Task<string> String(string url, WebClient client = null)
        {
            if (!FileCache.TryGetValue(url, out var value))
            {
                value = await (client ?? Client).DownloadStringTaskAsync(url);
            }

            return value;
        }

        public static async Task<T> Json<T>(string url, WebClient client = null)
        {
            return JsonConvert.DeserializeObject<T>(await String(url, client));
        }
    }
}
