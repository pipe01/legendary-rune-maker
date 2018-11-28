using Anotar.Log4Net;
using LCU.NET;
using LCU.NET.API_Models;
using LCU.NET.Plugins.LoL;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Game
{
    public class LoginDetector
    {
        private ILoL LoL;

        public LoginDetector(ILoL lol)
        {
            this.LoL = lol;
        }

        public async Task Init()
        {
            LogTo.Debug("Initializing login detector");

            LoL.Socket.Subscribe<LolLoginLoginSession>(Login.Endpoint, SessionEvent);

            try
            {
                await ForceUpdate();
            }
            catch (APIErrorException ex)
            {
                LogTo.DebugException("Failed to force login update", ex);
            }
        }

        public async Task ForceUpdate()
        {
            LogTo.Debug("Forcing login update");
            SessionEvent(EventType.Update, await LoL.Login.GetSessionAsync());
        }

        private void SessionEvent(EventType eventType, LolLoginLoginSession data)
        {
            if (data == null)
            {
                LogTo.Debug("Empty data");
                return;
            }

            if (eventType == EventType.Update && data.state == "SUCCEEDED")
            {
                LogTo.Info("User is logged in");
                GameState.State.Fire(GameTriggers.LogIn);
            }
            else if (eventType == EventType.Delete)
            {
                LogTo.Info("User logged out");
                GameState.State.Fire(GameTriggers.LogOut);
            }
        }
    }
}
