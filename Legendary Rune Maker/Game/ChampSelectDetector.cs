using LCU.NET;
using LCU.NET.API_Models;
using LCU.NET.Plugins.LoL;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Utils;
using Notifications.Wpf;
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

                var myAction = data.actions.LastOrDefault(o => o[0].actorCellId == data.localPlayerCellId)?.First();

                if (myAction?.completed == false)
                {
                    if (myAction.type == "pick")
                    {
                        if (Config.Default.AutoPickChampion)
                            await Pick(myAction);
                    }
                    else if (myAction.type == "ban")
                    {
                        //Ban
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
            }
        }

        private static async Task Pick(LolChampSelectChampSelectAction myAction)
        {
            Dictionary<Position, int> picks = Config.Default.PickChampions;
            var pickable = await ChampSelect.GetPickableChampions();

            Func<int, bool> isValidChamp = o => pickable.championIds.Contains(o);

            var pos = CurrentSelection.assignedPosition.ToPosition();

            int preferredPick = picks[pos]; //Try to pick the wanted champion for the lane

            if (!isValidChamp(preferredPick)) //If there is no champ for that lane, get the champ for the "Fill" lane
            {
                preferredPick = picks[Position.Fill];
            }

            if (!isValidChamp(preferredPick)) //If there is no champ for fill, pick any lane's champion
            {
                for (int i = 0; i < (int)Position.UNSELECTED && !isValidChamp(preferredPick); i++)
                {
                    preferredPick = picks[(Position)i];
                }
            }

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
