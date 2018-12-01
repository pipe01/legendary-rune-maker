using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anotar.Log4Net;
using LCU.NET;
using LCU.NET.API_Models;
using LCU.NET.Plugins.LoL;
using Legendary_Rune_Maker.Utils;

namespace Legendary_Rune_Maker.Game
{
    public class LoginDetector : Detector
    {
        public LoginDetector(ILoL lol) : base(lol)
        {
        }

        protected override async Task Init()
        {
            await LoL.Socket.SubscribeAndUpdate<LolLoginLoginSession>(Login.Endpoint, LoginChanged);
        }

        private void LoginChanged(EventType eventType, LolLoginLoginSession data)
        {
            if (data?.state == "SUCCEEDED")
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
