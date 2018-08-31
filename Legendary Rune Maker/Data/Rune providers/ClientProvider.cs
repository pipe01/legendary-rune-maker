using LCU.NET;
using Legendary_Rune_Maker.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Rune_providers
{
    internal class ClientProvider : RuneProvider
    {
        public override string Name => "Client";

        protected override Task<IEnumerable<Position>> GetPossibleRolesInner(int championId)
        {
            if (GameState.CanUpload)
                return Task.FromResult((IEnumerable<Position>)new[] { Position.Fill });
            
            return Task.FromResult((IEnumerable<Position>)new Position[0]);
        }

        protected override Task<RunePage> GetRunePageInner(int championId, Position position) => RunePage.GetActivePageFromClient();
    }
}
