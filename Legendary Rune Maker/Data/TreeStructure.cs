using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data
{
    public class TreeStructure
    {
        public int ID { get; }
        public int[][] PerkSlots { get; }
        public int[][] StatSlots { get; }

        public TreeStructure(int id, int[][] perkSlots, int[][] statSlots)
        {
            this.ID = id;
            this.PerkSlots = perkSlots;
            this.StatSlots = statSlots;
        }
    }
}
