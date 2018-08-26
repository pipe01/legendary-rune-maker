using LCU.NET;
using LCU.NET.API_Models;
using LCU.NET.Plugins.LoL;
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

            var session = await TryGetSession();

            if (session.Success)
                ChampSelectUpdate(EventType.Create, session.Session);
        }

        private static void CurrentChampionUpdate(EventType eventType, int data)
        {
            if (eventType != EventType.Delete)
            {
                GameState.State.Fire(GameTriggers.LockIn);
            }
        }

        private static void ChampSelectUpdate(EventType eventType, LolChampSelectChampSelectSession data)
        {
            if (data == null)
                return;

            if (eventType == EventType.Update || eventType == EventType.Create)
            {
                Session = data;

                bool lockedIn = data.actions.Select(o => o[0]).LastOrDefault(o => o.actorCellId == CurrentSelection?.cellId && o.type == "pick")?.completed ?? false;
                
                SessionUpdated?.Invoke(data);
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

        public static async Task ForceUpdate()
        {
            var session = await TryGetSession();
            
            var ev = EventType.Update;

            if (Session == null && session.Success)
                ev = EventType.Create;

            ChampSelectUpdate(ev, session.Session);
        }

        private static async Task<(bool Success, LolChampSelectChampSelectSession Session)> TryGetSession()
        {
            try
            {
                return (true, await ChampSelect.GetSessionAsync());
            }
            catch (APIErrorException ex) when (ex.Message == "No active delegate")
            {
                return (false, null);
            }
        }
    }
}
