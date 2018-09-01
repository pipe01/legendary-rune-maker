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
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Game
{
    internal static class ChampSelectDetector
    {
        private static LolChampSelectChampSelectSession Session;

        private static bool Picked;

        public static LolChampSelectChampSelectPlayerSelection CurrentSelection => Session?.myTeam?.SingleOrDefault(o => o.cellId == Session.localPlayerCellId);

        public static event Action<LolChampSelectChampSelectSession> SessionUpdated;

        public static async Task Init()
        {
            LeagueSocket.Subscribe<LolChampSelectChampSelectSession>(ChampSelect.Endpoint, ChampSelectUpdate);
            LeagueSocket.Subscribe<int>("/lol-champ-select/v1/current-champion", CurrentChampionUpdate);

            await ForceUpdate();
        }

        private static void CurrentChampionUpdate(EventType eventType, int data)
        {
            if (eventType != EventType.Delete)
            {
                GameState.State.Fire(GameTriggers.LockIn);
            }
        }

        private static async void ChampSelectUpdate(EventType eventType, LolChampSelectChampSelectSession data)
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

        private static async Task PickSumms()
        {
            var pos = CurrentSelection.assignedPosition.ToPosition();

            var summs = Config.Default.SpellsToPick[pos];

            if (summs.Any(o => o == 0))
                summs = Config.Default.SpellsToPick[Position.Fill];

            if (summs.Any(o => o == 0))
                return;

            try
            {
                await LeagueClient.MakeRequestAsync(ChampSelect.Endpoint + "/my-selection", Method.PATCH, new
                {
                    spell1Id = summs[0],
                    spell2Id = summs[1]
                });
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

        private static async Task Pick(LolChampSelectChampSelectAction myAction)
        {
            Dictionary<Position, int> picks = Config.Default.ChampionsToPick;
            var pickable = await ChampSelect.GetPickableChampions();

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
                await ChampSelect.PatchActionById(myAction, myAction.id);
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

        private static async Task Ban(LolChampSelectChampSelectAction myAction)
        {
            Dictionary<Position, int> bans = Config.Default.ChampionsToBan;
            var bannable = await ChampSelect.GetBannableChampions();

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
                await ChampSelect.PatchActionById(myAction, myAction.id);
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

        private static int GetChampion(Func<int, bool> isValid, IEnumerable<int> champs)
        {
            return champs.FirstOrDefault(isValid);
        }

        public static async Task ForceUpdate()
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
                    champId = await ChampSelect.GetCurrentChampion();
                }
                catch (NoActiveDelegateException)
                {
                    return;
                }

                var eventType = (await Riot.GetChampions()).Any(o => o.ID == champId) ? EventType.Update : EventType.Delete;

                CurrentChampionUpdate(eventType, champId);
            }
        }

        private static async Task<(bool Success, LolChampSelectChampSelectSession Session)> TryGetSession()
        {
            try
            {
                return (true, await ChampSelect.GetSessionAsync());
            }
            catch (NoActiveDelegateException)
            {
                return (false, null);
            }
        }
    }
}
