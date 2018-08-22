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

        public static string VersionEndpoint => CdnEndpoint + "8.16.1/"; //TODO Get version automatically
        public static string ImageEndpoint => CdnEndpoint + "img/";

        private static readonly string[] CacheZipURLs = new[]
        {
            "https://www.dropbox.com/s/jre9wq13mu1k7bc/cache.zip?dl=1"
        };

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

        public static IDictionary<int, Rune> GetAllRunes()
            => Trees.SelectMany(o => o.Slots).SelectMany(o => o.Runes).ToDictionary(o => o.ID);

        public static async Task CacheAll(Action<double> progress)
        {
            int p = 0;
            var runes = (await GetRuneTrees()).SelectMany(o => o.Slots).SelectMany(o => o.Runes).Select(o => ImageEndpoint + o.IconURL);
            var champions = (await GetChampions()).Select(o => o.ImageURL);
            var total = runes.Concat(champions).Concat((await GetRuneTrees()).Select(o => ImageEndpoint + o.IconURL));
            int count = total.Count();

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
