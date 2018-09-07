using ICSharpCode.SharpZipLib.Core;
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

        public static string Locale { get; set; } = "en_US";

        private static readonly string[] CacheZipURLs = new[]
        {
            "https://www.dropbox.com/s/jre9wq13mu1k7bc/cache.zip?dl=1"
        };

        private static WebClient Client => new WebClient { Encoding = Encoding.UTF8 };

        private static RuneTree[] Trees;
        public static async Task<RuneTree[]> GetRuneTrees()
        {
            if (Trees == null)
            {
                Trees = (await WebCache.Json<RuneTree[]>($"{CdnEndpoint}{await GetLatestVersionAsync()}/data/{Locale}/runesReforged.json")).OrderBy(o => o.ID).ToArray();
            }

            return Trees;
        }
        
        public static async Task<Champion[]> GetChampions(string locale = null)
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
        
        public static async Task<SummonerSpell[]> GetSummonerSpells()
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


        public static async Task SetLanguage(CultureInfo culture)
        {
            var availLangs = JsonConvert.DeserializeObject<string[]>(await Client.DownloadStringTaskAsync(CdnEndpoint + "languages.json"));

            string cultureName = culture.Name.Replace('-', '_');

            if (availLangs.Any(o => o.Equals(cultureName)))
            {
                Locale = cultureName;
            }
            else
            {
                //Try to get a locale that matches the region (es_ES -> es)
                string almostLocale = availLangs.FirstOrDefault(o => o.Split('_')[0].Equals(cultureName.Split('_')[0]));

                if (almostLocale != null)
                {
                    Locale = almostLocale;
                }
                else
                {
                    Locale = "en_US";
                }
            }
        }



        private static string LatestVersion;
        public static async Task<string> GetLatestVersionAsync()
            => LatestVersion ?? (LatestVersion = JsonConvert.DeserializeObject<string[]>(await Client.DownloadStringTaskAsync("https://ddragon.leagueoflegends.com/api/versions.json"))[0]);



        public static async Task<IDictionary<int, RuneTree>> GetRuneTreesByIDAsync()
            => (await GetRuneTrees()).ToDictionary(o => o.ID);

        public static IDictionary<int, RuneTree> GetRuneTreesByID()
            => Trees.ToDictionary(o => o.ID);

        public static IDictionary<int, Rune> GetAllRunes()
            => Trees.SelectMany(o => o.Slots).SelectMany(o => o.Runes).ToDictionary(o => o.ID);

        public static Champion GetChampion(int id, string locale = null)
        {
            var task = GetChampions(locale);
            task.Wait();
            return task.Result.SingleOrDefault(o => o.ID == id);
        }

        public static async Task CacheAll(Action<double> progress)
        {
            await GetLatestVersionAsync();

            await GetItemsAsync();

            var runes = (await GetRuneTrees()).SelectMany(o => o.Slots).SelectMany(o => o.Runes).Select(o => ImageEndpoint + o.IconURL);
            var champions = (await GetChampions()).Select(o => o.ImageURL);
            var spells = (await GetSummonerSpells()).Select(o => o.ImageURL);
            var trees = (await GetRuneTrees()).Select(o => ImageEndpoint + o.IconURL);

            var total = runes.Concat(champions).Concat(spells).Concat(trees);
            int count = total.Count();

            int p = 0;
            await Task.WhenAll(total.Select(async o =>
            {
                await ImageCache.Instance.Get(o);
                progress((double)p++ / count);
            }));
        }

        public static async Task DownloadCacheCompressed(int host = 0)
        {
            if (!Directory.Exists(ImageCache.Instance.FullCachePath))
                Directory.CreateDirectory(ImageCache.Instance.FullCachePath);

            using (Stream file = await Client.OpenReadTaskAsync(CacheZipURLs[host]))
            {
                var zip = new ZipInputStream(file);
                ZipEntry entry;

                while ((entry = zip.GetNextEntry()) != null)
                {
                    string path = Path.Combine(ImageCache.Instance.FullCachePath, entry.Name);
                    
                    using (Stream local = File.OpenWrite(path))
                    {
                        await Copy(zip, local);
                    }
                }
            }
        }

        private static async Task Copy(Stream source, Stream target)
        {
            byte[] buffer = new byte[4096];
            int read;

            while ((read = await source.ReadAsync(buffer, 0, 4096)) != 0)
            {
                await target.WriteAsync(buffer, 0, read);
            }

            await target.FlushAsync();
        }
    }
}
