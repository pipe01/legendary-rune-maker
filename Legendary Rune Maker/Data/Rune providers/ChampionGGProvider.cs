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
            => $"https://champion.gg/champion/{Riot.GetChampion(championId).Name}/"
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

        protected override Task<RunePage> GetRunePageInner(int championId, Position position)
        {
            throw new NotImplementedException();
        }
    }
}
