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
            LoL.Socket.Subscribe<LolChampSelectChampSelectSession>(ChampSelect.Endpoint, ChampSelectUpdate);
            LoL.Socket.Subscribe<int>("/lol-champ-select/v1/current-champion", CurrentChampionUpdate);

            await ForceUpdate();
        }

        private void CurrentChampionUpdate(EventType eventType, int data)
        {
            if (eventType != EventType.Delete)
            {
                GameState.State.Fire(GameTriggers.LockIn);
            }
        }

        private async void ChampSelectUpdate(EventType eventType, LolChampSelectChampSelectSession data)
        {
            if (data == null)
                return;

            if (eventType == EventType.Update || eventType == EventType.Create)
            {
                Session = data;
                
                SessionUpdated?.Invoke(data);
                
                var myAction = data.actions.SelectMany(o => o).LastOrDefault(o => o.actorCellId == data.localPlayerCellId);

                if (myAction?.completed == false)
                {
                    if (myAction.type == "pick")
                    {
                        if (!Picked)
                        {
                            if (Config.Default.AutoPickSumms)
                                await PickSumms();

                            if (Config.Default.AutoPickChampion)
                                await Pick(myAction);

                            Picked = true;
                        }
                    }
                    else if (myAction.type == "ban")
                    {
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
            var pos = CurrentSelection.assignedPosition.ToPosition();

            var summs = Config.Default.SpellsToPick[pos];

            if (summs.Any(o => o == 0))
                summs = Config.Default.SpellsToPick[Position.Fill];

            if (summs.Any(o => o == 0))
                return;

            try
            {
                await LoL.Client.MakeRequestAsync(ChampSelect.Endpoint + "/my-selection", Method.PATCH, new LolChampSelectChampSelectMySelection
                {
                    spell1Id = summs[0],
                    spell2Id = summs[1]
                }, null, "spell1Id", "spell2Id");
            }
            catch (APIErrorException)
            {
            }

            if (Config.Default.DisablePickSumms)
            {
                Config.Default.AutoPickSumms = false;
                Config.Default.Save();
            }
        }

        private async Task Pick(LolChampSelectChampSelectAction myAction)
        {
            Dictionary<Position, int> picks = Config.Default.ChampionsToPick;
            var pickable = await LoL.ChampSelect.GetPickableChampions();

            Func<int, bool> isValidChamp = o => pickable.championIds.Contains(o);

            var pos = CurrentSelection.assignedPosition.ToPosition();

            var possiblePicks = new List<int>();
            possiblePicks.Add(picks[pos]);
            possiblePicks.Add(picks[Position.Fill]);
            possiblePicks.AddRange(Enumerable.Range(0, (int)Position.UNSELECTED - 1).Select(o => picks[(Position)o]));

            int preferredPick = GetChampion(isValidChamp, possiblePicks);
            
            if (!isValidChamp(preferredPick))
            {
                MainWindow.NotificationManager.Show(new NotificationContent
                {
                    Title = "Couldn't pick any champion",
                    Message = "Maybe all of your selected champions were banned",
                    Type = NotificationType.Error
                });
                return;
            }

            myAction.championId = preferredPick;
            myAction.completed = true;

            try
            {
                await LoL.ChampSelect.PatchActionById(myAction, myAction.id);
            }
            catch (APIErrorException)
            {
            }

            if (Config.Default.DisablePickChampion)
            {
                Config.Default.AutoPickChampion = false;
                Config.Default.Save();
            }
        }

        private async Task Ban(LolChampSelectChampSelectAction myAction)
        {
            Dictionary<Position, int> bans = Config.Default.ChampionsToBan;
            var bannable = await LoL.ChampSelect.GetBannableChampions();

            Func<int, bool> isValidChamp = o => bannable.championIds.Contains(o);

            var pos = CurrentSelection.assignedPosition.ToPosition();

            var possibleBans = new List<int>();
            possibleBans.Add(bans[pos]);
            possibleBans.Add(bans[Position.Fill]);
            possibleBans.AddRange(Enumerable.Range(0, (int)Position.UNSELECTED - 1).Select(o => bans[(Position)o]));

            int preferredBan = GetChampion(isValidChamp, possibleBans);

            if (!isValidChamp(preferredBan))
            {
                MainWindow.NotificationManager.Show(new NotificationContent
                {
                    Title = "Couldn't ban any champion",
                    Message = "Maybe all of your selected champions were banned",
                    Type = NotificationType.Error
                });
                return;
            }

            myAction.championId = preferredBan;
            myAction.completed = true;

            try
            {
                await LoL.ChampSelect.PatchActionById(myAction, myAction.id);
            }
            catch (APIErrorException)
            {
            }

            if (Config.Default.DisableBanChampion)
            {
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
