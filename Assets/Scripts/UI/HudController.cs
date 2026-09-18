using System.Collections.Generic;
using DG.Tweening;
using Match3.Core.Domain;
using Match3.Core.Events;
using Match3.Gameplay.Tiles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.UI
{
    public class HudController : MonoBehaviour
    {
        [Header("Labels / Подписи")]
        [SerializeField] private TextMeshProUGUI levelLabel;
        [SerializeField] private TextMeshProUGUI movesLabel;

        [Header("Moves styling / Оформление ходов")]
        [SerializeField] private Image movesBackground;
        [SerializeField] private Color movesNormalColor = new Color(0.22f, 0.56f, 0.92f);
        [SerializeField] private Color movesLowColor = new Color(0.90f, 0.32f, 0.26f);

        [SerializeField, Tooltip("EN: Below this, the counter turns red and pulses. / RU: Ниже этого счётчик краснеет и пульсирует.")]
        private int lowMovesThreshold = 5;

        [Header("Goals / Цели")]
        [SerializeField] private Transform goalContainer;
        [SerializeField] private GoalTrackerItem goalItemPrefab;
        [SerializeField] private TileDatabase tileDatabase;

        private EventBus _eventBus;

        private readonly Dictionary<GoalKey, GoalTrackerItem> _trackers = new Dictionary<GoalKey, GoalTrackerItem>();

        public void Setup(EventBus eventBus, LevelConfig config, int levelIndex)
        {
            Shutdown();
            _eventBus = eventBus;

            if (levelLabel != null)
            {
                levelLabel.text = string.IsNullOrEmpty(config.DisplayName)
                    ? $"Level {levelIndex + 1}"
                    : config.DisplayName;
            }

            BuildTrackers(config);
            UpdateMoves(config.MoveLimit);

            _eventBus.Subscribe<MovesChangedEvent>(OnMovesChanged);
            _eventBus.Subscribe<GoalProgressChangedEvent>(OnGoalProgressChanged);
        }

        public void Shutdown()
        {
            if (_eventBus == null) return;

            _eventBus.Unsubscribe<MovesChangedEvent>(OnMovesChanged);
            _eventBus.Unsubscribe<GoalProgressChangedEvent>(OnGoalProgressChanged);
            _eventBus = null;
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void BuildTrackers(LevelConfig config)
        {
            if (goalContainer == null || goalItemPrefab == null) return;

            for (int i = goalContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(goalContainer.GetChild(i).gameObject);
            }
            _trackers.Clear();

            var totals = new Dictionary<GoalKey, int>();
            var order = new List<GoalKey>();

            foreach (var goal in config.Goals)
            {
                var key = goal.Key;
                if (!totals.ContainsKey(key))
                {
                    order.Add(key);
                }
                totals.TryGetValue(key, out int existing);
                totals[key] = existing + goal.amount;
            }

            foreach (var key in order)
            {
                var item = Instantiate(goalItemPrefab, goalContainer);
                item.Initialize(key, totals[key], tileDatabase);
                _trackers[key] = item;
            }
        }

        private void OnMovesChanged(MovesChangedEvent evt)
        {
            UpdateMoves(evt.RemainingMoves);
        }

        private void UpdateMoves(int remaining)
        {
            if (movesLabel != null)
            {
                movesLabel.text = remaining.ToString();
                movesLabel.transform.DOKill();
                movesLabel.transform.localScale = Vector3.one;
                movesLabel.transform.DOPunchScale(Vector3.one * 0.2f, 0.28f, 4, 0.7f);
            }

            if (movesBackground == null) return;

            bool isLow = remaining <= lowMovesThreshold;
            movesBackground.DOKill();
            movesBackground.DOColor(isLow ? movesLowColor : movesNormalColor, 0.3f);

            if (isLow)
            {
                movesBackground.transform.DOKill();
                movesBackground.transform.localScale = Vector3.one;
                movesBackground.transform.DOPunchScale(Vector3.one * 0.12f, 0.3f, 3, 0.6f);
            }
        }

        private void OnGoalProgressChanged(GoalProgressChangedEvent evt)
        {
            if (!_trackers.TryGetValue(evt.Key, out var tracker)) return;

            tracker.SetProgress(evt.Current, evt.Target);
            tracker.PlayProgressPulse();
        }
    }
}
