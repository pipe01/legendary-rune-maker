using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Legendary_Rune_Maker.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Providers
{
    internal class METAsrcProvider : Provider
    {
        public override string Name => "METAsrc";

        public override Options ProviderOptions => Options.Counters;

        private static readonly IDictionary<Position, string> PositionNames = new Dictionary<Position, string>
        {
            [Position.Top] = "top",
            [Position.Jungle] = "jungle",
            [Position.Mid] = "mid",
            [Position.Bottom] = "adc",
            [Position.Support] = "support",
            [Position.Fill] = ""
        };

        private async Task<string> GetChampionURL(int championId, Position? pos = null)
            => $"https://www.metasrc.com/5v5/champion/{(await Riot.GetChampionAsync(championId)).Key}/{(pos == null ? null : PositionNames[pos.Value])}";

        public override async Task<Position[]> GetPossibleRoles(int championId)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await WebCache.String(await GetChampionURL(championId), soft: true));

            return doc.DocumentNode
                .QuerySelectorAll("a._yeu4ck")
                .Select(o =>
                {
                    var linkHref = o.GetAttributeValue("href", "").Split('/').ArrayLast();
                    return PositionNames.Single(i => i.Value == linkHref).Key;
                })
                .ToArray();
        }

        public override async Task<Champion[]> GetCountersFor(int championId, Position position, int maxCount = 5)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await WebCache.String(await GetChampionURL(championId), soft: true));

            return await doc.DocumentNode
                .QuerySelectorAll(".row._yq1p7n._cu8r22._gzehfc")
                .ElementAt(1)
                .QuerySelectorAll("img")
                .Select(o =>
                {
                    var imgUrl = o.GetAttributeValue("src", "");

                    if (string.IsNullOrEmpty(imgUrl))
                        return null;

                    var champKey = Path.GetFileNameWithoutExtension(new Uri(imgUrl).Segments.ArrayLast());
                    return Riot.GetChampionAsync(champKey);
                })
                .Where(o => o != null)
                .Take(maxCount)
                .ToArray()
                .AwaitAll();
        }
    }
}
