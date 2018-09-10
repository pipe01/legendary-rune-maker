using LCU.NET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Providers
{
    internal class LolFlavorProvider : Provider
    {
        public override string Name => "LolFlavor";
        public override Options ProviderOptions => Options.ItemSets;

        private static readonly IDictionary<Position, string> PositionToName = new Dictionary<Position, string>
        {
            [Position.Top] = "Top",
            [Position.Jungle] = "Jungle",
            [Position.Mid] = "Middle",
            [Position.Support] = "Support",
            [Position.Bottom] = "ADC",
            [Position.Fill] = ""
        };

        private static IDictionary<(int, Position), string> Cache = new Dictionary<(int, Position), string>();
        
        protected override async Task<Position[]> GetPossibleRolesInner(int championId)
        {
            string champ = Riot.GetChampion(championId).Key;
            var client = new HttpClient();

            var ret = new SynchronizedCollection<Position>();

            await Task.WhenAll(PositionToName.Select(async item =>
            {
                if (item.Key == Position.Fill)
                    return;

                string url = $"http://lolflavor.com/champions/{champ}/Recommended/{champ}_{item.Value}_scrape.json";
                var data = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));

                if (data.StatusCode != HttpStatusCode.NotFound)
                {
                    Cache[(championId, item.Key)] = await data.Content.ReadAsStringAsync();
                    ret.Add(item.Key);
                }
            }));

            return ret.ToArray();
        }

        protected override async Task<ItemSet> GetItemSetInner(int championId, Position position)
        {
            if (!Cache.TryGetValue((championId, position), out var cacheJson))
            {
                string champ = Riot.GetChampion(championId).Key;

                Cache[(championId, position)] = cacheJson = await new WebClient().DownloadStringTaskAsync(
                    $"http://lolflavor.com/champions/{champ}/Recommended/{champ}_{PositionToName[position]}_scrape.json");
            }

            var lolItemSet = JsonConvert.DeserializeObject<LolItemSetsItemSet>(cacheJson);

            return new ItemSet
            {
                Champion = championId,
                Position = position,
                Name = Name + ": " + position,
                Blocks = lolItemSet.blocks.Select(o => new ItemSet.SetBlock
                {
                    Name = o.type,
                    Items = o.items.Select(i => int.Parse(i.id)).ToArray()
                }).ToArray()
            };
        }
    }
}
