using Legendary_Rune_Maker.Data;

namespace Legendary_Rune_Maker.Overlay
{
    public readonly struct Enemy
    {
        public Champion Champion { get; }
        public Champion[] GoodPicks { get; }
        public Champion[] BadPicks { get; }

        public Enemy(Champion champion, Champion[] goodPicks, Champion[] badPicks)
        {
            this.Champion = champion;
            this.GoodPicks = goodPicks;
            this.BadPicks = badPicks;
        }
    }
}
