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
    public static class ChampSelectDetector
    {
        private static LolChampSelectChampSelectSession Session;

        public static LolChampSelectChampSelectPlayerSelection CurrentSelection => Session?.myTeam.Single(o => o.cellId == Session.localPlayerCellId);

        public static event Action<LolChampSelectChampSelectSession> SessionUpdated;

        public static void Init()
        {
            LeagueSocket.Subscribe<LolChampSelectChampSelectSession>(ChampSelect.Endpoint, ChampSelectUpdate);
        }

        private static void ChampSelectUpdate(string eventType, LolChampSelectChampSelectSession data)
        {
            if (eventType == "Update")
            {
                Session = data;
                SessionUpdated?.Invoke(data);
            }
        }

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
