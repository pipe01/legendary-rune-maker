using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Rune_providers
{
    internal class ChampionGGProvider : RuneProvider
    {
        public override string Name => "Champion.GG";

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
            doc.LoadHtml(await new WebClient().DownloadStringTaskAsync(GetChampionURL(championId)));

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
            doc.LoadHtml(await new WebClient().DownloadStringTaskAsync(GetChampionURL(championId, position)));

            var pathIcons = doc.DocumentNode.SelectNodes("//img[contains(@class, 'PathButton__Icon')]").Take(2);
            var pathStyles = pathIcons.Select(GetPathStyleId).ToArray();

            var perkIcons = doc.DocumentNode.SelectNodes("//div[contains(@class, 'LeftSide')]//img[contains(@class, 'PerkButton__Icon')]").Take(6);
            var perkIds = perkIcons.Select(GetPerkId).ToArray();

            return new RunePage(perkIds, pathStyles[0], pathStyles[1], championId, position);
        }

        private static int GetPerkId(HtmlNode node)
        {
            string src = node.GetAttributeValue("src", "");
            return Riot.GetAllRunes().Values.First(o => src.Contains(o.IconURL)).ID;
        }

        private static int GetPathStyleId(HtmlNode node)
        {
            string src = node.GetAttributeValue("src", "");
            return Riot.GetRuneTrees().Result.First(o => src.Contains(o.IconURL)).ID;
        }
    }
}
