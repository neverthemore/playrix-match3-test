using System;
using UnityEngine;

namespace Match3.Core.Domain
{
    
    
    public enum BlockKind
    {

        None = 0,

        Crate = 1,

        ColoredCrate = 2,

        ChameleonCrate = 3,

        Blocker = 4
    }


    public enum BlockDamageSource
    {

        AdjacentMatch = 0,

        BoosterBlast = 1
    }

    public readonly struct BlockDamage
    {

        public readonly BlockDamageSource Source;

        public readonly TileColor Color;

        public BlockDamage(BlockDamageSource source, TileColor color)
        {
            Source = source;
            Color = color;
        }
    }



    [Serializable]
    public class BlockLayerData
    {
        [Tooltip("EN: Kind of this box layer. / RU: Вид этого слоя ящика.")]
        public BlockKind kind = BlockKind.Crate;

        [Tooltip("EN: Required color for colored/chameleon crates. / RU: Требуемый цвет для цветного ящика и хамелеона.")]
        public TileColor color = TileColor.None;

        [Tooltip("EN: Hits needed to destroy this layer (>=1). / RU: Сколько ударов нужно для разрушения слоя (>=1).")]
        [Min(1)]
        public int lives = 1;

        public BlockLayerData() { }

        public BlockLayerData(BlockKind kind, TileColor color = TileColor.None, int lives = 1)
        {
            this.kind = kind;
            this.color = color;
            this.lives = Mathf.Max(1, lives);
        }
    }
}
