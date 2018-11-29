using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CacheKey = System.ValueTuple<int, Legendary_Rune_Maker.Data.Position>;

namespace Legendary_Rune_Maker.Data.Providers
{
    internal abstract class Provider
    {
        [Flags]
        public enum Options
        {
            RunePages = 1,
            ItemSets = 2,
            SkillOrder = 4
        }

        protected WebClient Client => new WebClient();

        public abstract string Name { get; }

        public virtual Options ProviderOptions => Options.RunePages | Options.ItemSets;

        private IDictionary<int, Position[]> PossibleRolesCache = new Dictionary<int, Position[]>();
        private IDictionary<CacheKey, RunePage> RunePageCache = new Dictionary<CacheKey, RunePage>();
        private IDictionary<CacheKey, ItemSet> ItemSetCache = new Dictionary<CacheKey, ItemSet>();
        private IDictionary<CacheKey, string> SkillOrderCache = new Dictionary<CacheKey, string>();

        public Task<Position[]> GetPossibleRoles(int championId)
            => Cache(PossibleRolesCache, championId, () => GetPossibleRolesInner(championId));

        public Task<RunePage> GetRunePage(int championId, Position position)
            => Cache(RunePageCache, (championId, position),
                async () => FillStatsIfNone(await GetRunePageInner(championId, position)));

        public Task<ItemSet> GetItemSet(int championId, Position position)
            => Cache(ItemSetCache, (championId, position), () => GetItemSetInner(championId, position));

        public Task<string> GetSkillOrder(int championId, Position position)
            => Cache(SkillOrderCache, (championId, position), () => GetSkillOrderInner(championId, position));

        protected abstract Task<Position[]> GetPossibleRolesInner(int championId);

        protected virtual Task<RunePage> GetRunePageInner(int championId, Position position)
            => throw new NotImplementedException();
        protected virtual Task<ItemSet> GetItemSetInner(int championId, Position position)
            => throw new NotImplementedException();
        protected virtual Task<string> GetSkillOrderInner(int championId, Position position)
             => throw new NotImplementedException();


        private async Task<TValue> Cache<TValue, TKey>(IDictionary<TKey, TValue> cacheDic,
                                                        TKey key, Func<Task<TValue>> getter)
            => cacheDic.TryGetValue(key, out var val) ? val :
               cacheDic[key] = await getter();

        private RunePage FillStatsIfNone(RunePage page)
        {
            if (page.RuneIDs.Length == 6)
            {
                var allStats = Riot.GetStatRunes();
                page.RuneIDs = page.RuneIDs.Concat(new[] { allStats[0,0].ID, allStats[1,0].ID, allStats[2,0].ID }).ToArray();
            }

            return page;
        }
    }
}
