using System.Collections.Generic;
using Match3.Gameplay.Tiles;
using UnityEngine;

namespace Match3.Gameplay.Board
{
    public class Board
    {
        public int Width { get; }
        public int Height { get; }

        private readonly Cell[,] _cells;
        private int _nextTileId;

        public Board(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new Cell[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _cells[x, y] = new Cell(x, y);
                }
            }
        }

        public bool IsInBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public bool IsInBounds(Vector2Int pos) => IsInBounds(pos.x, pos.y);

        public Cell GetCell(int x, int y) => IsInBounds(x, y) ? _cells[x, y] : null;

        public Cell GetCell(Vector2Int pos) => GetCell(pos.x, pos.y);

        public Tile GetTile(Vector2Int pos) => GetCell(pos)?.Tile;

        public void SetTile(Vector2Int pos, Tile tile)
        {
            var cell = GetCell(pos);
            if (cell != null)
            {
                cell.Tile = tile;
            }
        }

        public int GenerateNextTileId() => _nextTileId++;

        public bool AreAdjacent(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return dx + dy == 1;
        }

        public void SwapTiles(Vector2Int a, Vector2Int b)
        {
            var cellA = GetCell(a);
            var cellB = GetCell(b);
            if (cellA == null || cellB == null) return;

            (cellA.Tile, cellB.Tile) = (cellB.Tile, cellA.Tile);
        }

        public IEnumerable<Vector2Int> GetOrthogonalNeighbors(Vector2Int pos)
        {
            var up = new Vector2Int(pos.x, pos.y + 1);
            var down = new Vector2Int(pos.x, pos.y - 1);
            var left = new Vector2Int(pos.x - 1, pos.y);
            var right = new Vector2Int(pos.x + 1, pos.y);

            if (IsInBounds(up)) yield return up;
            if (IsInBounds(down)) yield return down;
            if (IsInBounds(left)) yield return left;
            if (IsInBounds(right)) yield return right;
        }

        public IEnumerable<Cell> AllCells()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    yield return _cells[x, y];
                }
            }
        }

        public Board Clone()
        {
            var clone = new Board(Width, Height);
            foreach (var cell in AllCells())
            {
                var target = clone._cells[cell.X, cell.Y];
                target.Tile = cell.Tile?.Clone();
                target.Block = cell.Block;
                target.IsHole = cell.IsHole;
            }
            clone._nextTileId = _nextTileId;
            return clone;
        }
    }
}
