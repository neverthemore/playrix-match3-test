namespace Match3.Core.Domain
{
    public enum TileColor
    {
        None = 0,
        Red = 1,
        Blue = 2,
        Green = 3,
        Yellow = 4,
        Purple = 5,
        Orange = 6
    }

    public enum BoosterType
    {
        None = 0,

        LineHorizontal = 1,

        LineVertical = 2,

        Bomb = 3,

        RainbowBall = 4,

        Plane = 5
    }

    public static class BoosterTypeExtensions
    {
        public static bool IsLine(this BoosterType type)
        {
            return type == BoosterType.LineHorizontal || type == BoosterType.LineVertical;
        }

        public static bool IsBooster(this BoosterType type)
        {
            return type != BoosterType.None;
        }
    }
}
