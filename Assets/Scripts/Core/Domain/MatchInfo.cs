using System.Collections.Generic;
using UnityEngine;

namespace Match3.Core.Domain
{
    public enum MatchShape
    {
        Line3 = 0,

        Square2x2 = 1,

        Line4 = 2,

        LShapeOrT = 3,

        Line5Plus = 4
    }

    public class MatchInfo
    {
        public List<Vector2Int> Positions { get; }

        public TileColor Color { get; }

        public MatchShape Shape { get; }

        public bool IsHorizontal { get; }

        public MatchInfo(List<Vector2Int> positions, TileColor color, MatchShape shape, bool isHorizontal)
        {
            Positions = positions;
            Color = color;
            Shape = shape;
            IsHorizontal = isHorizontal;
        }

        public int Count => Positions.Count;

        public Vector2Int GetBoosterSpawnPosition(Vector2Int? preferredPosition)
        {
            if (preferredPosition.HasValue && Positions.Contains(preferredPosition.Value))
            {
                return preferredPosition.Value;
            }
            return Positions[Positions.Count / 2];
        }
    }
}
