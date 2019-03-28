using Anotar.Log4Net;
using LCU.NET;
using LCU.NET.API_Models;
using LCU.NET.Extras;
using LCU.NET.Plugins.LoL;
using Legendary_Rune_Maker.Game;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private void VerifyRunes()
        {
            var statIds = this.RuneIDs.Where(o => Riot.TreeStructures[PrimaryTree].StatSlots.Any(p => p.Any(q => q.ID == o)));
            this.RuneIDs = GetRunes(PrimaryTree).Concat(GetRunes(SecondaryTree)).Concat(statIds).ToArray();

            IEnumerable<int> GetRunes(int tree)
            {
                foreach (var slot in Riot.TreeStructures[tree].PerkSlots)
                {
                    foreach (var rune in slot)
                    {
                        if (this.RuneIDs.Contains(rune.ID))
                            yield return rune.ID;
                    }
                }
            }
        }

        public async Task UploadToClient(IPerks perks)
        {
            if (!GameState.CanUpload)
                return;

            VerifyRunes();

            var page = new LolPerksPerkPageResource
            {
                primaryStyleId = PrimaryTree,
                subStyleId = SecondaryTree,
                selectedPerkIds = RuneIDs,
                name = this.Name ?? Riot.GetChampion(ChampionID).Name + " - " + Enum.GetName(typeof(Position), Position)
            };

            LogTo.Debug("Uploading rune page with name '{0}' and runes [ {1} ]", page.name, string.Join(", ", RuneIDs));

            if (Config.Current.LastRunePageId != default)
            {
                try
                {
                    await perks.DeletePageAsync(Config.Current.LastRunePageId);
                }
                catch
                {
                }
            }

            try
            {
                var pageRet = await perks.PostPageAsync(page);
                Config.Current.LastRunePageId = pageRet.id;
            }
            catch (APIErrorException ex) when (ex.Message == "Max pages reached")
            {
                LogTo.Info("Max number of rune pages reached, deleting current page and trying again");

                var currentPage = await perks.GetCurrentPageAsync();

                if (currentPage.isDeletable)
                {
                    await perks.DeletePageAsync(currentPage.id);
                    await UploadToClient(perks);
                }
                else
                {
                    LCUApp.MainWindow.ShowNotification("Couldn't upload rune page", "There is no room for more pages.");
                    return;
                }
            }
        }

        public static async Task<RunePage> GetActivePageFromClient(IPerks perks)
        {
            var page = await perks.GetCurrentPageAsync();

            return new RunePage(page.selectedPerkIds, page.primaryStyleId, page.subStyleId, 0, Position.Fill)
            {
                Name = page.name
            };
        }
    }
}
