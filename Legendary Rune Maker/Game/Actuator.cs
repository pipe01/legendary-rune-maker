using LCU.NET;
using LCU.NET.API_Models;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Data.Providers;
using Legendary_Rune_Maker.Locale;
using Legendary_Rune_Maker.Utils;
using Notifications.Wpf;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Game
{
    public class Actuator
    {
        internal static readonly Provider[] RuneProviders = new Provider[]
        {
            new ClientProvider(),
            new RunesLolProvider(),
            new ChampionGGProvider(),
            new MetaLolProvider(),
            new LolFlavorProvider(),
            new OpGGProvider()
        };

        public IMainWindow Main { get; set; }

        private ILeagueClient LeagueClient => LoL.Client;

        private readonly ILoL LoL;
        private readonly ChampSelectDetector ChampSelectDetector;
        private readonly LoginDetector LoginDetector;
        private readonly ReadyCheckDetector ReadyCheckDetector;

        public Actuator(ILoL lol, ChampSelectDetector champSelectDetector, LoginDetector loginDetector, ReadyCheckDetector readyCheckDetector)
        {
            this.LoL = lol;
            this.ChampSelectDetector = champSelectDetector;
            this.LoginDetector = loginDetector;
            this.ReadyCheckDetector = readyCheckDetector;
        }

        public async Task Init()
        {
            GameState.State.EnteredState += State_EnteredState;
            ChampSelectDetector.SessionUpdated += ChampSelectDetector_SessionUpdated;
            LeagueClient.ConnectedChanged += LeagueClient_ConnectedChanged;

            if (!LeagueClient.Init())
            {
                LeagueClient.BeginTryInit();
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
                    LeagueClient.BeginTryInit();
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
            var provider = Array.Find(RuneProviders, o => o.Name == Config.Default.ItemSetProvider) ?? RuneProviders[0];
            var set = await provider.GetItemSet(Main.SelectedChampion, Main.SelectedPosition);

            await set.UploadToClient(LoL.Login, LoL.ItemSets);

            Main.ShowNotification(Text.UploadedItemSet,
                Text.UploadedItemSetFrom.FormatStr(provider.Name), NotificationType.Success);
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

            await Task.Run(() => Main.Page.UploadToClient(LoL.Perks));
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
