using LCU.NET;
using LCU.NET.Plugins.LoL;
using System;
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
            
            var itemSet = new LolItemSetsItemSet
            {
                map = "any",
                mode = "any",
                startedFrom = "blank",
                type = "custom",
                associatedChampions = new[] { Champion },
                title = Name,
                blocks = Blocks.Select(o => new LolItemSetsItemSetBlock
                {
                    items = o.Items.Select(i => new LolItemSetsItemSetItem { id = i.ToString(), count = 1 }).ToArray(),
                    type = o.Name
                }).ToArray(),
                uid = Guid.NewGuid().ToString()
            };

            var currentSetInfo = await ItemSets.GetItemSets(session.summonerId);
            var currentSetsList = currentSetInfo.itemSets.ToList();

            currentSetsList.Add(itemSet);

            if (saveToConfig && Config.Default.LastItemSetUid != null)
                currentSetsList.RemoveAll(o => o.uid == Config.Default.LastItemSetUid);

            if (saveToConfig)
            {
                Config.Default.LastItemSetUid = itemSet.uid;
                Config.Default.Save();
            }

            currentSetInfo.itemSets = currentSetsList.ToArray();
            currentSetInfo.timestamp = (long)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;

            await ItemSets.PutItemSets(session.summonerId, currentSetInfo);
        }
    }
}