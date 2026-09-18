using Match3.Core.Domain;

namespace Match3.Gameplay.Board
{
    public static class BoosterPromotion
    {
        public static BoosterType GetPromotion(MatchInfo match)
        {
            switch (match.Shape)
            {
                case MatchShape.Square2x2:
                    return BoosterType.Plane;

                case MatchShape.Line4:
                    return match.IsHorizontal ? BoosterType.LineHorizontal : BoosterType.LineVertical;

                case MatchShape.LShapeOrT:
                    return BoosterType.Bomb;

                case MatchShape.Line5Plus:
                    return BoosterType.RainbowBall;

                case MatchShape.Line3:
                default:
                    return BoosterType.None;
            }
        }
    }
}
