using LCU.NET;
using Legendary_Rune_Maker.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Rune_providers
{
    internal class ClientProvider : Provider
    {
        public override string Name => "Client";
        public override Options ProviderOptions => Options.RunePages;

        protected override Task<ItemSet> GetItemSetInner(int championId, Position position)
        {
            throw new NotImplementedException();
        }

        protected override Task<Position[]> GetPossibleRolesInner(int championId)
        {
            if (GameState.CanUpload)
                return Task.FromResult(new[] { Position.Fill });
            
            return Task.FromResult(new Position[0]);
        }

        protected override Task<RunePage> GetRunePageInner(int championId, Position position) => RunePage.GetActivePageFromClient();
    }
}
