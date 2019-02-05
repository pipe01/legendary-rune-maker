namespace Legendary_Rune_Maker.Data
{
    public class TreeStructure
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string IconURL { get; set; }

        public Rune[][] PerkSlots { get; set; }
        public Rune[][] StatSlots { get; set; }
    }
}
