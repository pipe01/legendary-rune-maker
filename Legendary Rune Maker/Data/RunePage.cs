using LCU.NET.Plugins.LoL;
using Legendary_Rune_Maker.Game;
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

            var page = await Perks.GetCurrentPageAsync();

            if (!page.isEditable)
            {
                MainWindow.NotificationManager.Show(new NotificationContent
                {
                    Title = "Couldn't upload rune page",
                    Message = "Make sure the active rune page is editable.",
                    Type = NotificationType.Error
                });
                return;
            }

            page.primaryStyleId = PrimaryTree;
            page.subStyleId = SecondaryTree;
            page.selectedPerkIds = RuneIDs;
            page.name = this.Name ?? (await Riot.GetChampions()).Single(o => o.ID == ChampionID).Name + " - " + Enum.GetName(typeof(Position), Position);

            await Perks.PutPageAsync(page.id, page);
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
