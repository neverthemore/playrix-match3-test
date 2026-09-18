using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.UI
{
    public class LevelResultScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI subtitleLabel;
        [SerializeField] private TextMeshProUGUI buttonLabel;
        [SerializeField] private Button actionButton;

        [Header("Text / Тексты")]
        [SerializeField] private string winTitle = "Level complete!";
        [SerializeField] private string loseTitle = "Out of moves";
        [SerializeField] private string winButton = "Next level";
        [SerializeField] private string loseButton = "Retry";

        [Header("Colors / Цвета")]
        [SerializeField] private Color winColor = new Color(0.25f, 0.78f, 0.42f);
        [SerializeField] private Color loseColor = new Color(0.88f, 0.36f, 0.30f);

        [Header("Flow / Переходы")]
        [SerializeField, Min(0f), Tooltip(
            "EN: Seconds the win result remains visible before the next level starts automatically. " +
            "Set to 0 to require the button. Losses always wait for Retry. / " +
            "RU: Сколько секунд виден результат победы до автоматического запуска следующего уровня. " +
            "0 требует нажатия кнопки. Поражение всегда ждёт Retry.")]
        private float winAutoAdvanceDelay = 1.75f;

        private System.Action _onContinue;

        private Coroutine _autoContinueRoutine;

        private bool _continueInvoked;

        private void Awake()
        {
            if (actionButton != null)
            {
                actionButton.onClick.AddListener(ContinueNow);
            }

        }

        public void Show(bool won, int levelIndex, System.Action onContinue)
        {
            CancelAutoContinue();
            _onContinue = onContinue;
            _continueInvoked = false;
            gameObject.SetActive(true);

            if (titleLabel != null)
            {
                titleLabel.text = won ? winTitle : loseTitle;
                titleLabel.color = won ? winColor : loseColor;
            }

            if (subtitleLabel != null)
            {
                subtitleLabel.text = won
                    ? $"Level {levelIndex + 1} cleared"
                    : $"Level {levelIndex + 1} failed";
            }

            if (buttonLabel != null)
            {
                buttonLabel.text = won ? winButton : loseButton;
            }

            PlayOpenAnimation();

            if (won && winAutoAdvanceDelay > 0f)
            {
                _autoContinueRoutine = StartCoroutine(AutoContinueAfterDelay());
            }
        }

        public void Hide()
        {
            CancelAutoContinue();
            _onContinue = null;
            _continueInvoked = true;

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            if (panel != null)
            {
                panel.DOKill();
            }
            gameObject.SetActive(false);
        }

        private IEnumerator AutoContinueAfterDelay()
        {
            yield return new WaitForSecondsRealtime(winAutoAdvanceDelay);
            _autoContinueRoutine = null;
            ContinueNow();
        }

        private void ContinueNow()
        {
            if (_continueInvoked) return;
            _continueInvoked = true;

            var callback = _onContinue;
            _onContinue = null;
            CancelAutoContinue();
            Hide();
            callback?.Invoke();
        }

        private void CancelAutoContinue()
        {
            if (_autoContinueRoutine == null) return;
            StopCoroutine(_autoContinueRoutine);
            _autoContinueRoutine = null;
        }

        private void PlayOpenAnimation()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                canvasGroup.DOKill();
                canvasGroup.DOFade(1f, 0.3f)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        canvasGroup.interactable = true;
                        canvasGroup.blocksRaycasts = true;
                    });
            }

            if (panel != null)
            {
                panel.DOKill();
                panel.localScale = Vector3.one * 0.8f;
                panel.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack, 1.4f).SetUpdate(true);
            }
        }

        private void OnDestroy()
        {
            CancelAutoContinue();
            if (actionButton != null) actionButton.onClick.RemoveListener(ContinueNow);
            if (canvasGroup != null) canvasGroup.DOKill();
            if (panel != null) panel.DOKill();
        }
    }
}
