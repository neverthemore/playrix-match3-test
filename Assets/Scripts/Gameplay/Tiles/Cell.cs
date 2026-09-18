using Match3.Gameplay.Blocks;

namespace Match3.Gameplay.Tiles
{
    public class Cell
    {
        public int X { get; }
        public int Y { get; }

        public Tile Tile { get; set; }

        public BlockInstance Block { get; set; }

        public bool IsHole { get; set; }

        public Cell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool CanHoldTile => !IsHole && Block == null;

        public bool IsPassable => !IsHole && Block == null;

        public bool HasBlock => Block != null;
        public bool IsEmpty => Tile == null;
    }
}
