using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Rune_providers
{
    internal abstract class RuneProvider
    {
        public abstract string Name { get; }

        public Task<IEnumerable<Position>> GetPossibleRoles(int championId) => GetPossibleRolesInner(championId);
        public Task<RunePage> GetRunePage(int championId, Position position) => GetRunePageInner(championId, position);

        protected abstract Task<IEnumerable<Position>> GetPossibleRolesInner(int championId);
        protected abstract Task<RunePage> GetRunePageInner(int championId, Position position);
    }
}
