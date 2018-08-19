using LCU.NET.Plugins.LoL;
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

        public RunePage()
        {
        }

        public RunePage(int[] runeIDs, int primaryTree, int secondaryTree)
        {
            this.RuneIDs = runeIDs;
            this.PrimaryTree = primaryTree;
            this.SecondaryTree = secondaryTree;
        }

        public async Task UploadToClient()
        {
            var page = await Perks.GetCurrentPageAsync();

            page.primaryStyleId = PrimaryTree;
            page.subStyleId = SecondaryTree;
            page.selectedPerkIds = RuneIDs;

            await Perks.PutPageAsync(page.id, page);
        }
    }
}
