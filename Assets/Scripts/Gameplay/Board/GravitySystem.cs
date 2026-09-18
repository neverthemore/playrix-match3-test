using System.Collections.Generic;
using Match3.Core.Domain;
using Match3.Gameplay.Tiles;
using UnityEngine;

namespace Match3.Gameplay.Board
{
    public readonly struct FallMovement
    {
        public readonly Tile Tile;
        public readonly Vector2Int From;
        public readonly Vector2Int To;

        public readonly bool IsSpawned;

        public FallMovement(Tile tile, Vector2Int from, Vector2Int to, bool isSpawned)
        {
            Tile = tile;
            From = from;
            To = to;
            IsSpawned = isSpawned;
        }

        public int Distance => Mathf.Abs(From.y - To.y) + Mathf.Abs(From.x - To.x);
    }

    public class GravityResult
    {
        public List<FallMovement> Movements { get; } = new List<FallMovement>();

        public bool HasChanges => Movements.Count > 0;

        public int LongestDistance
        {
            get
            {
                int longest = 0;
                foreach (var move in Movements)
                {
                    longest = Mathf.Max(longest, move.Distance);
                }
                return longest;
            }
        }
    }

    public class GravitySystem
    {
        public GravityResult ApplyGravityAndRefill(Board board, IReadOnlyList<TileColor> colors, System.Random rng)
        {
            var result = new GravityResult();

            for (int x = 0; x < board.Width; x++)
            {
                CollapseColumn(board, x, result);
                RefillColumn(board, x, colors, rng, result);
            }

            return result;
        }

        private void CollapseColumn(Board board, int x, GravityResult result)
        {
            int writeY = 0;

            for (int y = 0; y < board.Height; y++)
            {
                var cell = board.GetCell(x, y);

                if (!cell.IsPassable)
                {
                    writeY = y + 1;
                    continue;
                }

                if (cell.Tile == null)
                {
                    continue;
                }

                if (y != writeY)
                {
                    var tile = cell.Tile;
                    board.GetCell(x, writeY).Tile = tile;
                    cell.Tile = null;

                    result.Movements.Add(new FallMovement(
                        tile, new Vector2Int(x, y), new Vector2Int(x, writeY), isSpawned: false));
                }

                writeY++;
            }
        }

        private void RefillColumn(Board board, int x, IReadOnlyList<TileColor> colors, System.Random rng, GravityResult result)
        {
            int spawnSlot = 0;

            for (int y = 0; y < board.Height; y++)
            {
                var cell = board.GetCell(x, y);

                if (!cell.IsPassable)
                {
                    spawnSlot = 0;
                    continue;
                }

                if (cell.Tile != null) continue;

                var color = colors[rng.Next(colors.Count)];
                var tile = new Tile(board.GenerateNextTileId(), color);
                cell.Tile = tile;

                var from = new Vector2Int(x, board.Height + spawnSlot);
                result.Movements.Add(new FallMovement(tile, from, new Vector2Int(x, y), isSpawned: true));
                spawnSlot++;
            }
        }
    }
}
