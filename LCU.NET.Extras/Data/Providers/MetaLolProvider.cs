using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Providers
{
    internal class MetaLolProvider : Provider
    {
        private static readonly IDictionary<Position, string> PositionToName = new Dictionary<Position, string>
        {
            [Position.Top] = "Top",
            [Position.Jungle] = "Jungle",
            [Position.Mid] = "Middle",
            [Position.Bottom] = "Bottom",
            [Position.Support] = "Bottom",
            [Position.Fill] = ""
        };

        private static readonly IList<(string, string)> WrongRuneNames = new List<(string, string)>
        {
            ("DangerousGame", "Triumph"),
            ("LastResort/LastResortIcon", "PresenceOfMind/PresenceOfMind"),
            ("Legend_Heroism", "LegendAlacrity/LegendAlacrity"),
            ("Legend_Tenacity", "LegendTenacity/LegendTenacity"),
            ("Legend_Infamy", "LegendBloodline/LegendBloodline"),
        };

        public override string Name => "MetaLol";
        public override Options ProviderOptions => Options.ItemSets | Options.RunePages | Options.SkillOrder;

        private static string GetChampionURL(int championId, Position? pos = null)
            => $"https://www.metalol.net/champions/lol-build-guide/solo-queue/{Riot.GetChampion(championId, "en_US").Name}/"
               + (pos != null && pos != Position.Fill ? PositionToName[pos.Value] : "top"); //Use 'top' as a dummy

        public override async Task<Position[]> GetPossibleRoles(int championId)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await WebCache.String(GetChampionURL(championId), soft: true));

            var tabs = doc.DocumentNode.Descendants().Where(o => o.HasClass("champ-tab") && o.ChildNodes.Count > 1);
            string[] rolesStr = tabs.Select(o => o.FirstChild.InnerText.Trim('\n')).ToArray();

            var ret = new List<Position>();

            foreach (var item in rolesStr)
            {
                foreach (var pos in PositionToName)
                {
                    if (pos.Value.Equals(item, StringComparison.InvariantCultureIgnoreCase))
                    {
                        ret.Add(pos.Key);
                        break;
                    }
                }
            }

            return ret.ToArray();
        }

        public override async Task<RunePage> GetRunePage(int championId, Position position)
        {
            string html = await WebCache.String(GetChampionURL(championId, position), soft: true);
            string perksStr = Regex.Match(html, @"(?<=perks \= ).*(?=;)").Value;
            JToken perksJson = JArray.Parse(perksStr)[0]["tree"];

            return new RunePage
            {
                ChampionID = championId,
                Position = position,
                PrimaryTree = perksJson["perkPrimaryStyle"].ToObject<int>(),
                SecondaryTree = perksJson["perkSubStyle"].ToObject<int>(),
                RuneIDs = Enumerable.Range(0, 6).Select(i => perksJson["perk" + i].ToObject<int>()).ToArray()
            };
        }

        public override async Task<ItemSet> GetItemSet(int championId, Position position)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await WebCache.String(GetChampionURL(championId, position), soft: true));

            var containers = doc.DocumentNode.Descendants()
                .Where(o => o.HasClass("champ-content-container") && !o.HasClass("description"))
                .Take(6)
                .ToArray();

            var blocks = new List<ItemSet.SetBlock>();

            for (int i = 0; i < 5; i += 2)
            {
                blocks.Add(ParseContainerBlock(containers[i], containers[i + 1]));
            }

            return new ItemSet
            {
                Name = this.Name + ": " + position,
                Champion = championId,
                Position = position,
                Blocks = blocks.ToArray()
            };
        }

        private static ItemSet.SetBlock ParseContainerBlock(params HtmlNode[] containers)
        {
            if (containers.Length != 2)
                return null;

            string name = containers[0].ChildNodes.First(o => o.Name == "h3").FirstChild.InnerText;
            int[] items = containers[1].Descendants()
                .Where(o => o.HasClass("champ-item") && o.ParentNode.GetAttributeValue("style", null) == null)
                .Select(o => o.GetAttributeValue("item", 0))
                .ToArray();

            return new ItemSet.SetBlock
            {
                Items = items,
                Name = name
            };
        }

        public override async Task<string> GetSkillOrder(int championId, Position position)
        {
            if (position == Position.Fill)
                position = (await GetPossibleRoles(championId))[0];

            string html = await WebCache.String(GetChampionURL(championId, position), soft: true);
            string perksStr = Regex.Match(html, @"(?<=skills2 \= ).*(?=;)").Value;
            JToken perksJson = JArray.Parse(perksStr)[0]["skills"];

            char[] skills = new[] { 'Q', 'W', 'E', 'R' };

            return new string(perksJson.Select(o => skills[o.ToObject<int>() - 1]).ToArray());
        }
    }
}
