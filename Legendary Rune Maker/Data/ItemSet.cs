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

            var session = await Login.GetSessionAsync();

            await LeagueClient.Default.MakeRequestAsync($"/lol-item-sets/v1/item-sets/{session.summonerId}/sets",
                Method.POST, itemSet, "associatedChampions", "title", "blocks");
            //await ItemSets.PostItemSet(session.summonerId, itemSet);
        }
    }
}