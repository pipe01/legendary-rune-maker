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
            SkillOrder = 4
        }

        protected WebClient Client => new WebClient();

        public abstract string Name { get; }
        public virtual bool IsEnabled => true;

        public virtual Options ProviderOptions => Options.RunePages | Options.ItemSets;

        public virtual Task<Position[]> GetPossibleRoles(int championId) => throw new NotImplementedException();
        public virtual Task<RunePage> GetRunePage(int championId, Position position) => throw new NotImplementedException();
        public virtual Task<ItemSet> GetItemSet(int championId, Position position) => throw new NotImplementedException();
        /// <summary>
        /// Format: "[(QEW) ]QWERQWERQWERQWERQW"
        /// </summary>
        public virtual Task<string> GetSkillOrder(int championId, Position position) => throw new NotImplementedException();


        public bool Supports(Options options) => (ProviderOptions & options) == options;

        protected RunePage FillStatsIfNone(RunePage page)
        {
            if (page.RuneIDs.Length == 6)
            {
                var allStats = Riot.GetStatRuneStructureAsync().Result;
                page.RuneIDs = page.RuneIDs.Concat(new[] { allStats[0][0].ID, allStats[1][0].ID, allStats[2][0].ID }).ToArray();
            }

            return page;
        }
    }
}
