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
            LoL.Socket.Subscribe<LolLoginLoginSession>(Login.Endpoint, SessionEvent);

            try
            {
                await ForceUpdate();
            }
            catch (APIErrorException)
            {
            }
        }

        public async Task ForceUpdate() => SessionEvent(EventType.Update, await LoL.Login.GetSessionAsync());

        private void SessionEvent(EventType eventType, LolLoginLoginSession data)
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
