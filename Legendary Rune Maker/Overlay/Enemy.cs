using Legendary_Rune_Maker.Data;

namespace Legendary_Rune_Maker.Overlay
{
    public readonly struct Enemy
    {
        public Champion Champion { get; }
        public Champion[] GoodPicks { get; }
        public Champion[] BadPicks { get; }
        public string Position { get; }

        public Enemy(Champion champion, Champion[] goodPicks, Champion[] badPicks, string position)
        {
            this.Champion = champion;
            this.GoodPicks = goodPicks;
            this.BadPicks = badPicks;
            this.Position = position;
        }
    }
}
