using LCU.NET;
using LCU.NET.API_Models;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Data.Rune_providers;
using Legendary_Rune_Maker.Locale;
using Legendary_Rune_Maker.Utils;
using Notifications.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Game
{
    public class Actuator
    {
        private readonly IMainWindow Main;

        internal static readonly RuneProvider[] RuneProviders = new RuneProvider[]
        {
            new RunesLolProvider(),
            new ChampionGGProvider(),
            new OpGGProvider(),
            new ClientProvider()
        };

        public Actuator(IMainWindow main)
        {
            this.Main = main;
        }

        public async Task Init()
        {
            GameState.State.EnteredState += State_EnteredState;
            ChampSelectDetector.SessionUpdated += ChampSelectDetector_SessionUpdated;
            LeagueClient.Default.ConnectedChanged += LeagueClient_ConnectedChanged;

            if (!LeagueClient.Default.SmartInit())
            {
                LeagueClient.Default.BeginTryInit();
            }

            await LoginDetector.Init();
            await ChampSelectDetector.Init();
            ReadyCheckDetector.Init();
        }
        
        private void LeagueClient_ConnectedChanged(bool connected)
        {
            Debug.WriteLine("Connected: " + connected);

            if (connected)
            {
                GameState.State.Fire(GameTriggers.OpenGame);
            }
            else
            {
                GameState.State.Fire(GameTriggers.CloseGame);
            }
        }

        private async void State_EnteredState(GameStates state)
        {
            Main.SafeInvoke(() => Main.SetState(state));

            if (state == GameStates.Disconnected)
            {
                await Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    LeagueClient.Default.BeginTryInit();
                });
            }
            else if (state == GameStates.LockedIn)
            {
                await Main.SafeInvoke(async () =>
                {
                    if (Config.Default.UploadOnLock)
                    {
                        await UploadPage();
                    }

                    if (Config.Default.SetItemSet)
                    {
                        await UploadItemSet();
                    }
                });
            }
        }

        private async Task UploadItemSet()
        {
            var provider = RuneProviders.First(o => o.HasItemSets);
            var set = await provider.GetItemSet(Main.SelectedChampion, Main.SelectedPosition);

            await set.UploadToClient();

            Main.ShowNotification("Uploaded item set");
        }

        private async Task UploadPage()
        {
            string champion = Riot.GetChampion(Main.SelectedChampion).Name;

            Main.ShowNotification(Text.LockedInMessage, champion + ", " + Main.SelectedPosition.ToString().ToLower(), NotificationType.Success);

            if (!Main.ValidPage)
            {
                if (Config.Default.LoadOnLock)
                {
                    await Main.LoadPageFromDefaultProvider();
                }
                else
                {
                    Main.ShowNotification(Text.PageChampNotSet.FormatStr(champion), null, NotificationType.Error);
                    return;
                }
            }

            await Task.Run(Main.Page.UploadToClient);
        }

        private void ChampSelectDetector_SessionUpdated(LolChampSelectChampSelectSession obj)
        {
            var player = ChampSelectDetector.CurrentSelection;

            Main.SafeInvoke(async () =>
            {
                if (player != null && Main.Attached)
                {
                    Main.SelectedPosition = player.assignedPosition.ToPosition();

                    if (player.championId != 0)
                        await Main.SetChampion(player.championId);
                }
            });
        }
    }
}
