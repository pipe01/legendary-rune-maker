using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

        private static RuneTree[] Trees;
        public static async Task<RuneTree[]> GetRuneTrees()
        {
            if (Trees == null)
            {
                Trees = JsonConvert.DeserializeObject<RuneTree[]>(await new WebClient().DownloadStringTaskAsync($"{CdnEndpoint}{await GetLatestVersionAsync()}/data/{Locale}/runesReforged.json")).OrderBy(o => o.ID).ToArray();
            }

            return Trees;
        }

        private static Champion[] Champions;
        public static async Task<Champion[]> GetChampions()
        {
            if (Champions == null)
            {
                string json = await new WebClient().DownloadStringTaskAsync($"{CdnEndpoint}{await GetLatestVersionAsync()}/data/{Locale}/champion.json");

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
                        ImageURL = $"{CdnEndpoint}{LatestVersion}/img/champion/" + p.Value["image"]["full"].ToObject<string>()
                    };
                })
                .OrderBy(o => o.Name)
                .ToArray();
            }

            return Champions;
        }

        private static SummonerSpell[] Spells;
        public static async Task<SummonerSpell[]> GetSummonerSpells()
        {
            if (Spells == null)
            {
                string json = await new WebClient().DownloadStringTaskAsync($"{CdnEndpoint}{await GetLatestVersionAsync()}/data/{Locale}/summoner.json");

                var jobj = JObject.Parse(json);
                var data = jobj["data"];

                Spells = data.Children().Select(o =>
                {
                    var p = o as JProperty;
                    return new SummonerSpell
                    {
                        ID = p["key"].ToObject<int>(),
                        Key = p["id"].ToObject<string>(),
                        Name = p["name"].ToObject<string>(),
                        SummonerLevel = p["summonerLevel"].ToObject<int>(),
                        ImageURL = $"{CdnEndpoint}{LatestVersion}/img/spell/" + p.Value["image"]["full"].ToObject<string>()
                    };
                })
                .OrderBy(o => o.SummonerLevel)
                .ThenBy(o => o.Name)
                .ToArray();
            }

            return Spells;
        }

        private static string LatestVersion;
        public static async Task<string> GetLatestVersionAsync()
            => LatestVersion ?? (LatestVersion = JsonConvert.DeserializeObject<string[]>(await new WebClient().DownloadStringTaskAsync("https://ddragon.leagueoflegends.com/api/versions.json"))[0]);

        public static async Task<IDictionary<int, RuneTree>> GetRuneTreesByIDAsync()
            => (await GetRuneTrees()).ToDictionary(o => o.ID);

        public static IDictionary<int, RuneTree> GetRuneTreesByID()
            => Trees.ToDictionary(o => o.ID);

        public static IDictionary<int, Rune> GetAllRunes()
            => Trees.SelectMany(o => o.Slots).SelectMany(o => o.Runes).ToDictionary(o => o.ID);

        public static async Task CacheAll(Action<double> progress)
        {
            await GetLatestVersionAsync();

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
            var client = new WebClient();

            if (!Directory.Exists(ImageCache.Instance.FullCachePath))
                Directory.CreateDirectory(ImageCache.Instance.FullCachePath);

            using (Stream file = await client.OpenReadTaskAsync(CacheZipURLs[host]))
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
