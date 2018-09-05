using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data
{
    public class ItemSet
    {
        public class SetBlock
        {
            public string Name { get; set; }
            public int[] Items { get; set; }
        }

        public int Champion { get; set; }
        public Position Position { get; set; }

        public SetBlock[] Blocks { get; set; }
    }
}
