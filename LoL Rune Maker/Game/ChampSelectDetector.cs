using LCU.NET;
using LCU.NET.API_Models;
using LCU.NET.Plugins.LoL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoL_Rune_Maker.Game
{
    internal static class ChampSelectDetector
    {
        private static LolChampSelectChampSelectSession Session;

        public static LolChampSelectChampSelectPlayerSelection CurrentSelection => Session?.myTeam.Single(o => o.cellId == Session.localPlayerCellId);

        public static event Action<LolChampSelectChampSelectSession> SessionUpdated;

        public static async Task Init()
        {
            LeagueSocket.Subscribe<LolChampSelectChampSelectSession>(ChampSelect.Endpoint, ChampSelectUpdate);

            try
            {
                ChampSelectUpdate(EventType.Create, await ChampSelect.GetSessionAsync());
            }
            catch (APIErrorException ex) when (ex.Message == "No active delegate")
            {
            }
        }

        private static void ChampSelectUpdate(EventType eventType, LolChampSelectChampSelectSession data)
        {
            if (data == null)
                return;

            if (eventType == EventType.Update)
            {
                Session = data;

                bool lockedIn = data.actions.Select(o => o[0]).LastOrDefault(o => o.actorCellId == CurrentSelection.cellId && o.type == "pick")?.completed ?? false;

                if (lockedIn)
                    GameState.State.Fire(GameTriggers.LockIn);

                SessionUpdated?.Invoke(data);
            }
            else if (eventType == EventType.Create)
            {
                GameState.State.Fire(GameTriggers.EnterChampSelect);
            }
            else if (eventType == EventType.Delete)
            {
                GameState.State.Fire(GameTriggers.ExitChampSelect);
            }
        }

        public static async Task ForceUpdate() => ChampSelectUpdate(EventType.Update, await GetSession());

        public static async Task<LolChampSelectChampSelectSession> GetSession()
        {
            if (Session == null)
            {
                Session = await ChampSelect.GetSessionAsync();
            }

            return Session;
        }
    }
}
