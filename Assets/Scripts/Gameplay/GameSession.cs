using System.Collections.Generic;
using Match3.Core.Domain;
using Match3.Core.Events;
using UnityEngine;

namespace Match3.Gameplay
{
    public class GameSession
    {
        public int RemainingMoves { get; private set; }
        public bool IsFinished { get; private set; }
        public int LevelIndex { get; private set; }

        private readonly EventBus _eventBus;

        private readonly Dictionary<GoalKey, int> _progress = new Dictionary<GoalKey, int>();

        private readonly Dictionary<GoalKey, int> _targets = new Dictionary<GoalKey, int>();

        private readonly Dictionary<GoalKey, int> _remaining = new Dictionary<GoalKey, int>();

        private bool _movePending;

        public GameSession(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public IReadOnlyDictionary<GoalKey, int> RemainingGoals => _remaining;

        public void StartLevel(LevelConfig config, int levelIndex)
        {
            LevelIndex = levelIndex;
            RemainingMoves = config.MoveLimit;
            IsFinished = false;
            _movePending = false;

            _progress.Clear();
            _targets.Clear();

            foreach (var goal in config.Goals)
            {
                var key = goal.Key;
                _targets.TryGetValue(key, out int existing);
                _targets[key] = existing + goal.amount;
                _progress[key] = 0;
            }

            RecomputeRemaining();
            Subscribe();

            _eventBus.Publish(new LevelStartedEvent(config, levelIndex));
            _eventBus.Publish(new MovesChangedEvent(RemainingMoves));
            PublishAllGoals();
        }

        public void Shutdown()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            _eventBus.Subscribe<GoalContributionEvent>(OnGoalContribution);
            _eventBus.Subscribe<PlayerMoveCommittedEvent>(OnPlayerMoveCommitted);
            _eventBus.Subscribe<BoardInputLockChangedEvent>(OnInputLockChanged);
        }

        private void Unsubscribe()
        {
            _eventBus.Unsubscribe<GoalContributionEvent>(OnGoalContribution);
            _eventBus.Unsubscribe<PlayerMoveCommittedEvent>(OnPlayerMoveCommitted);
            _eventBus.Unsubscribe<BoardInputLockChangedEvent>(OnInputLockChanged);
        }


        private void OnGoalContribution(GoalContributionEvent evt)
        {
            if (IsFinished) return;
            if (!_targets.TryGetValue(evt.Key, out int target)) return;

            int current = Mathf.Min(target, _progress[evt.Key] + evt.Amount);
            _progress[evt.Key] = current;
            _remaining[evt.Key] = target - current;

            _eventBus.Publish(new GoalProgressChangedEvent(evt.Key, current, target));

            if (AreAllGoalsMet())
            {
                Finish(won: true);
            }
        }

        private bool AreAllGoalsMet()
        {
            if (_targets.Count == 0) return false;

            foreach (var kvp in _targets)
            {
                if (_progress[kvp.Key] < kvp.Value)
                {
                    return false;
                }
            }
            return true;
        }

        private void RecomputeRemaining()
        {
            _remaining.Clear();
            foreach (var kvp in _targets)
            {
                _remaining[kvp.Key] = kvp.Value - _progress[kvp.Key];
            }
        }

        private void PublishAllGoals()
        {
            foreach (var kvp in _targets)
            {
                _eventBus.Publish(new GoalProgressChangedEvent(kvp.Key, _progress[kvp.Key], kvp.Value));
            }
        }


        private void OnPlayerMoveCommitted(PlayerMoveCommittedEvent evt)
        {
            _movePending = true;
        }

        private void OnInputLockChanged(BoardInputLockChangedEvent evt)
        {
            bool boardIsIdle = !evt.IsLocked;
            if (!boardIsIdle || !_movePending) return;

            _movePending = false;
            ConsumeMove();
        }

        private void ConsumeMove()
        {
            if (IsFinished) return;

            RemainingMoves = Mathf.Max(0, RemainingMoves - 1);
            _eventBus.Publish(new MovesChangedEvent(RemainingMoves));

            if (AreAllGoalsMet())
            {
                Finish(won: true);
                return;
            }

            if (RemainingMoves <= 0)
            {
                Finish(won: false);
            }
        }

        private void Finish(bool won)
        {
            if (IsFinished) return;
            IsFinished = true;

            if (won)
            {
                _eventBus.Publish(new LevelWonEvent(LevelIndex, RemainingMoves));
            }
            else
            {
                _eventBus.Publish(new LevelLostEvent(LevelIndex));
            }
        }


        public void CheatForceWin() => Finish(won: true);

        public void CheatForceLose() => Finish(won: false);
    }
}
