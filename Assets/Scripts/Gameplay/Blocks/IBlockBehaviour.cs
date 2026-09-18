using Match3.Core.Domain;

namespace Match3.Gameplay.Blocks
{
    public interface IBlockBehaviour
    {
        BlockKind Kind { get; }

        bool CanBeDamagedBy(BlockInstance block, BlockDamage damage);

        bool OnTurnCompleted(BlockInstance block, IBlockTurnContext context);
    }

    public interface IBlockTurnContext
    {
        TileColor GetRandomActiveColor();

        TileColor GetRandomActiveColorExcept(TileColor excluded);
    }
}
