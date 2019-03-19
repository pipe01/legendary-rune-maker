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
using System.Media;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Game
{
    public partial class Actuator
    {
        private static readonly int[] SkillOrderBlockContent = new[] { 3044, 3508, 1058, 3031, 3134 };

        public async Task PickChampion(Position pos, LolChampSelectChampSelectAction myAction, bool intent)
        {
            LogTo.Debug("Trying to pick champion" + (intent ? " intent" : ""));

            Dictionary<Position, int> picks = Config.ChampionsToPick;
            var pickable = await LoL.ChampSelect.GetPickableChampions();

            //Build list of possible champion candidates
            var possiblePicks = new List<int>();
            possiblePicks.Add(picks[pos]);
            possiblePicks.Add(picks[Position.Fill]);
            LogTo.Debug("possiblePicks: {0}", string.Join(", ", picks.Values.ToArray()));

            //Get the first valid one
            int preferredPick = possiblePicks.FirstOrDefault(pickable.championIds.Contains);
            LogTo.Debug("Preferred champ: {0}", preferredPick);

            if (preferredPick == 0)
            {
                LogTo.Info("Couldn't pick preferred champion");

                //TODO Add translatable string
                if (!intent)
                    Main.ShowNotification("Couldn't pick any champion",
                        "Maybe all of your selected champions were banned", NotificationType.Error);
                return;
            }

            LogTo.Debug("Candidate found, picking...");
            myAction.championId = preferredPick;
            myAction.completed = !intent;

            try
            {
                await LoL.ChampSelect.PatchActionById(myAction, myAction.id);
                LogTo.Debug("Champion picked");
            }
            catch (APIErrorException ex)
            {
                LogTo.DebugException("Couldn't pick champion", ex);
            }

            if (Config.DisablePickChampion && !intent)
            {
                LogTo.Debug("Unset auto pick champion");

                Config.AutoPickChampion = false;
                Config.Save();
            }
        }

        public async Task BanChampion(Position pos, LolChampSelectChampSelectAction myAction, LolChampSelectChampSelectPlayerSelection[] myTeam)
        {
            LogTo.Debug("Trying to ban champion");

            Dictionary<Position, int> bans = Config.Current.ChampionsToBan;
            var bannable = await LoL.ChampSelect.GetBannableChampions();

            var possibleBans = new List<int>();
            possibleBans.Add(bans[pos]);
            possibleBans.Add(bans[Position.Fill]);
            LogTo.Debug("possibleBans: {0}", string.Join(", ", possibleBans));

            int preferredBan = possibleBans.FirstOrDefault(bannable.championIds.Contains);
            var banName = preferredBan > 0 ? Riot.GetChampion(preferredBan).Name : "None";
            LogTo.Debug("Preferred ban: {0}", banName);

            if (preferredBan == 0)
            {
                LogTo.Debug("Couldn't ban any champion");

                //TODO Add translatable string
                Main.ShowNotification("Couldn't ban any champion",
                    "Maybe all of your selected champions were banned", NotificationType.Error);
                return;
            }

            var teamIntents = myTeam.Select(o => o.championPickIntent);

            if (teamIntents.Contains(preferredBan))
            {
                LogTo.Info("Wanted to ban {0}, but someone wants to play it", banName);

                Main.ShowNotification("Hey", $"Couldn't ban {banName} because someone wants to play it", NotificationType.Error);
                SystemSounds.Exclamation.Play();

                return;
            }

            LogTo.Debug("Candidate found ({0}), banning...", banName);
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

            if (Config.DisableBanChampion)
            {
                LogTo.Debug("Unset auto ban champion");

                Config.AutoBanChampion = false;
                Config.Save();
            }
        }

        public async Task PickSummoners(Position pos)
        {
            LogTo.Info("Picking summoners for " + pos);

            var summs = Config.SpellsToPick[pos];

            LogTo.Info($"Config summoners: {string.Join(", ", summs)}");

            if (summs.Any(o => o == 0))
            {
                LogTo.Info("Invalid summoners config for lane, trying for fill");
                summs = Config.SpellsToPick[Position.Fill];
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

            if (Config.DisablePickSumms)
            {
                LogTo.Debug("Unset auto summoner spell pick");

                Config.AutoPickSumms = false;
                Config.Save();
            }
        }

        public async Task UploadItemSet(Position pos, int championId)
        {
            LogTo.Debug("Trying to upload item set");

            var provider = Array.Find(RuneProviders, o => o.Name == Config.ItemSetProvider) ?? RuneProviders[0];
            var set = await provider.GetItemSet(championId, pos);

            if (set == null)
            {
                LogTo.Error("Failed to get item set from {0}", provider.Name);
                return;
            }

            LogTo.Info("Gotten item set from {0}", provider.Name);

            if (Config.ShowSkillOrder)
            {
                if (!provider.Supports(Provider.Options.SkillOrder))
                {
                    LogTo.Error("Tried to upload skill order but selected provider doesn't support it");
                }
                else
                {
                    LogTo.Debug("Appending skill order block to item set");

                    var order = await provider.GetSkillOrder(championId, pos);
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

        public async Task UploadRunePage(Position pos, int championId)
        {
            LogTo.Debug("Trying to upload rune page");

            string champion = Riot.GetChampion(championId).Name;

            LogTo.Debug("for champion {0}", champion);

            Main.ShowNotification(Text.LockedInMessage, champion + ", " + pos.ToString().ToLower(), NotificationType.Success);

            var page = RuneBook.Instance.Get(championId, pos, false);

            if (page == null)
            {
                LogTo.Info("Invalid current rune page");

                if (Config.Current.LoadOnLock)
                {
                    LogTo.Info("Downloading from provider");
                    page = await Main.SafeInvoke(async () => await Main.LoadPageFromDefaultProvider(championId));
                    LogTo.Debug("Downloaded from provider");
                }
                else
                {
                    Main.ShowNotification(Text.PageChampNotSet.FormatStr(champion), null, NotificationType.Error);
                    return;
                }
            }

            LogTo.Debug("Uploading rune page to client");
            await page.UploadToClient(LoL.Perks);
            LogTo.Debug("Uploaded rune page to client");
        }

        public async Task UploadSkillOrder(Position pos, int championId)
        {
            LogTo.Debug("Trying to upload skill order");

            var provider = Array.Find(RuneProviders, o => o.Name == Config.Current.SkillOrderProvider)
                            ?? RuneProviders.First(o => o.Supports(Provider.Options.SkillOrder));

            if (!provider.Supports(Provider.Options.SkillOrder))
            {
                LogTo.Error("Provider {0} doesn't support skill order", provider.Name);
                return;
            }

            LogTo.Debug("Getting skill order from provider {0}", provider.Name);

            var order = await provider.GetSkillOrder(championId, pos);

            LogTo.Info("Gotten skill order from {0}: {1}", provider.Name, order);

            var set = new ItemSet
            {
                Name = "Skill order" + (order.Contains(' ') ? " " + order.Split(' ')[0] : ""),
                Champion = championId,
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
    }
}
