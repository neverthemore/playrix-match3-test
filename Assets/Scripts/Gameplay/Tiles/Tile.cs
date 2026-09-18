using Match3.Core.Domain;

namespace Match3.Gameplay.Tiles
{
    public class Tile
    {
        public int Id { get; }

        public TileColor Color { get; private set; }

        public BoosterType Booster { get; private set; }

        public Tile(int id, TileColor color, BoosterType booster = BoosterType.None)
        {
            Id = id;
            Color = color;
            Booster = booster;
        }

        public bool IsBooster => Booster != BoosterType.None;

        public bool IsColorless => Booster == BoosterType.RainbowBall || Color == TileColor.None;

        public bool MatchesColor(Tile other)
        {
            if (other == null) return false;
            if (IsColorless || other.IsColorless) return false;
            return Color == other.Color;
        }

        public void PromoteTo(BoosterType booster)
        {
            Booster = booster;
            if (booster == BoosterType.RainbowBall)
            {
                Color = TileColor.None;
            }
        }

        public Tile Clone() => new Tile(Id, Color, Booster);
    }
}
