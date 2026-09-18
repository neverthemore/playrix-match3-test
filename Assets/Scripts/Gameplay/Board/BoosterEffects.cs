using System.Collections.Generic;
using Match3.Core.Domain;
using UnityEngine;

namespace Match3.Gameplay.Board
{
    public class BoosterEffects
    {
        private const int BombRadius = 2;

        private const int PlaneSplashRadius = 1;

        public HashSet<Vector2Int> GetAffected(Board board, Vector2Int origin, BoosterType booster,
            TileColor rainbowColor = TileColor.None, Vector2Int? planeTarget = null)
        {
            var affected = new HashSet<Vector2Int> { origin };

            switch (booster)
            {
                case BoosterType.LineHorizontal:
                    AddRow(board, origin.y, affected);
                    break;

                case BoosterType.LineVertical:
                    AddColumn(board, origin.x, affected);
                    break;

                case BoosterType.Bomb:
                    AddSquare(board, origin, BombRadius, affected);
                    break;

                case BoosterType.RainbowBall:
                    AddAllOfColor(board, rainbowColor, affected);
                    break;

                case BoosterType.Plane:
                    var target = planeTarget ?? origin;
                    affected.Add(target);
                    AddPlus(board, target, PlaneSplashRadius, affected);
                    break;
            }

            return affected;
        }


        public HashSet<Vector2Int> GetComboAffected(Board board, Vector2Int posA, Vector2Int posB,
            BoosterType typeA, BoosterType typeB)
        {
            var center = posB;
            var low = (BoosterType)Mathf.Min((int)typeA, (int)typeB);
            var high = (BoosterType)Mathf.Max((int)typeA, (int)typeB);

            var affected = new HashSet<Vector2Int> { posA, posB };

            if (low == BoosterType.RainbowBall && high == BoosterType.RainbowBall)
            {
                foreach (var cell in board.AllCells())
                {
                    if (cell.IsHole) continue;
                    affected.Add(new Vector2Int(cell.X, cell.Y));
                }
                return affected;
            }

            if (low == BoosterType.RainbowBall || high == BoosterType.RainbowBall)
            {
                var partner = low == BoosterType.RainbowBall ? high : low;
                var partnerPos = typeA == BoosterType.RainbowBall ? posB : posA;
                var color = GetColorAt(board, partnerPos);
                if (color == TileColor.None)
                {
                    color = GetMostCommonColor(board);
                }

                var seeds = new List<Vector2Int>();
                foreach (var cell in board.AllCells())
                {
                    if (cell.Tile == null || cell.Tile.IsColorless) continue;
                    if (cell.Tile.Color != color) continue;
                    seeds.Add(new Vector2Int(cell.X, cell.Y));
                }

                foreach (var seed in seeds)
                {
                    affected.Add(seed);
                    var asBooster = partner == BoosterType.LineHorizontal || partner == BoosterType.LineVertical
                        ? ((seed.x + seed.y) % 2 == 0 ? BoosterType.LineHorizontal : BoosterType.LineVertical)
                        : partner;

                    affected.UnionWith(GetAffected(board, seed, asBooster, color, seed));
                }
                return affected;
            }

            if (low.IsLine() && high.IsLine())
            {
                AddRow(board, center.y, affected);
                AddColumn(board, center.x, affected);
                return affected;
            }

            if ((low.IsLine() && high == BoosterType.Bomb) || (low == BoosterType.Bomb && high.IsLine()))
            {
                for (int offset = -1; offset <= 1; offset++)
                {
                    AddRow(board, center.y + offset, affected);
                    AddColumn(board, center.x + offset, affected);
                }
                return affected;
            }

            if (low == BoosterType.Bomb && high == BoosterType.Bomb)
            {
                AddSquare(board, center, BombRadius * 2, affected);
                return affected;
            }

            if (low == BoosterType.Plane && high == BoosterType.Plane)
            {
                var firstTarget = FindPlaneTarget(board, center, affected);
                affected.UnionWith(GetAffected(board, center, BoosterType.Plane, TileColor.None, firstTarget));

                var secondTarget = FindPlaneTarget(board, center, affected);
                affected.UnionWith(GetAffected(board, center, BoosterType.Plane, TileColor.None, secondTarget));
                return affected;
            }

            if ((low == BoosterType.Plane && high.IsLine()) || (low.IsLine() && high == BoosterType.Plane))
            {
                var planeTarget = FindPlaneTarget(board, center, affected);
                affected.UnionWith(GetAffected(board, center, BoosterType.Plane, TileColor.None, planeTarget));

                AddRow(board, planeTarget.y, affected);
                AddColumn(board, planeTarget.x, affected);
                return affected;
            }

            if ((low == BoosterType.Plane && high == BoosterType.Bomb) ||
                (low == BoosterType.Bomb && high == BoosterType.Plane))
            {
                var planeTarget = FindPlaneTarget(board, center, affected);
                AddSquare(board, planeTarget, BombRadius, affected);
                AddSquare(board, center, PlaneSplashRadius, affected);
                return affected;
            }

            affected.UnionWith(GetAffected(board, posA, typeA));
            affected.UnionWith(GetAffected(board, posB, typeB));
            return affected;
        }


        public Vector2Int FindPlaneTarget(Board board, Vector2Int origin,
            HashSet<Vector2Int> exclude = null, IReadOnlyDictionary<GoalKey, int> goalWeights = null)
        {
            Vector2Int best = origin;
            int bestScore = int.MinValue;

            foreach (var cell in board.AllCells())
            {
                var pos = new Vector2Int(cell.X, cell.Y);
                if (cell.IsHole) continue;
                if (exclude != null && exclude.Contains(pos)) continue;

                int score = ScoreCell(cell, goalWeights);
                if (score <= 0) continue;

                int distance = Mathf.Abs(pos.x - origin.x) + Mathf.Abs(pos.y - origin.y);
                score = score * 100 - distance;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = pos;
                }
            }

            if (bestScore == int.MinValue)
            {
                foreach (var cell in board.AllCells())
                {
                    var pos = new Vector2Int(cell.X, cell.Y);
                    if (exclude != null && exclude.Contains(pos)) continue;
                    if (cell.Tile != null || cell.HasBlock)
                    {
                        return pos;
                    }
                }
            }

            return best;
        }

        private int ScoreCell(Tiles.Cell cell, IReadOnlyDictionary<GoalKey, int> goalWeights)
        {
            int score = 0;

            if (cell.HasBlock)
            {
                var block = cell.Block;

                if (block.Kind == BlockKind.Blocker)
                {
                    return 0;
                }

                score += 5;
                score += GetWeight(goalWeights, GoalKey.ForBlock(block.Kind)) * 10;
                return score;
            }

            if (cell.Tile == null)
            {
                return 0;
            }

            if (cell.Tile.IsBooster)
            {
                score += 4;
                score += GetWeight(goalWeights, GoalKey.ForBooster(cell.Tile.Booster)) * 10;
                return score;
            }

            score += 1;
            score += GetWeight(goalWeights, GoalKey.ForColor(cell.Tile.Color)) * 3;
            return score;
        }

        private int GetWeight(IReadOnlyDictionary<GoalKey, int> weights, GoalKey key)
        {
            if (weights == null) return 0;
            return weights.TryGetValue(key, out int weight) ? weight : 0;
        }


        private void AddRow(Board board, int y, HashSet<Vector2Int> affected)
        {
            if (y < 0 || y >= board.Height) return;
            for (int x = 0; x < board.Width; x++)
            {
                affected.Add(new Vector2Int(x, y));
            }
        }

        private void AddColumn(Board board, int x, HashSet<Vector2Int> affected)
        {
            if (x < 0 || x >= board.Width) return;
            for (int y = 0; y < board.Height; y++)
            {
                affected.Add(new Vector2Int(x, y));
            }
        }

        private void AddSquare(Board board, Vector2Int center, int radius, HashSet<Vector2Int> affected)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    var pos = new Vector2Int(center.x + dx, center.y + dy);
                    if (board.IsInBounds(pos))
                    {
                        affected.Add(pos);
                    }
                }
            }
        }

        private void AddPlus(Board board, Vector2Int center, int radius, HashSet<Vector2Int> affected)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) > radius) continue;
                    var pos = new Vector2Int(center.x + dx, center.y + dy);
                    if (board.IsInBounds(pos))
                    {
                        affected.Add(pos);
                    }
                }
            }
        }

        private void AddAllOfColor(Board board, TileColor color, HashSet<Vector2Int> affected)
        {
            if (color == TileColor.None)
            {
                color = GetMostCommonColor(board);
            }
            if (color == TileColor.None) return;

            foreach (var cell in board.AllCells())
            {
                if (cell.Tile == null || cell.Tile.IsColorless) continue;
                if (cell.Tile.Color == color)
                {
                    affected.Add(new Vector2Int(cell.X, cell.Y));
                }
            }
        }

        private TileColor GetColorAt(Board board, Vector2Int pos)
        {
            var tile = board.GetTile(pos);
            return tile == null || tile.IsColorless ? TileColor.None : tile.Color;
        }

        public TileColor GetMostCommonColor(Board board)
        {
            var counts = new Dictionary<TileColor, int>();
            foreach (var cell in board.AllCells())
            {
                if (cell.Tile == null || cell.Tile.IsColorless) continue;
                counts.TryGetValue(cell.Tile.Color, out int count);
                counts[cell.Tile.Color] = count + 1;
            }

            var best = TileColor.None;
            int bestCount = 0;
            foreach (var kvp in counts)
            {
                if (kvp.Value > bestCount)
                {
                    bestCount = kvp.Value;
                    best = kvp.Key;
                }
            }
            return best;
        }
    }
}
