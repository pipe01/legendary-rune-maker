using LCU.NET;
using LCU.NET.Plugins.LoL;
using Notifications.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Game
{
    internal static class ReadyCheckDetector
    {
        public static void Init()
        {
            LeagueSocket.Subscribe<LolMatchmakingMatchmakingReadyCheckResource>(Matchmaking.ReadyCheckEndpoint, ReadyCheckChanged);
        }

        private static async void ReadyCheckChanged(EventType eventType, LolMatchmakingMatchmakingReadyCheckResource data)
        {
            if (eventType != EventType.Delete && data.state == "InProgress" && data.playerResponse == "None" && Properties.Settings.Default.AutoAccept)
            {
                await Matchmaking.PostReadyCheckAccept();

                MainWindow.NotificationManager.Show(new NotificationContent
                {
                    Title = "Accepted match",
                    Type = NotificationType.Success
                });
            }
        }
    }
}
