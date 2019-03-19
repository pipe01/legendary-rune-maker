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
        public override bool IsEnabled => false;

        private static readonly IDictionary<Position, string> PositionToName = new Dictionary<Position, string>
        {
            [Position.Top] = "Top",
            [Position.Jungle] = "Jungle",
            [Position.Mid] = "Mid",
            [Position.Support] = "Support",
            [Position.Bottom] = "ADC",
            [Position.Fill] = ""
        };

        public override async Task<Position[]> GetPossibleRoles(int championId)
        {
            string champ = Riot.GetChampion(championId).Key;

            var ret = new SynchronizedCollection<Position>();

            await Task.WhenAll(PositionToName.Select(async item =>
            {
                if (item.Key == Position.Fill)
                    return;

                var data = await WebCache.String($"http://lolflavor.com/champions/{champ}/Recommended/{champ}_{item.Value}_scrape.json", soft: true);

                if (data != null)
                    ret.Add(item.Key);
            }));

            return ret.ToArray();
        }

        public override async Task<ItemSet> GetItemSet(int championId, Position position)
        {
            if (position == Position.Fill)
                position = (await GetPossibleRoles(championId))[0];

            string champ = Riot.GetChampion(championId).Key;
            var json = await WebCache.String($"http://lolflavor.com/champions/{champ}/Recommended/{champ}_{PositionToName[position]}_scrape.json", soft: true);

            if (json == null)
                return null;

            var lolItemSet = JsonConvert.DeserializeObject<LolItemSetsItemSet>(json);

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
