using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LoL_Rune_Maker.Data
{
    internal static class Riot
    {
        public const string CdnEndpoint = "https://ddragon.leagueoflegends.com/cdn/";

        public static string VersionEndpoint => CdnEndpoint + "8.16.1/"; //TODO Get version automatically
        public static string ImageEndpoint => CdnEndpoint + "img/";

        private static RuneTree[] Trees;
        public static async Task<RuneTree[]> GetRuneTrees()
        {
            if (Trees == null)
            {
                Trees = JsonConvert.DeserializeObject<RuneTree[]>(await new WebClient().DownloadStringTaskAsync(VersionEndpoint + "data/en_US/runesReforged.json")).OrderBy(o => o.ID).ToArray();
            }

            return Trees;
        }

        public static async Task<IDictionary<int, RuneTree>> GetRuneTreesByID()
            => (await GetRuneTrees()).ToDictionary(o => o.ID);

        public static async Task CacheAllImages()
        {
            foreach (var tree in await GetRuneTrees())
            {
                foreach (var slot in tree.Slots)
                {
                    foreach (var rune in slot.Runes)
                    {
                        await ImageCache.Instance.Get(ImageEndpoint + rune.IconURL);
                    }
                }
            }
        }
    }
}
