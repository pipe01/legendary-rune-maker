using Anotar.Log4Net;
using LCU.NET;
using LCU.NET.API_Models;
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
        private LolChampSelectChampSelectSession Session;

        private LolChampSelectChampSelectPlayerSelection PlayerSelection
            => Session.myTeam?.SingleOrDefault(o => o.cellId == Session.localPlayerCellId);
        private Position CurrentPosition => PlayerSelection?.assignedPosition.ToPosition() ?? Position.Fill;

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
            if (eventType != EventType.Delete && State.Value?.LockedInChamp != data)
            {
                State.Value.LockedInChamp = data;

                LogTo.Info("Locked in champion {0}", data);
                GameState.State.Fire(GameTriggers.LockIn);

                if (!Enabled)
                    return;

                var tasks = new List<Task>();
                
                if (Config.UploadOnLock)
                    tasks.Add(Actuator.UploadRunePage(CurrentPosition, data));
                    
                if (Config.ShowSkillOrder && !Config.SetItemSet)
                    tasks.Add(Actuator.UploadSkillOrder(CurrentPosition, data));
                    
                if (Config.SetItemSet)
                    tasks.Add(Actuator.UploadItemSet(CurrentPosition, data));

                await Task.WhenAll(tasks.ToArray());
            }
        }

        private async void ChampSelectUpdate(EventType eventType, LolChampSelectChampSelectSession data)
        {
            Session = data;

            if (eventType == EventType.Create || (eventType == EventType.Update && !State.HasValue))
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
            
            if (!State.HasValue)
            {
                LogTo.Error("State is empty!");
                return;
            }

            if (PlayerSelection != null && Config.AutoPickSumms && !State.Value.HasPickedSumms)
            {
                State.Value.HasPickedSumms = true;

                await Actuator.PickSummoners(CurrentPosition);
            }


            var actions = data.actions.SelectMany(o => o).ToArray();
            var myAction = actions.FirstOrDefault(o => o.actorCellId == data.localPlayerCellId && !o.completed);

            if (myAction?.completed != false)
                return;

            LogTo.Debug("Incomplete user action found");

            if (myAction.type == "pick" && !State.Value.HasPickedChampion)
            {
                int index = Array.IndexOf(actions, myAction);
                var prev = index > 0 ? actions[index - 1] : null;

                if (prev == null || prev.completed)
                {
                    State.Value.HasPickedChampion = true;
                    LogTo.Debug("User must pick");

                    if (Config.AutoPickChampion)
                    {
                        await Task.Delay(Config.DelayBeforeAction);
                        await Actuator.PickChampion(CurrentPosition, myAction);
                    }
                }
            }
            else if (myAction.type == "ban" && !State.Value.HasBanned && Session.timer.phase.Contains("BAN"))
            {
                State.Value.HasBanned = true;
                LogTo.Debug("User must ban");

                if (Config.AutoBanChampion)
                {
                    await Task.Delay(Config.DelayBeforeAction);
                    await Actuator.BanChampion(CurrentPosition, myAction, Session.myTeam);
                }
            }
        }

        private void UpdateMainPage()
        {
            if (!Enabled)
                return;

            Actuator.Main.SafeInvoke(async () =>
            {
                if (PlayerSelection != null)
                {
                    if (!State.Value.HasSetPositionUI)
                    {
                        LogTo.Debug("Set position: {0}", CurrentPosition);
                        Actuator.Main.SelectedPosition = CurrentPosition;
                    }

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
