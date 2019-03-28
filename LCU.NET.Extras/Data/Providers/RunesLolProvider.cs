using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Providers
{
    internal class RunesLolProvider : Provider
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

        public override string Name => "Runes.lol";
        public override Options ProviderOptions => Options.RunePages;

        private static string GetChampionKey(int championId) => Riot.GetChampion(championId).Key;

        private static string GetRoleUrl(int championId, Position position)
            => $"https://runes.lol/ranked/gold/champion/win/{GetChampionKey(championId)}/{PositionToName[position]}";

        public override async Task<Position[]> GetPossibleRoles(int championId)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await WebCache.String(GetRoleUrl(championId, Position.Fill), soft: true));

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

            return ret.ToArray();
        }

        public override async Task<RunePage> GetRunePage(int championId, Position position)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await WebCache.String(GetRoleUrl(championId, position), soft: true));

            var runeClasses = doc.DocumentNode.SelectNodes("//div[@class='runeclassicon']");

            if (runeClasses.Count != 2)
                throw new FormatException("More than 2 rune trees found.");

            var runes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'tip')]");

            if (runes.Count != 6)
                throw new FormatException("Couldn't find all runes.");

            var page = new RunePage
            {
                ChampionID = championId,
                Position = position,
                PrimaryTree = GetRuneClass(runeClasses[0]),
                SecondaryTree = GetRuneClass(runeClasses[1]),
                RuneIDs = runes.Select(o => int.Parse(o.GetAttributeValue("data-id", "0"))).ToArray()
            };

            page.PrimaryTree = Swap(page.PrimaryTree, 8300, 8400);
            page.SecondaryTree = Swap(page.SecondaryTree, 8300, 8400);

            return page;

            int GetRuneClass(HtmlNode node) => int.Parse(Regex.Match(node.ChildNodes[0].GetAttributeValue("src", ""), @"(?<=\/)\d+(?=\.)").Value);

            int Swap(int v, int a, int b) => v == a ? b : v == b ? a : v;
        }
    }
}
