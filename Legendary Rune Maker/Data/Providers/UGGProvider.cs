//Mad props to @paolostyle for https://github.com/OrangeNote/RuneBook/pull/67

using Legendary_Rune_Maker.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Providers
{
    internal class UGGProvider : Provider
    {
        public override string Name => "U.GG";
        public override Options ProviderOptions => Options.RunePages | Options.ItemSets | Options.SkillOrder;

        private const string LolVersionsEndpoint = "https://ddragon.leagueoflegends.com/api/versions.json";

        private const int OverviewWorld = 12;
        private const int OverviewPlatPlus = 10;

        private const string UGGApiVersion = "1.1";
        private const string UGGDataVersion = "1.2";

        private readonly static IDictionary<int, Position> IdToPosition = new Dictionary<int, Position>
        {
            [1] = Position.Jungle,
            [2] = Position.Support,
            [3] = Position.Bottom,
            [4] = Position.Top,
            [5] = Position.Mid
        };
        
        private string _LolUGGVersion;
        private async Task<string> GetLolUGGVersion()
        {
            if (_LolUGGVersion == null)
            {
                _LolUGGVersion = JArray.Parse(await Client.DownloadStringTaskAsync(LolVersionsEndpoint))[0].ToObject<string>();

                //8.23.1 -> 8_23
                _LolUGGVersion = string.Join("_", _LolUGGVersion.Split('.').Take(2));
            }

            return _LolUGGVersion;
        }

        private string _UGGOverviewVersion;
        private async Task<string> GetUGGOverviewVersion()
        {
            if (_UGGOverviewVersion == null)
            {
                var url = $"https://u.gg/json/new_ugg_versions/{UGGDataVersion}.json";

                var json = JObject.Parse(await Client.DownloadStringTaskAsync(url));

                _UGGOverviewVersion = json["latest"]["overview"].ToObject<string>();
            }

            return _UGGOverviewVersion;

        }

        private IDictionary<int, JObject> ChampionData = new Dictionary<int, JObject>();
        protected async Task<JObject> GetChampionData(int championId)
        {
            if (!ChampionData.TryGetValue(championId, out var data))
            {
                string url = $"https://stats.u.gg/lol/{UGGApiVersion}/overview/{await GetLolUGGVersion()}/ranked_solo_5x5/{championId}/{await GetUGGOverviewVersion()}.json";

                var json = JObject.Parse(await Client.DownloadStringTaskAsync(url));
                ChampionData[championId] = data = (JObject)json[OverviewWorld.ToString()][OverviewPlatPlus.ToString()];
            }

            return data;
        }

        protected override async Task<Position[]> GetPossibleRolesInner(int championId)
        {
            JToken champData = await GetChampionData(championId);
            int totalGames = champData.Sum(o => ((JProperty)o).Value[0][0][0].ToObject<int>());
            
            //Only count positions that make up for more than 10% of the champion's total played games
            return champData
                .Cast<JProperty>()
                .Select((o, i) => o.Value[0][0][0].ToObject<float>() / totalGames > 0.1f ? IdToPosition[i + 1] : Position.UNSELECTED)
                .Where(o => o != Position.UNSELECTED)
                .ToArray();
        }

        protected override async Task<RunePage> GetRunePageInner(int championId, Position position)
        {
            var champData = await GetChampionData(championId);

            if (position == Position.Fill || position == Position.UNSELECTED)
                position = (await GetPossibleRoles(championId))[0];

            var root = champData[IdToPosition.Invert()[position].ToString()][0];
            var perksRoot = root[0];

            int primTree = perksRoot[2].ToObject<int>();
            int secTree = perksRoot[3].ToObject<int>();
            int[] runes =     /*Runes*/ perksRoot[4].Select(o => o.ToObject<int>())
                .Concat(/*Stat shards*/ root[8][2].Select(o => int.Parse(o.ToString())))
                .ToArray();

            return new RunePage(runes, primTree, secTree, championId, position);
        }

        protected override async Task<ItemSet> GetItemSetInner(int championId, Position position)
        {
            var champData = await GetChampionData(championId);

            if (position == Position.Fill || position == Position.UNSELECTED)
                position = (await GetPossibleRoles(championId))[0];

            var root = champData[IdToPosition.Invert()[position].ToString()][0];
            
            var blocks = new List<ItemSet.SetBlock>();
            
            AddSimple(2, "Starting Build");
            AddSimple(3, "Core Build");

            blocks.AddRange(root[5].Select((o, i) => new ItemSet.SetBlock
            {
                Name = new[] { "Fourth", "Fifth", "Sixth" }[i] + " Item Options (ordered by games played)",
                Items = o.Select(j => j[0].ToObject<int>()).ToArray()
            }));

            return new ItemSet
            {
                Champion = championId,
                Name = "U.GG - {0}",
                Position = position,
                Blocks = blocks.ToArray()
            };

            void AddSimple(int index, string name)
            {
                blocks.Add(new ItemSet.SetBlock
                {
                    Name = $"{name} - {root[index][1].ToObject<float>() / root[index][0].ToObject<int>() * 100}% win rate",
                    Items = root[index][2].Select(o => o.ToObject<int>()).ToArray()
                });
            }
        }

        protected override async Task<string> GetSkillOrderInner(int championId, Position position)
        {
            var champData = await GetChampionData(championId);

            if (position == Position.Fill || position == Position.UNSELECTED)
                position = (await GetPossibleRoles(championId))[0];

            var root = champData[IdToPosition.Invert()[position].ToString()][0][4];

            var skills = root[2].Select(o => o.ToObject<string>()[0]).ToArray();
            string @short = root[3].ToObject<string>();

            return $"{@short} {new string(skills)}";
        }
    }
}
