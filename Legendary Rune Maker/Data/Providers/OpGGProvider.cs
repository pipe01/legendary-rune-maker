using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Providers
{
    internal class OpGGProvider : Provider
    {
        private static readonly IDictionary<Position, string> PositionToName = new Dictionary<Position, string>
        {
            [Position.Top] = "Top",
            [Position.Jungle] = "Jungle",
            [Position.Mid] = "Mid",
            [Position.Support] = "Support",
            [Position.Bottom] = "Bot",
            [Position.Fill] = ""
        };

        public override string Name => "OP.GG";
        public override Options ProviderOptions => Options.RunePages;

        private static string GetRoleUrl(int championId, Position position)
            => $"https://op.gg/champion/{Riot.GetChampion(championId).Key}/statistics/{PositionToName[position]}";

        protected override async Task<Position[]> GetPossibleRolesInner(int championId)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await new WebClient().DownloadStringTaskAsync(GetRoleUrl(championId, Position.Fill)));

            var positionNodes = doc.DocumentNode.Descendants().Where(o => o.HasClass("champion-stats-header__position"));

            return positionNodes.Select<HtmlNode, Position?>(o =>
            {
                string data = o.GetAttributeValue("data-position", "");

                if (data == "ADC")
                    return Position.Bottom;

                foreach (var item in PositionToName)
                {
                    if (item.Value.Equals(data, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return item.Key;
                    }
                }

                return null;
            }).Where(o => o != null).Select(o => o.Value).ToArray();
        }

        protected override async Task<RunePage> GetRunePageInner(int championId, Position position)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await new WebClient().DownloadStringTaskAsync(GetRoleUrl(championId, position)));

            var pages = doc.DocumentNode.Descendants().Where(o => o.HasClass("perk-page")).Take(2);

            int[][] pageRows = pages.SelectMany(o => o.Descendants().Select(ParseRow)).Select(o => o.ToArray()).Where(o => o.Length > 0).ToArray();
            var perks = pageRows.Where((_, i) => i != 0 && i != 5).SelectMany(o => o).ToList();

            var fragments = doc.DocumentNode.Descendants().Where(o => o.HasClass("fragment")).Take(3);
            perks.AddRange(fragments.Select(o =>
            {
                var src = o.Descendants().Single(i => i.HasClass("tip")).GetAttributeValue("src", "");
                return int.Parse(Regex.Match(src, @"(?<=perkShard\/).*?(?=\.png)").Value);
            }));

            return new RunePage(perks.ToArray(), pageRows[0][0], pageRows[5][0], championId, position);
        }

        private static IEnumerable<int> ParseRow(HtmlNode row)
        {
            foreach (var item in row.Descendants().Where(o => o.HasClass("perk-page__item--mark")
                                                              || o.HasClass("perk-page__item--active")))
            {
                var img = item.Descendants().First(o => o.Name == "img" && o.HasClass("tip"));

                yield return int.Parse(Regex.Match(img.GetAttributeValue("src", ""), @"(?<=\/)\d+(?=\.)").Value);
            }
        }

        protected override Task<ItemSet> GetItemSetInner(int championId, Position position)
        {
            throw new NotImplementedException();
        }
    }
}
