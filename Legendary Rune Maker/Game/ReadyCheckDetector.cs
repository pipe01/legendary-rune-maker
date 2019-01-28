using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anotar.Log4Net;
using LCU.NET;
using LCU.NET.Plugins.LoL;
using Legendary_Rune_Maker.Data;
using Notifications.Wpf;

namespace Legendary_Rune_Maker.Game
{
    public class ReadyCheckDetector : Detector
    {
        private bool IsAccepting;

        private readonly Config Config;

        public ReadyCheckDetector(ILoL lol, Config config) : base(lol)
        {
            this.Config = config;
        }

        protected override Task Init()
        {
            LoL.Socket.Subscribe<LolMatchmakingMatchmakingReadyCheckResource>(Matchmaking.ReadyCheckEndpoint, ReadyCheckChanged);

            return Task.CompletedTask;
        }

        private async void ReadyCheckChanged(EventType eventType, LolMatchmakingMatchmakingReadyCheckResource data)
        {
            Func<bool> hasUserActed = () => data.state != "InProgress" || data.playerResponse != "None";

            if (eventType == EventType.Update && !hasUserActed() && Config.AutoAccept && !IsAccepting)
            {
                IsAccepting = true;

                LogTo.Info("Accepting matchmaking...");
                Notify("Accepting match", null, NotificationType.Success);

                await Task.Delay(Config.DelayBeforeAcceptReady);

                if (hasUserActed())
                    return;

                try
                {
                    await LoL.Matchmaking.PostReadyCheckAccept();
                }
                finally
                {
                    IsAccepting = false;
                }

                LogTo.Info("Accepted matchmaking");
            }
        }
    }
}
