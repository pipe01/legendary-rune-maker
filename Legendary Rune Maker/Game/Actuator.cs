using Anotar.Log4Net;
using LCU.NET;
using LCU.NET.API_Models;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Data.Providers;
using Legendary_Rune_Maker.Locale;
using Legendary_Rune_Maker.Utils;
using Notifications.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            new UGGProvider(),
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

            this.ChampSelectDetector.Actuator = this;
        }

        public async Task Init()
        {
            LogTo.Debug("Initializing actuator");

            GameState.State.EnteredState += State_EnteredState;
            ChampSelectDetector.SessionUpdated += ChampSelectDetector_SessionUpdated;
            LeagueClient.ConnectedChanged += LeagueClient_ConnectedChanged;

            if (!LeagueClient.Init())
            {
                LogTo.Info("League client not open, listening");
                LeagueClient.BeginTryInit();
            }
            else
            {
                LogTo.Info("Connected to league client");
            }

            LogTo.Debug("Initializing detectors");

            await LoginDetector.Init();
            await ChampSelectDetector.Init();
            ReadyCheckDetector.Init();

            LogTo.Debug("Initialized detectors");
        }
        
        private void LeagueClient_ConnectedChanged(bool connected)
        {
            LogTo.Debug("Connected: " + connected);

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
                LogTo.Info("Disconncted from client, trying to reconnect");

                await Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    LeagueClient.BeginTryInit();
                });
            }
        }
        
        private void ChampSelectDetector_SessionUpdated(LolChampSelectChampSelectSession obj)
        {
            var player = ChampSelectDetector.Session.PlayerSelection;

            Main.SafeInvoke(async () =>
            {
                if (player != null && Main.Attached)
                {
                    var pos = player.assignedPosition.ToPosition();

                    LogTo.Debug("Set position: {0}", pos);
                    Main.SelectedPosition = pos;

                    if (player.championId != 0)
                    {
                        LogTo.Debug("Set champion: {0}", player.championId);
                        await Main.SetChampion(player.championId);
                    }
                }
            });
        }
    }
}
