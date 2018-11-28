using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Providers
{
    internal abstract class Provider
    {
        [Flags]
        public enum Options
        {
            RunePages = 1,
            ItemSets = 2,
        }

        protected WebClient Client => new WebClient();

        public abstract string Name { get; }

        public virtual Options ProviderOptions => Options.RunePages | Options.ItemSets;

        private IDictionary<int, Position[]> PossibleRolesCache = new Dictionary<int, Position[]>();
        private IDictionary<(int, Position), RunePage> RunePageCache = new Dictionary<(int, Position), RunePage>();
        private IDictionary<(int, Position), ItemSet> ItemSetCache = new Dictionary<(int, Position), ItemSet>();

        public async Task<Position[]> GetPossibleRoles(int championId)
            => PossibleRolesCache.TryGetValue(championId, out var r) ? r :
               PossibleRolesCache[championId] = await GetPossibleRolesInner(championId);

        public async Task<RunePage> GetRunePage(int championId, Position position)
            => RunePageCache.TryGetValue((championId, position), out var r) ? r :
               RunePageCache[(championId, position)] = FillStatsIfNone(await GetRunePageInner(championId, position));

        public async Task<ItemSet> GetItemSet(int championId, Position position)
            => ItemSetCache.TryGetValue((championId, position), out var r) ? r :
               ItemSetCache[(championId, position)] = await GetItemSetInner(championId, position);


        protected abstract Task<Position[]> GetPossibleRolesInner(int championId);

        protected virtual Task<RunePage> GetRunePageInner(int championId, Position position)
            => throw new NotImplementedException();
        protected virtual Task<ItemSet> GetItemSetInner(int championId, Position position)
            => throw new NotImplementedException();


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
