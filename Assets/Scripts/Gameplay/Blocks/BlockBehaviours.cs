using Match3.Core.Domain;

namespace Match3.Gameplay.Blocks
{
    public class CrateBehaviour : IBlockBehaviour
    {
        public BlockKind Kind => BlockKind.Crate;

        public bool CanBeDamagedBy(BlockInstance block, BlockDamage damage) => true;

        public bool OnTurnCompleted(BlockInstance block, IBlockTurnContext context) => false;
    }

    public class ColoredCrateBehaviour : IBlockBehaviour
    {
        public BlockKind Kind => BlockKind.ColoredCrate;

        public bool CanBeDamagedBy(BlockInstance block, BlockDamage damage)
        {
            if (damage.Source == BlockDamageSource.BoosterBlast)
            {
                return true;
            }
            return damage.Color == block.Color;
        }

        public bool OnTurnCompleted(BlockInstance block, IBlockTurnContext context) => false;
    }

    public class ChameleonCrateBehaviour : IBlockBehaviour
    {
        private readonly ColoredCrateBehaviour _colorRule = new ColoredCrateBehaviour();

        public BlockKind Kind => BlockKind.ChameleonCrate;

        public bool CanBeDamagedBy(BlockInstance block, BlockDamage damage)
        {
            return _colorRule.CanBeDamagedBy(block, damage);
        }

        public bool OnTurnCompleted(BlockInstance block, IBlockTurnContext context)
        {
            var newColor = context.GetRandomActiveColorExcept(block.Color);
            if (newColor == TileColor.None || newColor == block.Color)
            {
                return false;
            }

            block.Color = newColor;
            return true;
        }
    }

    public class BlockerBehaviour : IBlockBehaviour
    {
        public BlockKind Kind => BlockKind.Blocker;

        public bool CanBeDamagedBy(BlockInstance block, BlockDamage damage) => false;

        public bool OnTurnCompleted(BlockInstance block, IBlockTurnContext context) => false;
    }
}
