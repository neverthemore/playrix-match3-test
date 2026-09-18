using System.Collections.Generic;
using Match3.Core.Domain;

namespace Match3.Gameplay.Blocks
{
    public class BlockBehaviourRegistry
    {
        private readonly Dictionary<BlockKind, IBlockBehaviour> _behaviours =
            new Dictionary<BlockKind, IBlockBehaviour>();

        private static readonly IBlockBehaviour NullBehaviour = new BlockerBehaviour();

        public BlockBehaviourRegistry()
        {
            Register(new CrateBehaviour());
            Register(new ColoredCrateBehaviour());
            Register(new ChameleonCrateBehaviour());
            Register(new BlockerBehaviour());
        }

        public void Register(IBlockBehaviour behaviour)
        {
            if (behaviour == null) return;
            _behaviours[behaviour.Kind] = behaviour;
        }

        public IBlockBehaviour Get(BlockKind kind)
        {
            return _behaviours.TryGetValue(kind, out var behaviour) ? behaviour : NullBehaviour;
        }

        public IBlockBehaviour Get(BlockInstance block)
        {
            return block == null ? NullBehaviour : Get(block.Kind);
        }
    }
}
