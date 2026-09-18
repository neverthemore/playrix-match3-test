using System.Collections.Generic;
using Match3.Core.Domain;
using UnityEngine;

namespace Match3.Core.Events
{
    public readonly struct TileSwappedEvent : IGameEvent
    {
        public readonly Vector2Int PositionA;
        public readonly Vector2Int PositionB;

        public TileSwappedEvent(Vector2Int a, Vector2Int b)
        {
            PositionA = a;
            PositionB = b;
        }
    }

    public readonly struct PlayerMoveCommittedEvent : IGameEvent
    {
        public readonly int TurnNumber;

        public PlayerMoveCommittedEvent(int turnNumber)
        {
            TurnNumber = turnNumber;
        }
    }

    public readonly struct InvalidSwapEvent : IGameEvent
    {
        public readonly Vector2Int PositionA;
        public readonly Vector2Int PositionB;

        public InvalidSwapEvent(Vector2Int a, Vector2Int b)
        {
            PositionA = a;
            PositionB = b;
        }
    }


    public readonly struct MatchFoundEvent : IGameEvent
    {
        public readonly IReadOnlyList<MatchInfo> Matches;


        public readonly int ChainIndex;

        public MatchFoundEvent(IReadOnlyList<MatchInfo> matches, int chainIndex)
        {
            Matches = matches;
            ChainIndex = chainIndex;
        }
    }


    public readonly struct BoosterCreatedEvent : IGameEvent
    {
        public readonly Vector2Int Position;
        public readonly BoosterType Booster;

        public BoosterCreatedEvent(Vector2Int position, BoosterType booster)
        {
            Position = position;
            Booster = booster;
        }
    }

    public readonly struct BoosterActivatedEvent : IGameEvent
    {
        public readonly Vector2Int Position;
        public readonly BoosterType Booster;
        public readonly IReadOnlyList<Vector2Int> AffectedCells;

        public readonly int ChainDepth;

        public readonly Vector2Int TargetCell;

        public BoosterActivatedEvent(Vector2Int position, BoosterType booster,
            IReadOnlyList<Vector2Int> affectedCells, int chainDepth, Vector2Int targetCell)
        {
            Position = position;
            Booster = booster;
            AffectedCells = affectedCells;
            ChainDepth = chainDepth;
            TargetCell = targetCell;
        }
    }

    public readonly struct BoosterComboEvent : IGameEvent
    {
        public readonly Vector2Int Position;
        public readonly BoosterType FirstBooster;
        public readonly BoosterType SecondBooster;
        public readonly IReadOnlyList<Vector2Int> AffectedCells;

        public BoosterComboEvent(Vector2Int position, BoosterType first, BoosterType second,
            IReadOnlyList<Vector2Int> affectedCells)
        {
            Position = position;
            FirstBooster = first;
            SecondBooster = second;
            AffectedCells = affectedCells;
        }
    }

    
    public readonly struct TilesClearedEvent : IGameEvent
    {
        public readonly IReadOnlyList<Vector2Int> Positions;

        public TilesClearedEvent(IReadOnlyList<Vector2Int> positions)
        {
            Positions = positions;
        }
    }

    
    public readonly struct BlockDamagedEvent : IGameEvent
    {
        public readonly Vector2Int Position;
        public readonly BlockKind Kind;
        public readonly int RemainingLives;

        public BlockDamagedEvent(Vector2Int position, BlockKind kind, int remainingLives)
        {
            Position = position;
            Kind = kind;
            RemainingLives = remainingLives;
        }
    }

    public readonly struct BlockDestroyedEvent : IGameEvent
    {
        public readonly Vector2Int Position;
        public readonly BlockKind Kind;
        public readonly BlockKind RevealedKind;

        public BlockDestroyedEvent(Vector2Int position, BlockKind kind, BlockKind revealedKind)
        {
            Position = position;
            Kind = kind;
            RevealedKind = revealedKind;
        }
    }

    
    public readonly struct BlockColorChangedEvent : IGameEvent
    {
        public readonly Vector2Int Position;
        public readonly TileColor NewColor;

        public BlockColorChangedEvent(Vector2Int position, TileColor newColor)
        {
            Position = position;
            NewColor = newColor;
        }
    }

    public readonly struct BoardRefillStartedEvent : IGameEvent { }

    public readonly struct BoardRefillCompletedEvent : IGameEvent { }

    public readonly struct BoardShuffledEvent : IGameEvent { }

    public readonly struct BoardInputLockChangedEvent : IGameEvent
    {
        public readonly bool IsLocked;

        public BoardInputLockChangedEvent(bool isLocked)
        {
            IsLocked = isLocked;
        }
    }

    public readonly struct GoalContributionEvent : IGameEvent
    {
        public readonly GoalKey Key;
        public readonly int Amount;
        public readonly Vector2Int SourcePosition;

        public GoalContributionEvent(GoalKey key, int amount, Vector2Int sourcePosition)
        {
            Key = key;
            Amount = amount;
            SourcePosition = sourcePosition;
        }
    }

    public readonly struct GoalProgressChangedEvent : IGameEvent
    {
        public readonly GoalKey Key;
        public readonly int Current;
        public readonly int Target;

        public GoalProgressChangedEvent(GoalKey key, int current, int target)
        {
            Key = key;
            Current = current;
            Target = target;
        }
    }

    public readonly struct MovesChangedEvent : IGameEvent
    {
        public readonly int RemainingMoves;

        public MovesChangedEvent(int remainingMoves)
        {
            RemainingMoves = remainingMoves;
        }
    }

    public readonly struct TurnCompletedEvent : IGameEvent
    {
        public readonly int TurnNumber;

        public TurnCompletedEvent(int turnNumber)
        {
            TurnNumber = turnNumber;
        }
    }

   
    public readonly struct LevelStartedEvent : IGameEvent
    {
        public readonly LevelConfig Config;
        public readonly int LevelIndex;

        public LevelStartedEvent(LevelConfig config, int levelIndex)
        {
            Config = config;
            LevelIndex = levelIndex;
        }
    }

    public readonly struct LevelWonEvent : IGameEvent
    {
        public readonly int LevelIndex;
        public readonly int MovesLeft;

        public LevelWonEvent(int levelIndex, int movesLeft)
        {
            LevelIndex = levelIndex;
            MovesLeft = movesLeft;
        }
    }

    public readonly struct LevelLostEvent : IGameEvent
    {
        public readonly int LevelIndex;

        public LevelLostEvent(int levelIndex)
        {
            LevelIndex = levelIndex;
        }
    }
}
