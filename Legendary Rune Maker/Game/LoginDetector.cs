using LCU.NET;
using LCU.NET.API_Models;
using LCU.NET.Plugins.LoL;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Game
{
    internal static class LoginDetector
    {
        public static async Task Init()
        {
            LeagueSocket.Subscribe<LolLoginLoginSession>(Login.Endpoint, SessionEvent);

            try
            {
                await ForceUpdate();
            }
            catch (APIErrorException)
            {
            }
        }

        public static async Task ForceUpdate() => SessionEvent(EventType.Update, await Login.GetSessionAsync());

        private static void SessionEvent(EventType eventType, LolLoginLoginSession data)
        {
            if (data == null)
                return;

            if (eventType == EventType.Update && data.state == "SUCCEEDED")
            {
                GameState.State.Fire(GameTriggers.LogIn);
            }
            else if (eventType == EventType.Delete)
            {
                GameState.State.Fire(GameTriggers.LogOut);
            }
        }
    }
}
