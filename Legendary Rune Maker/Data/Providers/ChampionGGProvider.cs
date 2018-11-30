using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Providers
{
    internal class ChampionGGProvider : Provider
    {
        public override string Name => "Champion.GG";
        public override Options ProviderOptions => Options.RunePages | Options.ItemSets | Options.SkillOrder;

        private static readonly IDictionary<Position, string> PositionToName = new Dictionary<Position, string>
        {
            [Position.Top] = "Top",
            [Position.Jungle] = "Jungle",
            [Position.Mid] = "Middle",
            [Position.Support] = "Support",
            [Position.Bottom] = "ADC",
            [Position.Fill] = ""
        };

        private static string GetChampionURL(int championId, Position? pos = null)
            => $"https://champion.gg/champion/{Riot.GetChampion(championId).Key}/"
               + (pos != null ? PositionToName[pos.Value] : "");

        protected override async Task<Position[]> GetPossibleRolesInner(int championId)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await WebCache.String(GetChampionURL(championId), soft: true));

            var n = doc.DocumentNode.SelectNodes("//ul//a[contains(@href, 'champion')]//h3");

            var ret = new List<Position>();

            foreach (var item in n)
            {
                string positionName = item.InnerText;

                foreach (var pos in PositionToName)
                {
                    if (pos.Value.Equals(positionName.Trim()))
                    {
                        ret.Add(pos.Key);
                        break;
                    }
                }
            }

            return ret.ToArray();
        }

        protected override async Task<RunePage> GetRunePageInner(int championId, Position position)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await WebCache.String(GetChampionURL(championId, position), soft: true));

            var pathIcons = doc.DocumentNode.SelectNodes("//img[contains(@class, 'PathButton__Icon')]").Take(2);
            var pathStyles = pathIcons.Select(GetPathStyleId).ToArray();

            var perkIcons = doc.DocumentNode.SelectNodes("//div[contains(@class, 'LeftSide')]//img[contains(@class, 'PerkButton__Icon')]").Take(9);
            var perkIds = perkIcons.Select(GetPerkId).ToArray();

            return new RunePage(perkIds, pathStyles[0], pathStyles[1], championId, position);
        }

        protected override async Task<ItemSet> GetItemSetInner(int championId, Position position)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await WebCache.String(GetChampionURL(championId, position), soft: true));
            
            var buildWrappers = doc.DocumentNode.Descendants().Where(o => o.HasClass("build-wrapper")).Reverse();

            var blocks = new List<ItemSet.SetBlock>();

            foreach (var wrapper in buildWrappers)
            {
                string title = wrapper.PreviousSibling.PreviousSibling.InnerText;
                var items = new List<int>();
                string[] stats = wrapper.Descendants()
                    .Where(o => o.Name == "strong" && o.ParentNode.HasClass("build-text"))
                    .Select(o => o.InnerText)
                    .ToArray();

                foreach (var item in wrapper.Descendants().Where(o => o.Name == "img"))
                {
                    items.Add(ParseItem(item));
                }

                blocks.Add(new ItemSet.SetBlock
                {
                    Items = items.ToArray(),
                    Name = $"{title} ({stats[0]} win rate | {stats[1]} games)"
                });
            }

            return new ItemSet
            {
                Champion = championId,
                Position = position,
                Blocks = blocks.ToArray(),
                Name = this.Name + ": " + position.ToString()
            };

            int ParseItem(HtmlNode aNode)
            {
                return int.Parse(aNode.GetAttributeValue("data-id", ""));
            }
        }

        protected override async Task<string> GetSkillOrderInner(int championId, Position position)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await WebCache.String(GetChampionURL(championId, position), soft: true));

            var skillOrder = doc.DocumentNode.Descendants().Where(o => o.HasClass("skill-order")).Last();
            
            char[] skills = new char[18];

            foreach (var skill in skillOrder.Descendants().Where(o => o.HasClass("skill-selections")))
            {
                int i = 0;
                foreach (var item in skill.Descendants("div"))
                {
                    if (item.HasClass("selected"))
                        skills[i] = item.Descendants("span").First().InnerText[0];

                    i++;
                }
            }

            return new string(skills);
        }

        private static int GetPerkId(HtmlNode node)
        {
            string src = node.GetAttributeValue("src", "");
            var rune = Riot.GetAllRunes().Values.FirstOrDefault(o => src.Contains(o.IconURL))?.ID;

            if (rune == null)
            {
                return int.Parse(Regex.Match(src, @"(?<=rune-shards\/).*?(?=\.png)").Value);
            }

            return rune.Value;
        }

        private static int GetPathStyleId(HtmlNode node)
        {
            string src = node.GetAttributeValue("src", "");
            return Riot.GetRuneTrees().Result.First(o => src.Contains(o.IconURL)).ID;
        }
    }
}
