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
        public override Options ProviderOptions => Options.ItemSets | Options.RunePages | Options.SkillOrder;

        private static string GetRoleUrl(int championId, Position position)
            => $"https://op.gg/champion/{Riot.GetChampion(championId).Key}/statistics/{PositionToName[position]}";

        private static string GetItemUrl(int championId, Position position)
            => $"https://op.gg/champion/{Riot.GetChampion(championId).Key}/statistics{(string.IsNullOrEmpty(PositionToName[position]) ? "" : $"/{PositionToName[position]}")}/item";

        public override async Task<Position[]> GetPossibleRoles(int championId)
        {
            return PositionToName.Keys.Except(new []{ Position.Fill }).ToArray();
        }

        public override async Task<RunePage> GetRunePage(int championId, Position position)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await WebCache.String(GetRoleUrl(championId, position), soft: true));

            var pages = doc.DocumentNode.Descendants().Where(o => o.HasClass("perk-page")).Take(2);

            int[][] pageRows = pages.SelectMany(o => o.Descendants().Select(ParseRow)).Select(o => o.ToArray()).Where(o => o.Length > 0).ToArray();
            var perks = pageRows.Where((_, i) => i != 0 && i != 5).SelectMany(o => o).ToList();

            var fragments = doc.DocumentNode.Descendants().Where(o => o.HasClass("fragment__row")).Take(3);
            perks.AddRange(fragments.Select(o =>
            {
                var src = o.Descendants().Single(i => i.HasClass("tip") && i.HasClass("active")).GetAttributeValue("src", "");
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

        public override async Task<string> GetSkillOrder(int championId, Position position)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await WebCache.String(GetRoleUrl(championId, position), soft: true));

            char[] tips = doc.DocumentNode.Descendants("li")
                .Where(o => o.HasClass("champion-stats__list__item") && o.HasClass("tip"))
                .Take(3)
                .Select(o => o.Descendants("span").First().InnerText[0])
                .ToArray();

            char[] slong = doc.DocumentNode.Descendants("table")
                .First(o => o.HasClass("champion-skill-build__table"))
                .Descendants("tr").Last()
                .Descendants()
                .Where(o => o.Name.Equals("td", StringComparison.OrdinalIgnoreCase))
                .Select(o => o.HasClass("tip") ? o.Descendants("span").First().InnerText.TrimStart()[0] : o.InnerText.TrimStart()[0])
                .ToArray();

            return $"({string.Join(">", tips)}) {new string(slong)}";
        }

        public override async Task<ItemSet> GetItemSet(int championId, Position position)
        {
            var test = GetItemUrl(championId, position);
            var doc = new HtmlDocument();
            doc.LoadHtml(await WebCache.String(GetItemUrl(championId, position), soft: true));

            var contentTables = doc.DocumentNode.Descendants("table")
                .Where(o => o.HasClass("champion-stats__table")).ToList();

            var starterItem = GetRows("Starter Items");
            var coreItems = GetRows("Core Build");
            var bootsItem = GetRows("Boots");
            
            var blocks = new List<ItemSet.SetBlock>();
            AddBlockEntry(starterItem, "Starter Items");
            AddBlockEntry(coreItems, "Core Build");
            AddBlockEntry(bootsItem, "Boots");
            
            return new ItemSet
            {
                Champion = championId,
                Name = this.Name + ": " + position,
                Position = position,
                Blocks = blocks.ToArray()
            };

            List<HtmlNode> GetRows(string tableHeader)
            {
                var table = contentTables.FirstOrDefault(d1 => d1.Descendants("th").Any(d2 => d2.InnerText.Contains(tableHeader)));
                return table?.Descendants("tr").Where(tr => tr.ChildNodes.Any(td => td.HasClass("champion-stats__table__cell"))).ToList();
            }

            void AddBlockEntry(List<HtmlNode> nodes, string blockName)
            {
                foreach (var node in nodes)
                {
                    var items = new List<int>();
                    var itemCounter = 0;
                    node.Descendants("img").ToList().ForEach(n =>
                    {
                        if (n.ParentNode.HasClass("champion-stats__list__item"))
                        {
                            itemCounter++;
                            if (int.TryParse(Regex.Match(n.Attributes["src"].Value, "/item/(\\d+).png").Groups[1].Value, out var item))
                            {
                                items.Add(item);
                            }   
                        }
                    });
                    if (items.Count != itemCounter)
                        continue;
                    
                    var pickRate = node.Descendants("td").FirstOrDefault(td => td.HasClass("champion-stats__table__cell--pickrate"))?.InnerText ?? "";
                    pickRate = Regex.Replace(pickRate, @"[^\w|\.|%]", "");
                    var matches = Regex.Match(pickRate, @"(\d{1,2}\.\d{2})%(\d+)");
                    var winRate = node.Descendants("td").FirstOrDefault(td => td.HasClass("champion-stats__table__cell--winrate"))?.InnerText;
                    blocks.Add(new ItemSet.SetBlock
                    {
                        Name = $"{blockName} | PickRate {matches?.Groups[1].Value}% | PickCount {matches?.Groups[2].Value} | WinRate {winRate}",
                        Items = items.ToArray()
                    });
                }
            }
        }
    }
}
