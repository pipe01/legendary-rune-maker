using Anotar.Log4Net;
using LCU.NET;
using LCU.NET.API_Models;
using LCU.NET.Plugins.LoL;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Utils;
using Notifications.Wpf;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Game
{
    public class ChampSelectDetector
    {
        public LolChampSelectChampSelectPlayerSelection CurrentSelection => Session?.myTeam?.SingleOrDefault(o => o.cellId == Session.localPlayerCellId);

        public event Action<LolChampSelectChampSelectSession> SessionUpdated;

        private LolChampSelectChampSelectSession Session;
        private bool Picked;

        private readonly ILoL LoL;

        public ChampSelectDetector(ILoL lol)
        {
            this.LoL = lol;
        }

        public async Task Init()
        {
            LogTo.Debug("Initializing champ select detector");

            LoL.Socket.Subscribe<LolChampSelectChampSelectSession>(ChampSelect.Endpoint, ChampSelectUpdate);
            LoL.Socket.Subscribe<int>("/lol-champ-select/v1/current-champion", CurrentChampionUpdate);

            await ForceUpdate();
        }

        private void CurrentChampionUpdate(EventType eventType, int data)
        {
            if (eventType != EventType.Delete)
            {
                LogTo.Info("Locked in champion");
                GameState.State.Fire(GameTriggers.LockIn);
            }
        }

        private async void ChampSelectUpdate(EventType eventType, LolChampSelectChampSelectSession data)
        {
            LogTo.Debug("Null data");

            if (data == null)
                return;

            if (eventType == EventType.Update || eventType == EventType.Create)
            {
                LogTo.Debug("Updated session data");
                Session = data;
                
                SessionUpdated?.Invoke(data);

                if (CurrentSelection != null && Config.Default.AutoPickSumms)
                    await PickSumms();

                var myAction = data.actions.SelectMany(o => o).LastOrDefault(o => o.actorCellId == data.localPlayerCellId);

                if (myAction?.completed == false)
                {
                    LogTo.Debug("Incomplete user action found");

                    if (myAction.type == "pick")
                    {
                        LogTo.Debug("User must pick");

                        if (!Picked)
                        {
                            if (Config.Default.AutoPickChampion)
                                await Pick(myAction);

                            Picked = true;
                        }
                    }
                    else if (myAction.type == "ban")
                    {
                        LogTo.Debug("User must ban");

                        if (Config.Default.AutoBanChampion)
                            await Ban(myAction);
                    }
                }
            }
            
            if (eventType == EventType.Create)
            {
                GameState.State.Fire(GameTriggers.EnterChampSelect);
            }
            else if (eventType == EventType.Delete)
            {
                GameState.State.Fire(GameTriggers.ExitChampSelect);

                Picked = false;
            }
        }

        private async Task PickSumms()
        {
            LogTo.Info("Picking summoners");

            var pos = CurrentSelection.assignedPosition.ToPosition();

            LogTo.Info("Position: " + pos);

            var summs = Config.Default.SpellsToPick[pos];

            LogTo.Info($"Config summoners: {string.Join(", ", summs)}");

            if (summs.Any(o => o == 0))
            {
                LogTo.Info("Invalid summoners config for lane, trying for fill");
                summs = Config.Default.SpellsToPick[Position.Fill];
            }

            LogTo.Info($"Config summoners: {string.Join(", ", summs)}");

            if (summs.Any(o => o == 0))
            {
                LogTo.Error("Empty summoner spell in config");
                return;
            }

            try
            {
                await LoL.Client.MakeRequestAsync(ChampSelect.Endpoint + "/my-selection", Method.PATCH, new LolChampSelectChampSelectMySelection
                {
                    spell1Id = summs[0],
                    spell2Id = summs[1]
                }, null, "spell1Id", "spell2Id");

                LogTo.Info("Summoner spells set");
            }
            catch (APIErrorException ex)
            {
                LogTo.ErrorException("Couldn't set summoner spells", ex);
            }

            if (Config.Default.DisablePickSumms)
            {
                LogTo.Debug("Unset auto summoner spell pick");

                Config.Default.AutoPickSumms = false;
                Config.Default.Save();
            }
        }

        private async Task Pick(LolChampSelectChampSelectAction myAction)
        {
            LogTo.Debug("Trying to pick champion");

            Dictionary<Position, int> picks = Config.Default.ChampionsToPick;
            var pickable = await LoL.ChampSelect.GetPickableChampions();
            
            Func<int, bool> isValidChamp = o => pickable.championIds.Contains(o);

            var pos = CurrentSelection.assignedPosition.ToPosition();

            var possiblePicks = new List<int>();
            possiblePicks.Add(picks[pos]);
            possiblePicks.Add(picks[Position.Fill]);
            possiblePicks.AddRange(Enumerable.Range(0, (int)Position.UNSELECTED - 1).Select(o => picks[(Position)o]));
            LogTo.Debug("possiblePicks: {0}", string.Join(", ", picks.Values.ToArray()));

            int preferredPick = GetChampion(isValidChamp, possiblePicks);
            LogTo.Debug("Preferred champ: {0}", preferredPick);

            if (!isValidChamp(preferredPick))
            {
                LogTo.Info("Couldn't pick preferred champion");

                MainWindow.NotificationManager.Show(new NotificationContent
                {
                    Title = "Couldn't pick any champion",
                    Message = "Maybe all of your selected champions were banned",
                    Type = NotificationType.Error
                });
                return;
            }

            LogTo.Debug("Candidate found, picking...");
            myAction.championId = preferredPick;
            myAction.completed = true;

            try
            {
                await LoL.ChampSelect.PatchActionById(myAction, myAction.id);
                LogTo.Debug("Champion picked");
            }
            catch (APIErrorException ex)
            {
                LogTo.DebugException("Couldn't pick champion", ex);
            }

            if (Config.Default.DisablePickChampion)
            {
                LogTo.Debug("Unset auto pick champion");

                Config.Default.AutoPickChampion = false;
                Config.Default.Save();
            }
        }

        private async Task Ban(LolChampSelectChampSelectAction myAction)
        {
            LogTo.Debug("Trying to ban champion");

            Dictionary<Position, int> bans = Config.Default.ChampionsToBan;
            var bannable = await LoL.ChampSelect.GetBannableChampions();

            Func<int, bool> isValidChamp = o => bannable.championIds.Contains(o);

            var pos = CurrentSelection.assignedPosition.ToPosition();

            var possibleBans = new List<int>();
            possibleBans.Add(bans[pos]);
            possibleBans.Add(bans[Position.Fill]);
            possibleBans.AddRange(Enumerable.Range(0, (int)Position.UNSELECTED - 1).Select(o => bans[(Position)o]));
            LogTo.Debug("possibleBans: {0}", string.Join(", ", possibleBans));

            int preferredBan = GetChampion(isValidChamp, possibleBans);
            LogTo.Debug("Preferred ban: {0}", preferredBan);

            if (!isValidChamp(preferredBan))
            {
                LogTo.Debug("Couldn't ban any champion");

                MainWindow.NotificationManager.Show(new NotificationContent
                {
                    Title = "Couldn't ban any champion",
                    Message = "Maybe all of your selected champions were banned",
                    Type = NotificationType.Error
                });
                return;
            }

            LogTo.Debug("Candidate found, banning...");
            myAction.championId = preferredBan;
            myAction.completed = true;

            try
            {
                await LoL.ChampSelect.PatchActionById(myAction, myAction.id);
                LogTo.Debug("Champion banned");
            }
            catch (APIErrorException ex)
            {
                LogTo.DebugException("Couldn't ban champion", ex);
            }

            if (Config.Default.DisableBanChampion)
            {
                LogTo.Debug("Unset auto ban champion");

                Config.Default.AutoBanChampion = false;
                Config.Default.Save();
            }
        }

        private int GetChampion(Func<int, bool> isValid, IEnumerable<int> champs)
        {
            return champs.FirstOrDefault(isValid);
        }

        public async Task ForceUpdate()
        {
            LogTo.Debug("Forcing champ select update");

            var session = await TryGetSession();
            
            var ev = EventType.Update;

            if (Session == null && session.Success)
                ev = EventType.Create;

            ChampSelectUpdate(ev, session.Session);

            if (GameState.State.CurrentState != GameStates.LockedIn)
            {
                int champId;

                try
                {
                    champId = await LoL.ChampSelect.GetCurrentChampion();
                }
                catch (NoActiveDelegateException)
                {
                    return;
                }

                var eventType = (await Riot.GetChampions()).Any(o => o.ID == champId) ? EventType.Update : EventType.Delete;

                CurrentChampionUpdate(eventType, champId);
            }
        }

        private async Task<(bool Success, LolChampSelectChampSelectSession Session)> TryGetSession()
        {
            try
            {
                return (true, await LoL.ChampSelect.GetSessionAsync());
            }
            catch (NoActiveDelegateException)
            {
                return (false, null);
            }
        }
    }
}
