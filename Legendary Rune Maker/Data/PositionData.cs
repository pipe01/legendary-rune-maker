namespace Legendary_Rune_Maker.Data
{
    public readonly struct PositionData
    {
        public Position Position { get; }
        public float Weight { get; }

        public PositionData(Position position, float weight)
        {
            this.Position = position;
            this.Weight = weight;
        }

        public override string ToString() => $"{Position} ({Weight})";
    }
}
