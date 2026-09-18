using System.Collections.Generic;
using Match3.Core.Domain;
using UnityEngine;

namespace Match3.Gameplay.Board
{
    public class MatchFinder
    {
        private const int MinMatchLength = 3;

        private readonly struct Run
        {
            public readonly List<Vector2Int> Positions;
            public readonly bool IsHorizontal;
            public readonly TileColor Color;

            public Run(List<Vector2Int> positions, bool isHorizontal, TileColor color)
            {
                Positions = positions;
                IsHorizontal = isHorizontal;
                Color = color;
            }
        }

        public List<MatchInfo> FindAllMatches(Board board)
        {
            var runs = new List<Run>();
            CollectRuns(board, horizontal: true, runs);
            CollectRuns(board, horizontal: false, runs);

            var matches = MergeRuns(runs);

            var claimed = new HashSet<Vector2Int>();
            foreach (var match in matches)
            {
                claimed.UnionWith(match.Positions);
            }
            CollectSquares(board, claimed, matches);

            return matches;
        }

        public bool WouldSwapCreateMatch(Board board, Vector2Int a, Vector2Int b)
        {
            var simulated = board.Clone();
            simulated.SwapTiles(a, b);
            return FindAllMatches(simulated).Count > 0;
        }

        public bool HasAnyPossibleMove(Board board)
        {
            foreach (var cell in board.AllCells())
            {
                if (cell.Tile == null) continue;
                var pos = new Vector2Int(cell.X, cell.Y);

                foreach (var neighbor in board.GetOrthogonalNeighbors(pos))
                {
                    if (neighbor.x < pos.x || neighbor.y < pos.y) continue;

                    var neighborCell = board.GetCell(neighbor);
                    if (neighborCell?.Tile == null) continue;

                    if (cell.Tile.IsBooster || neighborCell.Tile.IsBooster)
                    {
                        return true;
                    }

                    if (WouldSwapCreateMatch(board, pos, neighbor))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public (Vector2Int a, Vector2Int b)? FindFirstMove(Board board)
        {
            foreach (var cell in board.AllCells())
            {
                if (cell.Tile == null) continue;
                var pos = new Vector2Int(cell.X, cell.Y);

                foreach (var neighbor in board.GetOrthogonalNeighbors(pos))
                {
                    if (neighbor.x < pos.x || neighbor.y < pos.y) continue;

                    var neighborCell = board.GetCell(neighbor);
                    if (neighborCell?.Tile == null) continue;

                    if (cell.Tile.IsBooster || neighborCell.Tile.IsBooster ||
                        WouldSwapCreateMatch(board, pos, neighbor))
                    {
                        return (pos, neighbor);
                    }
                }
            }
            return null;
        }


        private void CollectRuns(Board board, bool horizontal, List<Run> runs)
        {
            int outerCount = horizontal ? board.Height : board.Width;
            int innerCount = horizontal ? board.Width : board.Height;

            for (int outer = 0; outer < outerCount; outer++)
            {
                var current = new List<Vector2Int>();
                var currentColor = TileColor.None;

                for (int inner = 0; inner < innerCount; inner++)
                {
                    var pos = horizontal ? new Vector2Int(inner, outer) : new Vector2Int(outer, inner);
                    var tile = board.GetTile(pos);

                    bool usable = tile != null && !tile.IsColorless;

                    if (usable && tile.Color == currentColor)
                    {
                        current.Add(pos);
                        continue;
                    }

                    FlushRun(runs, current, currentColor, horizontal);

                    current = new List<Vector2Int>();
                    currentColor = usable ? tile.Color : TileColor.None;
                    if (usable)
                    {
                        current.Add(pos);
                    }
                }

                FlushRun(runs, current, currentColor, horizontal);
            }
        }

        private void FlushRun(List<Run> runs, List<Vector2Int> current, TileColor color, bool horizontal)
        {
            if (current.Count >= MinMatchLength && color != TileColor.None)
            {
                runs.Add(new Run(current, horizontal, color));
            }
        }

        private List<MatchInfo> MergeRuns(List<Run> runs)
        {
            var groupCells = new List<HashSet<Vector2Int>>();
            var groupHasHorizontal = new List<bool>();
            var groupHasVertical = new List<bool>();
            var groupLongestLine = new List<int>();
            var groupColor = new List<TileColor>();

            foreach (var run in runs)
            {
                int target = -1;
                for (int i = groupCells.Count - 1; i >= 0; i--)
                {
                    bool overlaps = false;
                    foreach (var pos in run.Positions)
                    {
                        if (groupCells[i].Contains(pos))
                        {
                            overlaps = true;
                            break;
                        }
                    }
                    if (!overlaps) continue;

                    if (target == -1)
                    {
                        target = i;
                        continue;
                    }

                    groupCells[i].UnionWith(groupCells[target]);
                    groupHasHorizontal[i] |= groupHasHorizontal[target];
                    groupHasVertical[i] |= groupHasVertical[target];
                    groupLongestLine[i] = Mathf.Max(groupLongestLine[i], groupLongestLine[target]);

                    groupCells.RemoveAt(target);
                    groupHasHorizontal.RemoveAt(target);
                    groupHasVertical.RemoveAt(target);
                    groupLongestLine.RemoveAt(target);
                    groupColor.RemoveAt(target);

                    target = i;
                }

                if (target == -1)
                {
                    groupCells.Add(new HashSet<Vector2Int>(run.Positions));
                    groupHasHorizontal.Add(run.IsHorizontal);
                    groupHasVertical.Add(!run.IsHorizontal);
                    groupLongestLine.Add(run.Positions.Count);
                    groupColor.Add(run.Color);
                }
                else
                {
                    groupCells[target].UnionWith(run.Positions);
                    if (run.IsHorizontal) groupHasHorizontal[target] = true;
                    else groupHasVertical[target] = true;
                    groupLongestLine[target] = Mathf.Max(groupLongestLine[target], run.Positions.Count);
                }
            }

            var results = new List<MatchInfo>(groupCells.Count);
            for (int i = 0; i < groupCells.Count; i++)
            {
                bool isCross = groupHasHorizontal[i] && groupHasVertical[i];
                int longest = groupLongestLine[i];

                MatchShape shape;
                if (isCross)
                {
                    shape = MatchShape.LShapeOrT;
                }
                else if (longest >= 5)
                {
                    shape = MatchShape.Line5Plus;
                }
                else if (longest == 4)
                {
                    shape = MatchShape.Line4;
                }
                else
                {
                    shape = MatchShape.Line3;
                }

                var positions = new List<Vector2Int>(groupCells[i]);
                results.Add(new MatchInfo(positions, groupColor[i], shape, groupHasHorizontal[i]));
            }

            return results;
        }


        private void CollectSquares(Board board, HashSet<Vector2Int> claimed, List<MatchInfo> results)
        {
            for (int x = 0; x < board.Width - 1; x++)
            {
                for (int y = 0; y < board.Height - 1; y++)
                {
                    var a = new Vector2Int(x, y);
                    var b = new Vector2Int(x + 1, y);
                    var c = new Vector2Int(x, y + 1);
                    var d = new Vector2Int(x + 1, y + 1);

                    if (claimed.Contains(a) || claimed.Contains(b) ||
                        claimed.Contains(c) || claimed.Contains(d))
                    {
                        continue;
                    }

                    var tileA = board.GetTile(a);
                    if (tileA == null || tileA.IsColorless) continue;

                    var color = tileA.Color;
                    if (!SameColor(board, b, color) ||
                        !SameColor(board, c, color) ||
                        !SameColor(board, d, color))
                    {
                        continue;
                    }

                    var positions = new List<Vector2Int> { a, b, c, d };
                    claimed.UnionWith(positions);
                    results.Add(new MatchInfo(positions, color, MatchShape.Square2x2, true));
                }
            }
        }

        private bool SameColor(Board board, Vector2Int pos, TileColor color)
        {
            var tile = board.GetTile(pos);
            return tile != null && !tile.IsColorless && tile.Color == color;
        }
    }
}
