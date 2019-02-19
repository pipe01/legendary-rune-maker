using Anotar.Log4Net;
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

        public async Task UploadToClient(ILogin login, IItemsSets itemSets)
        {
            var session = await login.GetSessionAsync();
            bool saveToConfig = !Config.Default.KeepItemSets;

            if (this.Name.Contains("{0}"))
                this.Name = string.Format(this.Name, Riot.GetChampion(Champion).Name);

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

            LogTo.Debug($"Uploading item set with title '{itemSet.title}' ('{this.Name}' in model)");

            var currentSetInfo = await itemSets.GetItemSets(session.summonerId);
            var currentSetsList = currentSetInfo.itemSets.ToList();

            currentSetsList.Add(itemSet);

            if (saveToConfig && Config.Default.LastItemSetUid != null)
            {
                LogTo.Debug("Deleting last item set with ID {0}", Config.Default.LastItemSetUid);
                currentSetsList.RemoveAll(o => o.uid == Config.Default.LastItemSetUid);
            }

            if (saveToConfig)
            {
                Config.Default.LastItemSetUid = itemSet.uid;
                Config.Default.Save();
            }

            currentSetInfo.itemSets = currentSetsList.ToArray();
            currentSetInfo.timestamp = (long)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;

            await itemSets.PutItemSets(session.summonerId, currentSetInfo);
        }
    }
}