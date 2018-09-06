using LCU.NET;
using LCU.NET.Plugins.LoL;
using RestSharp;
using System.Linq;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data
{
    public class ItemSet
    {
        public class SetBlock
        {
            public string Name { get; set; }
            public int[] Items { get; set; }
        }

        public string Name { get; set; }
        public int Champion { get; set; }
        public Position Position { get; set; }

        public SetBlock[] Blocks { get; set; }

        public async Task UploadToClient()
        {
            var session = await Login.GetSessionAsync();
            bool saveToConfig = !Config.Default.KeepItemSets;

            if (saveToConfig && Config.Default.LastItemSetUid != null)
            {
                //Delete last item set

                var itemSets = await ItemSets.GetItemSets(session.summonerId);

                itemSets.itemSets = itemSets.itemSets.Where(o => !o.uid.Equals(Config.Default.LastItemSetUid)).ToArray();

                await ItemSets.PutItemSets(session.summonerId, itemSets);
            }

            var itemSet = new LolItemSetsItemSet
            {
                associatedChampions = new[] { Champion },
                title = Name,
                blocks = Blocks.Select(o => new LolItemSetsItemSetBlock
                {
                    items = o.Items.Select(i => new LolItemSetsItemSetItem { id = i.ToString(), count = 1 }).ToArray(),
                    type = o.Name
                }).ToArray()
            };
            
            await LeagueClient.Default.MakeRequestAsync($"/lol-item-sets/v1/item-sets/{session.summonerId}/sets",
                Method.POST, itemSet, "associatedChampions", "title", "blocks");

            if (saveToConfig)
            {
                var itemSets = await ItemSets.GetItemSets(session.summonerId);

                Config.Default.LastItemSetUid = itemSets.itemSets.Last().uid;
            }
        }
    }
}