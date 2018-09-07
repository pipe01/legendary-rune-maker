using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
        public override Options ProviderOptions => Options.RunePages;

        private static string GetChampionURL(int championId, Position? pos = null)
            => $"https://www.metalol.net/champions/lol-build-guide/solo-queue/{Riot.GetChampion(championId, "en_US").Name}"
               + (pos != null ? "/" + PositionToName[pos.Value] : "");

        protected override async Task<Position[]> GetPossibleRolesInner(int championId)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await new WebClient().DownloadStringTaskAsync(GetChampionURL(championId)));

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

        protected override async Task<RunePage> GetRunePageInner(int championId, Position position)
        {
            const string StyleRegex = @"(?<=\/)\d+(?=\.)";
            const string PerkRegex = @"perk-images(\/.*?)+(?=\.)";

            var doc = new HtmlDocument();
            doc.LoadHtml(await new WebClient().DownloadStringTaskAsync(GetChampionURL(championId, position)));

            var runeContainers = doc.DocumentNode.Descendants()
                .Where(o => o.HasClass("perk-image-container"))
                .Select(o => o.ChildNodes[1])
                .Take(8).ToArray();

            var ret = new RunePage();
            ret.ChampionID = championId;
            ret.Position = position;
            ret.PrimaryTree = int.Parse(Regex.Match(runeContainers[0].GetAttributeValue("src", ""), StyleRegex).Value);
            ret.SecondaryTree = int.Parse(Regex.Match(runeContainers[5].GetAttributeValue("src", ""), StyleRegex).Value);

            var perks = new List<int>();

            for (int i = 0; i < runeContainers.Length; i++)
            {
                if (i == 0 || i == 5)
                    continue;

                string imageUrl = Regex.Match(runeContainers[i].GetAttributeValue("src", ""), PerkRegex).Value;

                foreach (var item in WrongRuneNames)
                {
                    imageUrl = imageUrl.Replace(item.Item1, item.Item2);
                }

                perks.Add(Riot.GetAllRunes().Values.Single(o => o.IconURL.Contains(imageUrl)).ID);
            }

            ret.RuneIDs = perks.ToArray();

            return ret;
        }
    }
}
