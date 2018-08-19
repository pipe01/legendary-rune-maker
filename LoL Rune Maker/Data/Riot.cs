using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        private static Champion[] Champions;
        public static async Task<Champion[]> GetChampions()
        {
            if (Champions == null)
            {
                string json = await new WebClient().DownloadStringTaskAsync(VersionEndpoint + "data/en_US/champion.json");

                var jobj = JObject.Parse(json);
                var data = jobj["data"];

                Champions = data.Children().Select(o =>
                {
                    var p = o as JProperty;
                    return new Champion
                    {
                        ID = p.Value["key"].ToObject<int>(),
                        Key = p.Value["id"].ToObject<string>(),
                        Name = p.Value["name"].ToObject<string>(),
                        ImageURL = VersionEndpoint + "img/champion/" + p.Value["image"]["full"].ToObject<string>()
                    };
                }).ToArray();
            }

            return Champions;
        }

        public static async Task<IDictionary<int, RuneTree>> GetRuneTreesByIDAsync()
            => (await GetRuneTrees()).ToDictionary(o => o.ID);

        public static IDictionary<int, RuneTree> GetRuneTreesByID()
            => Trees.ToDictionary(o => o.ID);

        public static async Task CacheAllImages(Action<double> progress)
        {
            int p = 0;
            var runes = (await GetRuneTrees()).SelectMany(o => o.Slots).SelectMany(o => o.Runes).Select(o => ImageEndpoint + o.IconURL);
            var champions = (await GetChampions()).Select(o => o.ImageURL);
            var total = runes.Concat(champions);
            int count = total.Count();

            await Task.WhenAll(total.Select(async o =>
            {
                await ImageCache.Instance.Get(o);
                progress((double)p++ / count);
            }));
        }
    }
}
