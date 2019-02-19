using Anotar.Log4Net;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data
{
    internal static class Riot
    {
        public const string CdnEndpoint = "https://ddragon.leagueoflegends.com/cdn/";

        public static string ImageEndpoint => CdnEndpoint + "img/";

        private static readonly IDictionary<string, string> CDragonLocale = new Dictionary<string, string>
        {
            ["en_US"] = "en_gb",
            ["es_ES"] = "es_es"
        };

        public static string Locale { get; set; } = "en_US";

        private static WebClient Client => new WebClient { Encoding = Encoding.UTF8 };

        public static IDictionary<int, Rune> StatRunes => Runes.Where(o => o.Value.IsStatMod).ToDictionary(o => o.Key, o => o.Value);
        public static IDictionary<int, Rune> Runes { get; private set; }
        public static async Task<IDictionary<int, Rune>> GetRunesAsync()
        {
            if (Runes == null)
            {
                string locale = CDragonLocale[Locale];
                string raw = await WebCache.String($"https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/{locale}/v1/perks.json");
                var array = JArray.Parse(raw);

                Runes = array.Select(o => o.ToObject<Rune>()).ToDictionary(o => o.ID);
            }

            return Runes;
        }

        public static IDictionary<int, TreeStructure> TreeStructures { get; private set; }
        public static async Task<IDictionary<int, TreeStructure>> GetTreeStructuresAsync()
        {
            if (TreeStructures == null)
            {
                TreeStructures = new Dictionary<int, TreeStructure>();

                var runes = await GetRunesAsync();

                string locale = CDragonLocale[Locale];
                string raw = await WebCache.String($"https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/{locale}/v1/perkstyles.json");
                var json = JObject.Parse(raw);

                TreeStructures = new Dictionary<int, TreeStructure>();

                foreach (var style in json["styles"].ToArray())
                {
                    int id = style["id"].ToObject<int>();
                    List<Rune[]> runeSlots = new List<Rune[]>(),
                                 statSlots = new List<Rune[]>();

                    foreach (var slot in style["slots"].ToArray())
                    {
                        string type = slot["type"].ToObject<string>();
                        Rune[] ids = slot["perks"].ToArray().Select(o => runes[o.ToObject<int>()]).ToArray();

                        if (type == "kMixedRegularSplashable" || type == "kKeyStone")
                            runeSlots.Add(ids);
                        else if (type == "kStatMod")
                            statSlots.Add(ids);
                    }

                    TreeStructures[id] = new TreeStructure
                    {
                        ID = id,
                        Name = style["name"].ToObject<string>(),
                        IconURL = GetCDragonRuneIconUrl(style["iconPath"].ToObject<string>()),
                        PerkSlots = runeSlots.ToArray(),
                        StatSlots = statSlots.ToArray()
                    };
                }
            }

            return TreeStructures;
        }

        public static async Task<Rune[][]> GetStatRuneStructureAsync()
            => (await GetTreeStructuresAsync()).Values.First().StatSlots;

        public static async Task<Champion[]> GetChampionsAsync(string locale = null)
        {
            locale = locale ?? Locale;

            string url = $"{CdnEndpoint}{await GetLatestVersionAsync()}/data/{locale}/champion.json";

            return await WebCache.CustomJson(url, jobj =>
            {
                return jobj["data"].Children().Select(o =>
                {
                    var p = o as JProperty;
                    return new Champion
                    {
                        ID = p.Value["key"].ToObject<int>(),
                        Key = p.Value["id"].ToObject<string>(),
                        Name = p.Value["name"].ToObject<string>(),
                        ImageURL = $"{CdnEndpoint}{LatestVersion}/img/champion/" + p.Value["image"]["full"].ToObject<string>()
                    };
                })
                .OrderBy(o => o.Name)
                .ToArray();
            });
        }

        public static async Task<SummonerSpell[]> GetSummonerSpellsAsync()
        {
            string url = $"{CdnEndpoint}{await GetLatestVersionAsync()}/data/{Locale}/summoner.json";

            return await WebCache.CustomJson(url, jobj =>
            {
                return jobj["data"].Children().Select(o =>
                {
                    var p = o as JProperty;
                    return new SummonerSpell
                    {
                        ID = p.Value["key"].ToObject<int>(),
                        Key = p.Value["id"].ToObject<string>(),
                        Name = p.Value["name"].ToObject<string>(),
                        SummonerLevel = p.Value["summonerLevel"].ToObject<int>(),
                        ImageURL = $"{CdnEndpoint}{LatestVersion}/img/spell/" + p.Value["image"]["full"].ToObject<string>()
                    };
                })
                .OrderBy(o => o.SummonerLevel)
                .ThenBy(o => o.Name)
                .ToArray();
            });
        }

        public static async Task<Item[]> GetItemsAsync()
        {
            string url = $"{CdnEndpoint}{await GetLatestVersionAsync()}/data/{Locale}/item.json";

            return await WebCache.CustomJson(url, jobj =>
            {
                return jobj["data"].Children().Select(o =>
                {
                    var p = o as JProperty;
                    return new Item(int.Parse(p.Name));
                })
                .ToArray();
            });
        }


        public static void SetLanguage(CultureInfo culture)
        {
            var availLangs = CDragonLocale.Keys;

            string cultureName = culture.Name.Replace('-', '_');

            if (availLangs.Any(o => o.Equals(cultureName)))
            {
                Locale = cultureName;
            }
            else
            {
                //Try to get a locale that matches the region (es_ES -> es)
                string almostLocale = availLangs.FirstOrDefault(o => o.Split('_')[0].Equals(cultureName.Split('_')[0]));

                Locale = almostLocale ?? "en_US";
            }

            LogTo.Debug("Riot locale set to " + Locale);
        }


        private static string LatestVersion;
        public static async Task<string> GetLatestVersionAsync()
            => LatestVersion ?? (LatestVersion = JsonConvert.DeserializeObject<string[]>(await Client.DownloadStringTaskAsync("https://ddragon.leagueoflegends.com/api/versions.json"))[0]);

        public static Champion GetChampion(int id, string locale = null)
        {
            var task = GetChampionsAsync(locale);
            task.Wait();
            return task.Result.SingleOrDefault(o => o.ID == id);
        }

        public static async Task CacheAllAsync(Action<double> progress)
        {
            await GetLatestVersionAsync();

            await GetItemsAsync();

            var champions = (await GetChampionsAsync()).Select(o => o.ImageURL);
            var spells = (await GetSummonerSpellsAsync()).Select(o => o.ImageURL);
            var runes = (await GetRunesAsync()).Select(o => o.Value.IconURL);
            var trees = (await GetTreeStructuresAsync()).Select(o => o.Value.IconURL);

            var total = runes.Concat(champions).Concat(spells).Concat(trees);
            int count = total.Count();

            int p = 0;
            await Task.WhenAll(total.Select(async o =>
            {
                await ImageCache.Instance.Get(o);
                progress((double)p++ / count);
            }));
        }

        public static string GetCDragonRuneIconUrl(string url)
            => "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/"
                + url.Replace("/lol-game-data/assets/v1/", "").ToLower();
    }
}
