using LCU.NET;
using LCU.NET.API_Models;
using LCU.NET.Plugins.LoL;
using Legendary_Rune_Maker.Game;
using Legendary_Rune_Maker.Locale;
using Notifications.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data
{
    public class RunePage
    {
        public int[] RuneIDs { get; set; }
        public int PrimaryTree { get; set; }
        public int SecondaryTree { get; set; }

        public string Name { get; set; }

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

            var page = new LolPerksPerkPageResource
            {
                primaryStyleId = PrimaryTree,
                subStyleId = SecondaryTree,
                selectedPerkIds = RuneIDs,
                name = this.Name ?? Riot.GetChampion(ChampionID).Name + " - " + Enum.GetName(typeof(Position), Position)
            };

            try
            {
                await Perks.PostPageAsync(page);
            }
            catch (APIErrorException ex) when (ex.Message == "Max pages reached")
            {
                //The maximum number of pages has been reached, try to delete current page and upload again

                var currentPage = await Perks.GetCurrentPageAsync();

                if (currentPage.isDeletable)
                {
                    await Perks.DeletePageAsync(currentPage.id);
                    await UploadToClient();
                }
                else
                {
                    MainWindow.ShowNotification(Text.CantUploadPageTitle, Text.CantUploadPageMessage);
                    return;
                }
            }
        }

        public static async Task<RunePage> GetActivePageFromClient()
        {
            var page = await Perks.GetCurrentPageAsync();

            return new RunePage(page.selectedPerkIds, page.primaryStyleId, page.subStyleId, 0, Position.Fill)
            {
                Name = page.name
            };
        }
    }
}
