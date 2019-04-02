using Anotar.Log4Net;
using LCU.NET;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Data.Providers;
using Legendary_Rune_Maker.Utils;
using Ninject;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Game
{
	public partial class Actuator
	{
		public class State
		{
			public int LockedInChamp;
			public bool HasLockedIn,
						HasPickedChampion,
						HasPickedSumms,
						HasBanned,
						HasSetPositionUI,
						HasSetIntent;
		}

		public static List<Provider> RuneProviders = new List<Provider>
		{
			new ClientProvider(),
			new ChampionGGProvider(),
			new MetaLolProvider(),
			new LolFlavorProvider(),
			new UGGProvider(),
			new OpGGProvider(),
			new RunesLolProvider()
		};

		public IUiActuator Main { get; set; }

		private ILeagueClient LeagueClient => LoL.Client;

		private readonly Container<State> CurrentState = new Container<State> ();

		[Inject]
		public Detector[] Detectors { get; set; }

		private bool _Enabled = true;
		public bool Enabled {
			get => _Enabled;
			set {
				_Enabled = value;

				foreach (var item in Detectors) {
					item.Enabled = value;
				}
			}
		}

		private readonly ILoL LoL;
		private readonly Container<Config> ConfigContainer;

		private Config Config => ConfigContainer;

		public Actuator (ILoL lol, Container<Config> config)
		{
			this.LoL = lol;
			this.ConfigContainer = config;
		}

		public async Task Init ()
		{
			LogTo.Debug ("Initializing actuator");

			GameState.State.EnteredState += State_EnteredState;
			LeagueClient.ConnectedChanged += LeagueClient_ConnectedChanged;

			if (!LeagueClient.Init ()) {
				LogTo.Info ("League client not open, listening");
				LeagueClient.BeginTryInit ();
			} else {
				LogTo.Info ("Connected to league client");
			}

			LogTo.Debug ("Initializing detectors");

			foreach (var item in Detectors) {
				await item.Init (CurrentState);
			}

			LogTo.Debug ("Initialized detectors");
		}

		private void LeagueClient_ConnectedChanged (bool connected)
		{
			LogTo.Debug ("Connected: " + connected);

			if (connected) {
				GameState.State.Fire (GameTriggers.OpenGame);
			} else {
				GameState.State.Fire (GameTriggers.CloseGame);
			}
		}

		private async void State_EnteredState (GameStates state)
		{
			Main.SafeInvoke (() => Main.SetState (state));

			if (state == GameStates.Disconnected) {
				LogTo.Info ("Disconncted from client, trying to reconnect");

				await Task.Run (async () => {
					await Task.Delay (1000);
					LeagueClient.BeginTryInit ();
				});
			}
		}
	}
}
