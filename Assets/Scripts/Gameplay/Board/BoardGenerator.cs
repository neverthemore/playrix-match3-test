using System.Collections.Generic;
using Match3.Core.Domain;
using Match3.Gameplay.Blocks;
using Match3.Gameplay.Tiles;
using UnityEngine;

namespace Match3.Gameplay.Board
{
    public class BoardGenerator
    {
        private readonly MatchFinder _matchFinder;

        public BoardGenerator(MatchFinder matchFinder)
        {
            _matchFinder = matchFinder;
        }

        public Board Generate(LevelConfig config, IReadOnlyList<TileColor> colors, System.Random rng)
        {
            var board = new Board(config.Width, config.Height);

            ApplyMap(board, config, rng);
            FillRemainingWithoutMatches(board, colors, rng);
            EnsurePlayable(board, colors, rng);

            return board;
        }

        private void ApplyMap(Board board, LevelConfig config, System.Random rng)
        {
            foreach (var levelCell in config.Cells)
            {
                var cell = board.GetCell(levelCell.x, levelCell.y);
                if (cell == null)
                {
                    Debug.LogWarning($"[BoardGenerator] Level '{config.DisplayName}' references " +
                                     $"out-of-bounds cell ({levelCell.x}, {levelCell.y}) - ignored.");
                    continue;
                }

                switch (levelCell.content)
                {
                    case CellContent.Hole:
                        cell.IsHole = true;
                        cell.Tile = null;
                        break;

                    case CellContent.Block:
                        cell.Block = BlockInstance.BuildChain(levelCell.blockLayers);
                        cell.Tile = null;
                        break;

                    case CellContent.FixedChip:
                        cell.Tile = new Tile(board.GenerateNextTileId(), levelCell.chipColor);
                        break;

                    case CellContent.Booster:
                        var tile = new Tile(board.GenerateNextTileId(), levelCell.chipColor);
                        tile.PromoteTo(levelCell.booster);
                        cell.Tile = tile;
                        break;

                    case CellContent.RandomChip:
                    default:
                        break;
                }
            }
        }

        private void FillRemainingWithoutMatches(Board board, IReadOnlyList<TileColor> colors, System.Random rng)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var cell = board.GetCell(x, y);
                    if (!cell.CanHoldTile || cell.Tile != null) continue;

                    var forbidden = GetForbiddenColors(board, x, y);
                    var color = PickColorAvoiding(colors, forbidden, rng);
                    cell.Tile = new Tile(board.GenerateNextTileId(), color);
                }
            }
        }

        private HashSet<TileColor> GetForbiddenColors(Board board, int x, int y)
        {
            var forbidden = new HashSet<TileColor>();

            var left1 = ColorAt(board, x - 1, y);
            var left2 = ColorAt(board, x - 2, y);
            if (left1 != TileColor.None && left1 == left2)
            {
                forbidden.Add(left1);
            }

            var down1 = ColorAt(board, x, y - 1);
            var down2 = ColorAt(board, x, y - 2);
            if (down1 != TileColor.None && down1 == down2)
            {
                forbidden.Add(down1);
            }

            var diag = ColorAt(board, x - 1, y - 1);
            if (diag != TileColor.None && diag == left1 && diag == down1)
            {
                forbidden.Add(diag);
            }

            return forbidden;
        }

        private TileColor ColorAt(Board board, int x, int y)
        {
            var tile = board.GetTile(new Vector2Int(x, y));
            return tile == null || tile.IsColorless ? TileColor.None : tile.Color;
        }

        private TileColor PickColorAvoiding(IReadOnlyList<TileColor> colors, HashSet<TileColor> forbidden, System.Random rng)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var candidate = colors[rng.Next(colors.Count)];
                if (!forbidden.Contains(candidate))
                {
                    return candidate;
                }
            }

            foreach (var color in colors)
            {
                if (!forbidden.Contains(color))
                {
                    return color;
                }
            }

            return colors[rng.Next(colors.Count)];
        }

        private void EnsurePlayable(Board board, IReadOnlyList<TileColor> colors, System.Random rng)
        {
            const int maxAttempts = 30;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (_matchFinder.HasAnyPossibleMove(board))
                {
                    return;
                }

                foreach (var cell in board.AllCells())
                {
                    if (cell.Tile == null || cell.Tile.IsBooster) continue;
                    cell.Tile = null;
                }
                FillRemainingWithoutMatches(board, colors, rng);
            }

            Debug.LogWarning("[BoardGenerator] Could not produce a board with a legal move - " +
                             "check the level's palette size and map layout.");
        }
    }
}
