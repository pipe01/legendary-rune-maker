using System.Diagnostics;

namespace Legendary_Rune_Maker.Data
{
    [DebuggerDisplay("{ID} {Name}")]
    public class Champion
    {
        public int ID { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string ImageURL { get; set; }
    }
}
