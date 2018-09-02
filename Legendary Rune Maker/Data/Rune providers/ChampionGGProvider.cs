using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Rune_providers
{
    internal class ChampionGGProvider : RuneProvider
    {
        public override string Name => "Champion.GG";

        protected override Task<Position[]> GetPossibleRolesInner(int championId)
        {
            throw new NotImplementedException();
        }

        protected override Task<RunePage> GetRunePageInner(int championId, Position position)
        {
            throw new NotImplementedException();
        }
    }
}
