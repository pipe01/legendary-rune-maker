using Anotar.Log4Net;
using LCU.NET;
using LCU.NET.API_Models;
using LCU.NET.Extras.Utils;
using LCU.NET.Plugins.LoL;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Game
{
	public class ChampSelectDetector : Detector
	{
		private struct SessionData
		{
			public LolChampSelectChampSelectSession Data { get; }

			public bool IsARAM => Data.actions.Length == 0;

			public LolChampSelectChampSelectPlayerSelection Player {
				get {
					var @this = this; //wtf c#
					return Data.myTeam?.SingleOrDefault (o => o.cellId == @this.Data.localPlayerCellId);
				}
			}

			public Position Position => Player?.assignedPosition.ToPosition () ?? Position.Fill;

			public SessionData (LolChampSelectChampSelectSession data)
			{
				this.Data = data;
			}
		}

		private const int MinimumActionDelay = 5000;

		private SessionData Session;

		private readonly Container<Config> ConfigContainer;
		private readonly Actuator Actuator;

		private Config Config => ConfigContainer.Value;

		public ChampSelectDetector (ILoL lol, Container<Config> config, Actuator actuator) : base (lol)
		{
			this.ConfigContainer = config;
			this.Actuator = actuator;
		}

		protected override async Task Init ()
		{
			await LoL.Socket.SubscribeAndUpdate<LolChampSelectChampSelectSession> (ChampSelect.Endpoint, ChampSelectUpdate);
			await LoL.Socket.SubscribeAndUpdate<int> ("/lol-champ-select/v1/current-champion", CurrentChampionUpdate);
		}

		private async void CurrentChampionUpdate (EventType eventType, int data)
		{
			if (eventType != EventType.Delete && State.Value?.LockedInChamp != data) {
				try {
					State.Value.LockedInChamp = data;

					LogTo.Info ("Locked in champion {0}", data);
					GameState.State.Fire (GameTriggers.LockIn);

					if (!Enabled)
						return;

					var tasks = new List<Task> ();

					if (Config.UploadOnLock) {
						tasks.Add (Actuator.UploadRunes (Session.Position, data));
						tasks.Add (Actuator.UploadSpells (Session.Position, data));
					}

					if (Config.ShowSkillOrder && !Config.SetItemSet)
						tasks.Add (Actuator.UploadSkillOrder (Session.Position, data));

					if (Config.SetItemSet)
						tasks.Add (Actuator.UploadItemSet (Session.Position, data));

					await Task.WhenAll (tasks.ToArray ());
				} catch (Exception ex) {
					LogTo.Error ($"Error while locking the champion. ChampionId: {data}. EventType: {eventType} - {ex}");
				}
			}
		}

		private async void ChampSelectUpdate (EventType eventType, LolChampSelectChampSelectSession data)
		{
			Session = new SessionData (data);

			if (eventType == EventType.Create || (eventType == EventType.Update && !State.HasValue)) {
				State.Value = new Actuator.State ();
				GameState.State.Fire (GameTriggers.EnterChampSelect);
			} else if (eventType == EventType.Delete) {
				State.Value = null;
				GameState.State.Fire (GameTriggers.ExitChampSelect);
				return;
			}

			if (data == null) {
				LogTo.Debug ("Null data");
				return;
			}

			UpdateMainPage ();

			if (!State.HasValue) {
				LogTo.Error ("State is empty!");
				return;
			}

			var actions = data.actions.SelectMany (o => o).ToArray ();

			if (Session.Player != null) {
				if (Config.AutoPickSumms && !State.Value.HasPickedSumms) {
					State.Value.HasPickedSumms = true;

					await Actuator.PickSummoners (Session.Position);
				}

				if (!State.Value.HasSetIntent && Config.AutoPickChampion && !Session.IsARAM) {
					State.Value.HasSetIntent = true;

					var action = actions.FirstOrDefault (o => o.actorCellId == data.localPlayerCellId && o.type == "pick" && !o.completed);

					if (action != null) {
						await Task.Delay (Config.DelayBeforeIntentSet);
						await Actuator.PickChampion (Session.Position, action, true);
					}
				}
			}

			var myAction = actions.FirstOrDefault (o => o.actorCellId == data.localPlayerCellId && !o.completed);

			if (myAction?.completed != false || Session.IsARAM)
				return;

			LogTo.Debug ("Incomplete user action found");

			if (myAction.type == "pick" && !State.Value.HasPickedChampion) {
				bool pick = false;

				if (data.actions.Count (o => o.All (i => i.type == "pick")) == 1) {
					//Blind pick mode

					if (data.actions.Length > 1) {
						//There is a ban phase

						//Second-to-last
						var phaseBeforePick = data.actions[data.actions.Length - 2];

						pick = phaseBeforePick.All (o => o.completed);
					} else {
						pick = true;
					}
				} else {
					//Draft pick mode

					var nextAction = data.actions.FirstOrDefault (o => o.All (i => i.type == "pick") && !o.All (i => i.completed));

					pick = nextAction?.Any (o => o.actorCellId == data.localPlayerCellId) ?? false;
				}

				if (pick) {
					State.Value.HasPickedChampion = true;
					LogTo.Debug ("User must pick");

					if (Config.AutoPickChampion) {
						await Task.Delay (Config.DelayBeforeAction);
						await Actuator.PickChampion (Session.Position, myAction, false);
					}
				}
			} else if (myAction.type == "ban" && !State.Value.HasBanned && Session.Data.timer.phase.Contains ("BAN")) {
				State.Value.HasBanned = true;
				LogTo.Debug ("User must ban");

				if (Config.AutoBanChampion) {
					await Task.Delay (Math.Max (Config.DelayBeforeAction, MinimumActionDelay));
					await Actuator.BanChampion (Session.Position, myAction, Session.Data.myTeam);
				}
			}
		}

		private void UpdateMainPage ()
		{
			if (!Enabled)
				return;

			Actuator.Main.SafeInvoke (async () => {
				if (Session.Player != null) {
					if (!State.Value.HasSetPositionUI) {
						LogTo.Debug ("Set position: {0}", Session.Position);
						Actuator.Main.SelectedPosition = Session.Position;

						State.Value.HasSetPositionUI = true;
					}

					if (Session.Player.championId != 0) {
						LogTo.Debug ("Set champion: {0}", Session.Player.championId);
						await Actuator.Main.SetChampion (Session.Player.championId);
					}
				}
			});
		}
	}
}
