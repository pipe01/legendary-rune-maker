using LCU.NET;
using LCU.NET.Plugins.LoL;
using Legendary_Rune_Maker.Data;
using Notifications.Wpf;

namespace Legendary_Rune_Maker.Game
{
    public class ReadyCheckDetector
    {
        private readonly ILoL LoL;

        public ReadyCheckDetector(ILoL lol)
        {
            this.LoL = lol;
        }

        public void Init()
        {
            LoL.Socket.Subscribe<LolMatchmakingMatchmakingReadyCheckResource>(Matchmaking.ReadyCheckEndpoint, ReadyCheckChanged);
        }

        private async void ReadyCheckChanged(EventType eventType, LolMatchmakingMatchmakingReadyCheckResource data)
        {
            if (eventType == EventType.Update && data.state == "InProgress" && data.playerResponse == "None" && Config.Default.AutoAccept)
            {
                await LoL.Matchmaking.PostReadyCheckAccept();

                MainWindow.NotificationManager.Show(new NotificationContent
                {
                    Title = "Accepted match",
                    Type = NotificationType.Success
                });
            }
        }
    }
}
