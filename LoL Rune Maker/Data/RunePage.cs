using LCU.NET.Plugins.LoL;
using LoL_Rune_Maker.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoL_Rune_Maker.Data
{
    public class RunePage
    {
        public int[] RuneIDs { get; set; }
        public int PrimaryTree { get; set; }
        public int SecondaryTree { get; set; }

        public int ChampionID { get; set; }
        public Position Position { get; set; }

        public RunePage()
        {
        }

        public RunePage(int[] runeIDs, int primaryTree, int secondaryTree, int championId, Position position)
        {
            this.RuneIDs = runeIDs;
            this.PrimaryTree = primaryTree;
            this.SecondaryTree = secondaryTree;
            this.ChampionID = championId;
            this.Position = position;
        }

        public async Task UploadToClient()
        {
            if (!GameState.CanUpload)
                return;

            var page = await Perks.GetCurrentPageAsync();

            page.primaryStyleId = PrimaryTree;
            page.subStyleId = SecondaryTree;
            page.selectedPerkIds = RuneIDs;
            page.name = (await Riot.GetChampions()).Single(o => o.ID == ChampionID).Name + " - " + Enum.GetName(typeof(Position), Position);

            await Perks.PutPageAsync(page.id, page);
        }
    }
}
