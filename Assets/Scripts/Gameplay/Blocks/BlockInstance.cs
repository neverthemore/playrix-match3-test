using System.Collections.Generic;
using Match3.Core.Domain;

namespace Match3.Gameplay.Blocks
{
    public class BlockInstance
    {
        public BlockKind Kind { get; private set; }

        public TileColor Color { get; set; }

        public int Lives { get; set; }

        public int MaxLives { get; private set; }

        public BlockInstance Inner { get; private set; }

        public BlockInstance(BlockKind kind, TileColor color, int lives, BlockInstance inner = null)
        {
            Kind = kind;
            Color = color;
            Lives = lives < 1 ? 1 : lives;
            MaxLives = Lives;
            Inner = inner;
        }

        public bool BlocksChips => true;

        public static BlockInstance BuildChain(IReadOnlyList<BlockLayerData> layers)
        {
            if (layers == null || layers.Count == 0)
            {
                return null;
            }

            BlockInstance current = null;
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                var layer = layers[i];
                if (layer == null || layer.kind == BlockKind.None) continue;
                current = new BlockInstance(layer.kind, layer.color, layer.lives, current);
            }
            return current;
        }
    }
}
