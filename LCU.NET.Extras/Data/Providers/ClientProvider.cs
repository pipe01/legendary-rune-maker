using LCU.NET;
using LCU.NET.Extras;
using Legendary_Rune_Maker.Game;
using Ninject;
using System;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Providers
{
    public class ClientProvider : Provider
    {
        public override string Name => "Client";
        public override Options ProviderOptions => Options.RunePages;

        public override Task<Position[]> GetPossibleRoles(int championId)
        {
            if (GameState.CanUpload)
                return Task.FromResult(new[] { Position.Fill });

            return Task.FromResult(new Position[0]);
        }

        public override Task<RunePage> GetRunePage(int championId, Position position)
            => RunePage.GetActivePageFromClient(LCUApp.Container.Get<ILoL>().Perks);
    }
}
