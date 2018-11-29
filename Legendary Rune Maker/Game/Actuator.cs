using Anotar.Log4Net;
using LCU.NET;
using LCU.NET.API_Models;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Data.Providers;
using Legendary_Rune_Maker.Locale;
using Legendary_Rune_Maker.Utils;
using Notifications.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Game
{
    public class Actuator
    {
        internal static readonly Provider[] RuneProviders = new Provider[]
        {
            new ClientProvider(),
            new RunesLolProvider(),
            new ChampionGGProvider(),
            new MetaLolProvider(),
            new LolFlavorProvider(),
            new UGGProvider(),
            new OpGGProvider()
        };

        private static readonly int[] SkillOrderBlockContent = new[] { 3044, 3508, 1058, 3031, 3134 };

        public IMainWindow Main { get; set; }

        private ILeagueClient LeagueClient => LoL.Client;

        private readonly ILoL LoL;
        private readonly ChampSelectDetector ChampSelectDetector;
        private readonly LoginDetector LoginDetector;
        private readonly ReadyCheckDetector ReadyCheckDetector;

        public Actuator(ILoL lol, ChampSelectDetector champSelectDetector, LoginDetector loginDetector, ReadyCheckDetector readyCheckDetector)
        {
            this.LoL = lol;
            this.ChampSelectDetector = champSelectDetector;
            this.LoginDetector = loginDetector;
            this.ReadyCheckDetector = readyCheckDetector;
        }

        public async Task Init()
        {
            LogTo.Debug("Initializing actuator");

            GameState.State.EnteredState += State_EnteredState;
            ChampSelectDetector.SessionUpdated += ChampSelectDetector_SessionUpdated;
            LeagueClient.ConnectedChanged += LeagueClient_ConnectedChanged;

            if (!LeagueClient.Init())
            {
                LogTo.Info("League client not open, listening");
                LeagueClient.BeginTryInit();
            }
            else
            {
                LogTo.Info("Connected to league client");
            }

            LogTo.Debug("Initializing detectors");

            await LoginDetector.Init();
            await ChampSelectDetector.Init();
            ReadyCheckDetector.Init();

            LogTo.Debug("Initialized detectors");
        }
        
        private void LeagueClient_ConnectedChanged(bool connected)
        {
            LogTo.Debug("Connected: " + connected);

            if (connected)
            {
                GameState.State.Fire(GameTriggers.OpenGame);
            }
            else
            {
                GameState.State.Fire(GameTriggers.CloseGame);
            }
        }

        private async void State_EnteredState(GameStates state)
        {
            Main.SafeInvoke(() => Main.SetState(state));

            if (state == GameStates.Disconnected)
            {
                LogTo.Info("Disconncted from client, trying to reconnect");

                await Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    LeagueClient.BeginTryInit();
                });
            }
            else if (state == GameStates.LockedIn)
            {
                await Main.SafeInvoke(async () =>
                {
                    if (Config.Default.UploadOnLock)
                        await UploadPage();

                    if (Config.Default.SetItemSet)
                        await UploadItemSet();

                    if (Config.Default.ShowSkillOrder && !Config.Default.SetItemSet)
                        await UploadSkillOrder();
                });
            }
        }

        public async Task UploadSkillOrder()
        {
            LogTo.Debug("Trying to upload skill order");

            var provider = Array.Find(RuneProviders, o => o.Name == Config.Default.ItemSetProvider)
                            ?? RuneProviders.First(o => o.Supports(Provider.Options.SkillOrder));

            if (!provider.Supports(Provider.Options.SkillOrder))
            {
                LogTo.Error("Provider {0} doesn't support skill order", provider.Name);
                return;
            }

            var order = await provider.GetSkillOrder(Main.SelectedChampion, Main.SelectedPosition);
            var set = new ItemSet
            {
                Name = "Skill order" + (order.Contains(' ') ? " " + order.Split(' ')[0] : ""),
                Champion = Main.SelectedChampion,
                Blocks = new []
                {
                    new ItemSet.SetBlock
                    {
                        Name = "Skill order: " + order,
                        Items = SkillOrderBlockContent
                    }
                }
            };

            LogTo.Debug("Uploading skill order item set");
            await set.UploadToClient(LoL.Login, LoL.ItemSets);

            LogTo.Debug("Uploaded skill order");
        }

        private async Task UploadItemSet()
        {
            LogTo.Debug("Trying to upload item set");

            var provider = Array.Find(RuneProviders, o => o.Name == Config.Default.ItemSetProvider) ?? RuneProviders[0];
            var set = await provider.GetItemSet(Main.SelectedChampion, Main.SelectedPosition);

            LogTo.Info("Gotten item set from {0}", provider.Name);

            if (Config.Default.ShowSkillOrder)
            {
                if (!provider.Supports(Provider.Options.SkillOrder))
                {
                    LogTo.Error("Tried to upload skill order but selected provider doesn't support it");
                }
                else
                {
                    LogTo.Debug("Appending skill order block to item set");

                    var order = await provider.GetSkillOrder(Main.SelectedChampion, Main.SelectedPosition);
                    var blocks = new List<ItemSet.SetBlock>(set.Blocks);

                    blocks.Insert(0, new ItemSet.SetBlock
                    {
                        Name = "Skill order: " + order,
                        Items = SkillOrderBlockContent
                    });

                    set.Blocks = blocks.ToArray();

                    if (order.Contains(' '))
                        set.Name += " " + order.Split(' ')[0];
                }
            }

            LogTo.Debug("Uploading item set");

            await set.UploadToClient(LoL.Login, LoL.ItemSets);

            LogTo.Debug("Uploaded item set");
            Main.ShowNotification(Text.UploadedItemSet,
                Text.UploadedItemSetFrom.FormatStr(provider.Name), NotificationType.Success);
        }

        private async Task UploadPage()
        {
            LogTo.Debug("Trying to upload rune page");

            string champion = Riot.GetChampion(Main.SelectedChampion).Name;

            LogTo.Debug("for champion {0}", champion);

            Main.ShowNotification(Text.LockedInMessage, champion + ", " + Main.SelectedPosition.ToString().ToLower(), NotificationType.Success);

            if (!Main.ValidPage)
            {
                LogTo.Info("Invalid current rune page");

                if (Config.Default.LoadOnLock)
                {
                    LogTo.Info("Downloading from provider");
                    await Main.LoadPageFromDefaultProvider();
                    LogTo.Debug("Downloaded from provider");
                }
                else
                {
                    Main.ShowNotification(Text.PageChampNotSet.FormatStr(champion), null, NotificationType.Error);
                    return;
                }
            }

            LogTo.Debug("Uploading rune page to client");
            await Task.Run(() => Main.Page.UploadToClient(LoL.Perks));
            LogTo.Debug("Uploaded rune page to client");
        }

        private void ChampSelectDetector_SessionUpdated(LolChampSelectChampSelectSession obj)
        {
            var player = ChampSelectDetector.CurrentSelection;

            Main.SafeInvoke(async () =>
            {
                if (player != null && Main.Attached)
                {
                    var pos = player.assignedPosition.ToPosition();

                    LogTo.Debug("Set position: {0}", pos);
                    Main.SelectedPosition = pos;

                    if (player.championId != 0)
                    {
                        LogTo.Debug("Set champion: {0}", player.championId);
                        await Main.SetChampion(player.championId);
                    }
                }
            });
        }
    }
}
