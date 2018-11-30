using Anotar.Log4Net;
using LCU.NET;
using LCU.NET.API_Models;
using LCU.NET.Plugins.LoL;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Utils;
using System.Linq;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Game
{
    public class ChampSelectDetector : Detector
    {
        private LolChampSelectChampSelectSession Session;

        private LolChampSelectChampSelectPlayerSelection PlayerSelection
            => Session.myTeam?.SingleOrDefault(o => o.cellId == Session.localPlayerCellId);
        private Position CurrentPosition => PlayerSelection.assignedPosition.ToPosition();

        private readonly Config Config;
        private readonly Actuator Actuator;

        public ChampSelectDetector(ILoL lol, Config config, Actuator actuator) : base(lol)
        {
            this.Config = config;
            this.Actuator = actuator;
        }

        protected override async Task Init()
        {
            await LoL.Socket.SubscribeAndUpdate<LolChampSelectChampSelectSession>(ChampSelect.Endpoint, ChampSelectUpdate);
            await LoL.Socket.SubscribeAndUpdate<int>("/lol-champ-select/v1/current-champion", CurrentChampionUpdate);
        }

        private async void CurrentChampionUpdate(EventType eventType, int data)
        {
            if (eventType != EventType.Delete && (State.Value?.HasLockedIn == false))
            {
                State.Value.HasLockedIn = true;

                LogTo.Info("Locked in champion");
                GameState.State.Fire(GameTriggers.LockIn);

                if (!State.Value.HasTriedRunePage)
                {
                    State.Value.HasTriedRunePage = true;

                    if (Config.UploadOnLock)
                        await Actuator.UploadRunePage(CurrentPosition, PlayerSelection.championId);
                }

                if (!State.Value.HasTriedSkillOrder)
                {
                    State.Value.HasTriedSkillOrder = true;

                    if (Config.ShowSkillOrder && !Config.SetItemSet)
                        await Actuator.UploadSkillOrder(CurrentPosition, PlayerSelection.championId);
                }

                if (!State.Value.HasTriedItemSet)
                {
                    State.Value.HasTriedItemSet = true;

                    if (Config.SetItemSet)
                        await Actuator.UploadItemSet(CurrentPosition, PlayerSelection.championId);
                }
            }
        }

        private async void ChampSelectUpdate(EventType eventType, LolChampSelectChampSelectSession data)
        {
            Session = data;

            if (eventType == EventType.Create)
            {
                State.Value = new Actuator.State();
                GameState.State.Fire(GameTriggers.EnterChampSelect);
            }
            else if (eventType == EventType.Delete)
            {
                State.Value = null;
                GameState.State.Fire(GameTriggers.ExitChampSelect);
                return;
            }

            if (data == null)
            {
                LogTo.Debug("Null data");
                return;
            }

            UpdateMainPage();

            if (PlayerSelection != null && Config.AutoPickSumms && !State.Value.HasPickedSumms)
            {
                State.Value.HasPickedSumms = true;

                await Actuator.PickSummoners(CurrentPosition);
            }

            var myAction = data.actions.SelectMany(o => o).LastOrDefault(o => o.actorCellId == data.localPlayerCellId);

            if (myAction?.completed != false)
                return;

            LogTo.Debug("Incomplete user action found");

            if (myAction.type == "pick" && !State.Value.HasPickedChampion)
            {
                State.Value.HasPickedChampion = true;
                LogTo.Debug("User must pick");

                if (Config.Default.AutoPickChampion)
                    await Actuator.PickChampion(CurrentPosition, myAction);
            }
            else if (myAction.type == "ban" && !State.Value.HasBanned)
            {
                State.Value.HasBanned = true;
                LogTo.Debug("User must ban");

                if (Config.Default.AutoBanChampion)
                    await Actuator.BanChampion(CurrentPosition, myAction);
            }
        }

        private void UpdateMainPage()
        {
            Actuator.Main.SafeInvoke(async () =>
            {
                if (PlayerSelection != null && Actuator.Main.Attached)
                {
                    LogTo.Debug("Set position: {0}", CurrentPosition);
                    Actuator.Main.SelectedPosition = CurrentPosition;

                    if (PlayerSelection.championId != 0)
                    {
                        LogTo.Debug("Set champion: {0}", PlayerSelection.championId);
                        await Actuator.Main.SetChampion(PlayerSelection.championId);
                    }
                }
            });
        }
    }
}
