using LCU.NET;
using Legendary_Rune_Maker.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Rune_providers
{
    internal class ClientProvider : IRuneProvider
    {
        public string Name => "Client";

        public Task<IEnumerable<Position>> GetPossibleRoles(int championId)
        {
            if (GameState.CanUpload)
                return Task.FromResult((IEnumerable<Position>)new[] { Position.Fill });
            
            return Task.FromResult((IEnumerable<Position>)new Position[0]);
        }

        public Task<RunePage> GetRunePage(int championId, Position position) => RunePage.GetActivePageFromClient();
    }
}
