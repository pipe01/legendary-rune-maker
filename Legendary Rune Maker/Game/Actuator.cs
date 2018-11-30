using Anotar.Log4Net;
using LCU.NET;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Data.Providers;
using Legendary_Rune_Maker.Utils;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Game
{
    public partial class Actuator
    {
        public class State
        {
            public bool HasLockedIn,
                        HasPickedChampion,
                        HasPickedSumms,
                        HasBanned,
                        HasTriedSkillOrder,
                        HasTriedRunePage,
                        HasTriedItemSet;
        }

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

        private Container<State> CurrentState = new Container<State>();

        private readonly ILoL LoL;
        private Config Config;

        public Actuator(ILoL lol, Config config)
        {
            this.LoL = lol;
            this.Config = config;
        }

        public async Task Init(Detector[] detectors)
        {
            LogTo.Debug("Initializing actuator");

            GameState.State.EnteredState += State_EnteredState;
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

            foreach (var item in detectors)
            {
                await item.Init(CurrentState);
            }

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
    }
}
