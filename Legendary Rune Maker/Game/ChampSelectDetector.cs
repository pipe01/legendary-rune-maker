using Anotar.Log4Net;
using LCU.NET;
using LCU.NET.API_Models;
using LCU.NET.Plugins.LoL;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Data.Providers;
using Legendary_Rune_Maker.Locale;
using Legendary_Rune_Maker.Utils;
using Notifications.Wpf;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Game
{
    public class ChampSelectDetector
    {
        internal class CSSession
        {
            public readonly LolChampSelectChampSelectSession Data;
            public readonly LolChampSelectChampSelectPlayerSelection PlayerSelection;

            public bool HasPickedChampion,
                        HasPickedSumms,
                        HasBanned,
                        HasTriedSkillOrder,
                        HasTriedRunePage,
                        HasTriedItemSet;

            public CSSession(CSSession last, LolChampSelectChampSelectSession data)
            {
                this.Data = data;
                this.PlayerSelection = data.myTeam?.SingleOrDefault(o => o.cellId == data.localPlayerCellId);

                if (last != null)
                {
                    this.HasPickedChampion = last.HasPickedChampion;
                    this.HasPickedSumms = last.HasPickedSumms;
                    this.HasBanned = last.HasBanned;
                    this.HasTriedSkillOrder = last.HasTriedSkillOrder;
                    this.HasTriedRunePage = last.HasTriedRunePage;
                    this.HasTriedItemSet = last.HasTriedItemSet;
                }
            }
        }
        
        public event Action<LolChampSelectChampSelectSession> SessionUpdated;

        private static readonly int[] SkillOrderBlockContent = new[] { 3044, 3508, 1058, 3031, 3134 };

        internal CSSession Session;

        private readonly ILoL LoL;

        public Actuator Actuator { get; set; }
        private IMainWindow Main => Actuator?.Main;

        public ChampSelectDetector(ILoL lol)
        {
            this.LoL = lol;
        }

        public async Task Init()
        {
            LogTo.Debug("Initializing champ select detector");

            LoL.Socket.Subscribe<LolChampSelectChampSelectSession>(ChampSelect.Endpoint, ChampSelectUpdate);
            LoL.Socket.Subscribe<int>("/lol-champ-select/v1/current-champion", CurrentChampionUpdate);

            await ForceUpdate();
        }

        private void CurrentChampionUpdate(EventType eventType, int data)
        {
            if (eventType != EventType.Delete)
            {
                LogTo.Info("Locked in champion");
                GameState.State.Fire(GameTriggers.LockIn);

                Task.Factory.StartNew(async () =>
                {
                    if (!Session.HasTriedRunePage)
                    {
                        Session.HasTriedRunePage = true;

                        if (Config.Default.UploadOnLock)
                            await UploadPage();
                    }

                    if (!Session.HasTriedItemSet)
                    {
                        Session.HasTriedItemSet = true;

                        if (Config.Default.SetItemSet)
                            await UploadItemSet();
                    }

                    if (!Session.HasTriedSkillOrder)
                    {
                        Session.HasTriedSkillOrder = true;

                        if (Config.Default.ShowSkillOrder && !Config.Default.SetItemSet)
                            await UploadSkillOrder();
                    }
                });
            }
        }

        private async void ChampSelectUpdate(EventType eventType, LolChampSelectChampSelectSession data)
        {
            LogTo.Debug("Null data");

            if (data == null)
                return;

            if (eventType == EventType.Update || eventType == EventType.Create)
            {
                LogTo.Debug("Updated session data");
                Session = new CSSession(Session, data);
                
                SessionUpdated?.Invoke(data);

                if (Session.PlayerSelection != null && Config.Default.AutoPickSumms)
                    await PickSumms();

                var myAction = data.actions.SelectMany(o => o).LastOrDefault(o => o.actorCellId == data.localPlayerCellId);

                if (myAction?.completed == false)
                {
                    LogTo.Debug("Incomplete user action found");

                    if (myAction.type == "pick" && !Session.HasPickedChampion)
                    {
                        LogTo.Debug("User must pick");
                        
                        if (Config.Default.AutoPickChampion)
                            await Pick(myAction);

                        Session.HasPickedChampion = true;
                    }

                    if (myAction.type == "ban" && !Session.HasBanned)
                    {
                        LogTo.Debug("User must ban");

                        if (Config.Default.AutoBanChampion)
                            await Ban(myAction);

                        Session.HasBanned = true;
                    }
                }
            }
            
            if (eventType == EventType.Create)
            {
                Session = null;
                GameState.State.Fire(GameTriggers.EnterChampSelect);
            }
            else if (eventType == EventType.Delete)
            {
                Session = null;
                GameState.State.Fire(GameTriggers.ExitChampSelect);
            }
        }

        private async Task PickSumms()
        {
            LogTo.Info("Picking summoners");

            var pos = Session.PlayerSelection.assignedPosition.ToPosition();

            LogTo.Info("Position: " + pos);

            var summs = Config.Default.SpellsToPick[pos];

            LogTo.Info($"Config summoners: {string.Join(", ", summs)}");

            if (summs.Any(o => o == 0))
            {
                LogTo.Info("Invalid summoners config for lane, trying for fill");
                summs = Config.Default.SpellsToPick[Position.Fill];
                LogTo.Info($"New summoners: {string.Join(", ", summs)}");
            }
            
            if (summs.Any(o => o == 0))
            {
                LogTo.Error("Empty summoner spell in config");
                return;
            }

            try
            {
                await LoL.Client.MakeRequestAsync(ChampSelect.Endpoint + "/my-selection", Method.PATCH, new LolChampSelectChampSelectMySelection
                {
                    spell1Id = summs[0],
                    spell2Id = summs[1]
                }, null, "spell1Id", "spell2Id");

                LogTo.Info("Summoner spells set");
            }
            catch (APIErrorException ex)
            {
                LogTo.ErrorException("Couldn't set summoner spells", ex);
            }

            if (Config.Default.DisablePickSumms)
            {
                LogTo.Debug("Unset auto summoner spell pick");

                Config.Default.AutoPickSumms = false;
                Config.Default.Save();
            }
        }

        private async Task Pick(LolChampSelectChampSelectAction myAction)
        {
            LogTo.Debug("Trying to pick champion");

            Dictionary<Position, int> picks = Config.Default.ChampionsToPick;
            var pickable = await LoL.ChampSelect.GetPickableChampions();
            
            Func<int, bool> isValidChamp = o => pickable.championIds.Contains(o);

            var pos = Session.PlayerSelection.assignedPosition.ToPosition();

            var possiblePicks = new List<int>();
            possiblePicks.Add(picks[pos]);
            possiblePicks.Add(picks[Position.Fill]);
            possiblePicks.AddRange(Enumerable.Range(0, (int)Position.UNSELECTED - 1).Select(o => picks[(Position)o]));
            LogTo.Debug("possiblePicks: {0}", string.Join(", ", picks.Values.ToArray()));

            int preferredPick = GetChampion(isValidChamp, possiblePicks);
            LogTo.Debug("Preferred champ: {0}", preferredPick);

            if (!isValidChamp(preferredPick))
            {
                LogTo.Info("Couldn't pick preferred champion");

                MainWindow.NotificationManager.Show(new NotificationContent
                {
                    Title = "Couldn't pick any champion",
                    Message = "Maybe all of your selected champions were banned",
                    Type = NotificationType.Error
                });
                return;
            }

            LogTo.Debug("Candidate found, picking...");
            myAction.championId = preferredPick;
            myAction.completed = true;

            try
            {
                await LoL.ChampSelect.PatchActionById(myAction, myAction.id);
                LogTo.Debug("Champion picked");
            }
            catch (APIErrorException ex)
            {
                LogTo.DebugException("Couldn't pick champion", ex);
            }

            if (Config.Default.DisablePickChampion)
            {
                LogTo.Debug("Unset auto pick champion");

                Config.Default.AutoPickChampion = false;
                Config.Default.Save();
            }
        }

        private async Task Ban(LolChampSelectChampSelectAction myAction)
        {
            LogTo.Debug("Trying to ban champion");

            Dictionary<Position, int> bans = Config.Default.ChampionsToBan;
            var bannable = await LoL.ChampSelect.GetBannableChampions();

            Func<int, bool> isValidChamp = o => bannable.championIds.Contains(o);

            var pos = Session.PlayerSelection.assignedPosition.ToPosition();

            var possibleBans = new List<int>();
            possibleBans.Add(bans[pos]);
            possibleBans.Add(bans[Position.Fill]);
            possibleBans.AddRange(Enumerable.Range(0, (int)Position.UNSELECTED - 1).Select(o => bans[(Position)o]));
            LogTo.Debug("possibleBans: {0}", string.Join(", ", possibleBans));

            int preferredBan = GetChampion(isValidChamp, possibleBans);
            LogTo.Debug("Preferred ban: {0}", preferredBan);

            if (!isValidChamp(preferredBan))
            {
                LogTo.Debug("Couldn't ban any champion");

                MainWindow.NotificationManager.Show(new NotificationContent
                {
                    Title = "Couldn't ban any champion",
                    Message = "Maybe all of your selected champions were banned",
                    Type = NotificationType.Error
                });
                return;
            }

            LogTo.Debug("Candidate found, banning...");
            myAction.championId = preferredBan;
            myAction.completed = true;

            try
            {
                await LoL.ChampSelect.PatchActionById(myAction, myAction.id);
                LogTo.Debug("Champion banned");
            }
            catch (APIErrorException ex)
            {
                LogTo.DebugException("Couldn't ban champion", ex);
            }

            if (Config.Default.DisableBanChampion)
            {
                LogTo.Debug("Unset auto ban champion");

                Config.Default.AutoBanChampion = false;
                Config.Default.Save();
            }
        }

        public async Task UploadSkillOrder()
        {
            LogTo.Debug("Trying to upload skill order");

            var provider = Array.Find(Actuator.RuneProviders, o => o.Name == Config.Default.ItemSetProvider)
                            ?? Actuator.RuneProviders.First(o => o.Supports(Provider.Options.SkillOrder));

            if (!provider.Supports(Provider.Options.SkillOrder))
            {
                LogTo.Error("Provider {0} doesn't support skill order", provider.Name);
                return;
            }

            LogTo.Debug("Getting skill order from provider {0}", provider.Name);

            var order = await provider.GetSkillOrder(Main.SelectedChampion, Main.SelectedPosition);
            var set = new ItemSet
            {
                Name = "Skill order" + (order.Contains(' ') ? " " + order.Split(' ')[0] : ""),
                Champion = Main.SelectedChampion,
                Blocks = new[]
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

            var provider = Array.Find(Actuator.RuneProviders, o => o.Name == Config.Default.ItemSetProvider)
                            ?? Actuator.RuneProviders[0];
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

        private int GetChampion(Func<int, bool> isValid, IEnumerable<int> champs)
        {
            return champs.FirstOrDefault(isValid);
        }

        public async Task ForceUpdate()
        {
            LogTo.Debug("Forcing champ select update");

            var session = await TryGetSession();
            
            var ev = EventType.Update;

            if (Session == null && session.Success)
                ev = EventType.Create;

            ChampSelectUpdate(ev, session.Session);

            if (GameState.State.CurrentState != GameStates.LockedIn)
            {
                int champId;

                try
                {
                    champId = await LoL.ChampSelect.GetCurrentChampion();
                }
                catch (NoActiveDelegateException)
                {
                    return;
                }

                var eventType = (await Riot.GetChampions()).Any(o => o.ID == champId) ? EventType.Update : EventType.Delete;

                CurrentChampionUpdate(eventType, champId);
            }
        }

        private async Task<(bool Success, LolChampSelectChampSelectSession Session)> TryGetSession()
        {
            try
            {
                return (true, await LoL.ChampSelect.GetSessionAsync());
            }
            catch (NoActiveDelegateException)
            {
                return (false, null);
            }
        }
    }
}
