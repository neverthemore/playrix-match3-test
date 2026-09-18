using System;
using UnityEngine;

namespace Match3.Core.Domain
{

    public enum GoalKind
    {

        CollectColor = 0,

        CollectBooster = 1,


        DestroyBlock = 2
    }

   
    [Serializable]
    public class LevelGoal
    {
        [Tooltip("EN: What this goal counts. / RU: Что считает эта цель.")]
        public GoalKind kind = GoalKind.CollectColor;

        [Tooltip("EN: Target color, used when kind = CollectColor. / RU: Цвет цели, используется при kind = CollectColor.")]
        public TileColor color = TileColor.Red;

        [Tooltip("EN: Target booster, used when kind = CollectBooster. / RU: Бустер цели, используется при kind = CollectBooster.")]
        public BoosterType booster = BoosterType.Bomb;

        [Tooltip("EN: Target box kind, used when kind = DestroyBlock. / RU: Вид ящика, используется при kind = DestroyBlock.")]
        public BlockKind blockKind = BlockKind.Crate;

        [Tooltip("EN: How many to collect. / RU: Сколько нужно собрать.")]
        [Min(1)]
        public int amount = 10;

        public LevelGoal() { }

        public static LevelGoal Color_(TileColor color, int amount)
        {
            return new LevelGoal { kind = GoalKind.CollectColor, color = color, amount = amount };
        }

        public static LevelGoal Booster_(BoosterType booster, int amount)
        {
            return new LevelGoal { kind = GoalKind.CollectBooster, booster = booster, amount = amount };
        }

        public static LevelGoal Block_(BlockKind blockKind, int amount)
        {
            return new LevelGoal { kind = GoalKind.DestroyBlock, blockKind = blockKind, amount = amount };
        }

       
        public GoalKey Key => new GoalKey(kind, color, booster, blockKind);

        public string Describe()
        {
            switch (kind)
            {
                case GoalKind.CollectBooster: return booster.ToString();
                case GoalKind.DestroyBlock: return blockKind.ToString();
                default: return color.ToString();
            }
        }
    }


    public readonly struct GoalKey : IEquatable<GoalKey>
    {
        public readonly GoalKind Kind;
        public readonly TileColor Color;
        public readonly BoosterType Booster;
        public readonly BlockKind Block;

        public GoalKey(GoalKind kind, TileColor color, BoosterType booster, BlockKind block)
        {
            Kind = kind;

            Color = kind == GoalKind.CollectColor ? color : TileColor.None;
            Booster = kind == GoalKind.CollectBooster ? booster : BoosterType.None;
            Block = kind == GoalKind.DestroyBlock ? block : BlockKind.None;
        }

        public static GoalKey ForColor(TileColor color) => new GoalKey(GoalKind.CollectColor, color, BoosterType.None, BlockKind.None);
        public static GoalKey ForBooster(BoosterType booster) => new GoalKey(GoalKind.CollectBooster, TileColor.None, booster, BlockKind.None);
        public static GoalKey ForBlock(BlockKind block) => new GoalKey(GoalKind.DestroyBlock, TileColor.None, BoosterType.None, block);

        public bool Equals(GoalKey other)
        {
            return Kind == other.Kind && Color == other.Color && Booster == other.Booster && Block == other.Block;
        }

        public override bool Equals(object obj) => obj is GoalKey other && Equals(other);

        public override int GetHashCode()
        {

            return ((int)Kind << 24) | ((int)Color << 16) | ((int)Booster << 8) | (int)Block;
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case GoalKind.CollectBooster: return $"Booster:{Booster}";
                case GoalKind.DestroyBlock: return $"Block:{Block}";
                default: return $"Color:{Color}";
            }
        }
    }
}
