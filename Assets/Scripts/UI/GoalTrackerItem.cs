using DG.Tweening;
using Match3.Core.Domain;
using Match3.Gameplay.Tiles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.UI
{
    public class GoalTrackerItem : MonoBehaviour
    {
        [SerializeField, Tooltip("EN: Icon of the tracked thing. / RU: Иконка отслеживаемого объекта.")]
        private Image icon;

        [SerializeField] private TextMeshProUGUI progressLabel;

        [SerializeField, Tooltip("EN: Shown when the goal is met. / RU: Показывается при выполнении цели.")]
        private GameObject checkmark;

        private TileDatabase _database;
        private bool _isComplete;

        public void Initialize(GoalKey key, int target, TileDatabase database)
        {
            _database = database;
            _isComplete = false;

            if (icon != null)
            {
                switch (key.Kind)
                {
                    case GoalKind.CollectColor:
                        var definition = database.GetDefinition(key.Color);
                        icon.sprite = definition != null ? definition.Sprite : null;
                        icon.color = definition != null ? definition.Tint : Color.white;
                        break;

                    case GoalKind.CollectBooster:
                        icon.sprite = key.Booster == BoosterType.RainbowBall
                            ? database.RainbowBallSprite
                            : database.GetBoosterOverlay(key.Booster);
                        icon.color = Color.white;
                        break;

                    case GoalKind.DestroyBlock:
                        icon.sprite = database.GetBlockSprite(key.Block);
                        icon.color = Color.white;
                        break;
                }
            }

            SetProgress(0, target);
            if (checkmark != null) checkmark.SetActive(false);
        }

        public void SetProgress(int current, int target)
        {
            bool wasComplete = _isComplete;
            _isComplete = current >= target;

            if (progressLabel != null)
            {
                progressLabel.text = _isComplete ? string.Empty : $"{current}/{target}";
            }

            if (_isComplete && !wasComplete)
            {
                PlayCompletion();
            }
        }

        private void PlayCompletion()
        {
            if (checkmark != null)
            {
                checkmark.SetActive(true);
                checkmark.transform.localScale = Vector3.zero;
                checkmark.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack, 1.6f);
            }

            if (icon != null)
            {
                icon.transform.DOKill();
                icon.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 6, 0.7f);
            }
        }

        public void PlayProgressPulse()
        {
            if (icon == null) return;
            icon.transform.DOKill();
            icon.transform.localScale = Vector3.one;
            icon.transform.DOPunchScale(Vector3.one * 0.18f, 0.28f, 4, 0.8f);
        }

        private void OnDestroy()
        {
            if (icon != null) icon.transform.DOKill();
            if (checkmark != null) checkmark.transform.DOKill();
        }
    }
}
