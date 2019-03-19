using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anotar.Log4Net;
using LCU.NET;
using LCU.NET.Plugins.LoL;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Utils;
using Notifications.Wpf;

namespace Legendary_Rune_Maker.Game
{
    public class ReadyCheckDetector : Detector
    {
        private bool IsAccepting;

        private readonly Container<Config> Config;

        public ReadyCheckDetector(ILoL lol, Container<Config> config) : base(lol)
        {
            this.Config = config;
        }

        protected override Task Init()
        {
            LoL.Socket.Subscribe<LolMatchmakingMatchmakingReadyCheckResource>(Matchmaking.ReadyCheckEndpoint, ReadyCheckChanged);

            return Task.CompletedTask;
        }

        private async Task<bool> HasUserActed()
        {
            LolMatchmakingMatchmakingReadyCheckResource data;

            try
            {
                data = await LoL.Matchmaking.GetReadyCheck();
            }
            catch
            {
                return true;
            }

            return data.state != "InProgress" || data.playerResponse != "None";
        }

        private async void ReadyCheckChanged(EventType eventType, LolMatchmakingMatchmakingReadyCheckResource data)
        {
            if (eventType == EventType.Update && !await HasUserActed() && Config.Value.AutoAccept && !IsAccepting)
            {
                IsAccepting = true;

                LogTo.Info("Accepting matchmaking...");
                Notify("Accepting match", null, NotificationType.Success);

#pragma warning disable CS4014
                Task.Run(async () =>
                {
                    await Task.Delay(Config.Value.DelayBeforeAcceptReady);

                    if (await HasUserActed())
                        goto exit;

                    try
                    {
                        await LoL.Matchmaking.PostReadyCheckAccept();
                    }
                    catch (Exception ex)
                    {
                        LogTo.Error("Failed to accept matchmaking: " + ex);
                    }

                    LogTo.Info("Accepted matchmaking");

                exit:
                    LogTo.Debug($"Ready state: {data.state}, player response: {data.playerResponse}");

                    IsAccepting = false;
                });
#pragma warning restore CS4014
            }
        }
    }
}
