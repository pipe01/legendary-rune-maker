using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Rune_providers
{
    internal class RunesLolProvider : IRuneProvider
    {
        private static readonly IDictionary<Position, string> PositionToName = new Dictionary<Position, string>
        {
            [Position.Top] = "Top",
            [Position.Jungle] = "Jungle",
            [Position.Mid] = "Middle",
            [Position.Support] = "Support",
            [Position.Bottom] = "ADC",
            [Position.Fill] = ""
        };

        private static async Task<string> GetChampionKey(int championId)
            => (await Riot.GetChampions()).Single(o => o.ID == championId).Key;

        private static async Task<string> GetRoleUrl(int championId, Position position)
            => $"https://runes.lol/ranked/gold/champion/win/{await GetChampionKey(championId)}/{PositionToName[position]}";

        public async Task<IEnumerable<Position>> GetPossibleRoles(int championId)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await new WebClient().DownloadStringTaskAsync(await GetRoleUrl(championId, Position.Fill)));

            var n = doc.DocumentNode.SelectNodes("//a[contains(@class, 'lanefilter')]");

            var ret = new List<Position>();

            foreach (var item in n)
            {
                string img = item.ChildNodes["img"].GetAttributeValue("src", "");

                foreach (var pos in PositionToName)
                {
                    if (img.Contains(pos.Value.ToLower()))
                    {
                        ret.Add(pos.Key);
                        break;
                    }
                }
            }

            return ret;
        }

        public async Task<RunePage> GetRunePage(int championId, Position position)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await new WebClient().DownloadStringTaskAsync(await GetRoleUrl(championId, position)));

            var runeClasses = doc.DocumentNode.SelectNodes("//div[@class='runeclassicon']");

            if (runeClasses.Count != 2)
                throw new FormatException("More than 2 rune trees found.");

            var runes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'tip')]");

            if (runes.Count != 6)
                throw new FormatException("Couldn't find all runes.");

            return new RunePage
            {
                ChampionID = championId,
                Position = position,
                PrimaryTree = GetRuneClass(runeClasses[0]),
                SecondaryTree = GetRuneClass(runeClasses[1]),
                RuneIDs = runes.Select(o => int.Parse(o.GetAttributeValue("data-id", "0"))).ToArray()
            };

            int GetRuneClass(HtmlNode node) => int.Parse(Regex.Match(node.ChildNodes[0].GetAttributeValue("src", ""), @"(?<=\/)\d+(?=\.)").Value);
        }
    }
}
