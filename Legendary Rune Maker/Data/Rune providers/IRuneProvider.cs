using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data.Rune_providers
{
    internal interface IRuneProvider
    {
        string Name { get; }

        Task<IEnumerable<Position>> GetPossibleRoles(int championId);
        Task<RunePage> GetRunePage(int championId, Position position);
    }
}
