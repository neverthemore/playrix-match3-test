using System.Collections.Generic;
using Match3.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.UI
{
    public class LevelMapController : MonoBehaviour
    {
        [Header("Map / Карта")]
        [SerializeField] private GameObject mapBody;
        [SerializeField] private Transform levelButtonContainer;
        [SerializeField] private Button levelButtonPrefab;

        [Header("Navigation / Навигация")]
        [SerializeField] private Button openMapButton;

        [Header("Labels / Подписи")]
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI progressLabel;

        [Header("Gameplay visibility / Видимость игры")]
        [SerializeField] private GameObject gameplayBoard;
        [SerializeField] private GameObject cheatPanel;

        [Header("Button colors / Цвета кнопок")]
        [SerializeField] private Color unlockedColor = new Color(0.22f, 0.56f, 0.92f);
        [SerializeField] private Color lockedColor = new Color(0.28f, 0.30f, 0.38f);

        private readonly List<Button> _levelButtons = new List<Button>();
        private bool _initialized;

        private void Awake()
        {
            if (openMapButton != null)
            {
                openMapButton.onClick.AddListener(OpenMap);
            }
        }

        public void Initialize()
        {
            _initialized = true;
            BuildLevelButtons();
            RefreshProgress();
            ShowMap();
        }

        public void ShowMap()
        {
            if (mapBody != null)
            {
                mapBody.SetActive(true);
            }

            SetGameplayUiVisible(false);

            if (_initialized)
            {
                RefreshProgress();
            }
        }

        public void HideMap()
        {
            if (mapBody != null)
            {
                mapBody.SetActive(false);
            }

            SetGameplayUiVisible(true);
        }

        private void SetGameplayUiVisible(bool visible)
        {
            if (openMapButton != null)
            {
                var hud = openMapButton.transform.parent != null
                    ? openMapButton.transform.parent.gameObject
                    : openMapButton.gameObject;
                hud.SetActive(visible);
            }

            if (gameplayBoard != null)
            {
                gameplayBoard.SetActive(visible);
            }

            if (cheatPanel != null)
            {
                cheatPanel.SetActive(visible);
            }
        }

        public void RefreshProgress()
        {
            var controller = GameFlow.Controller;
            int total = controller?.LevelCount ?? 0;
            int unlockedCount = 0;

            if (titleLabel != null)
            {
                titleLabel.text = "КАРТА УРОВНЕЙ";
            }

            for (int i = 0; i < _levelButtons.Count; i++)
            {
                var button = _levelButtons[i];
                if (button == null) continue;

                bool unlocked = controller != null && controller.IsLevelUnlocked(i);
                if (unlocked) unlockedCount++;

                button.interactable = unlocked;

                var background = button.targetGraphic as Image;
                if (background == null)
                {
                    background = button.GetComponent<Image>();
                }
                if (background != null)
                {
                    background.color = unlocked ? unlockedColor : lockedColor;
                }

                var numberLabel = button.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (numberLabel != null)
                {
                    numberLabel.text = (i + 1).ToString();
                    numberLabel.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.55f);
                }

                var stateLabel = button.transform.Find("StateLabel")?.GetComponent<TextMeshProUGUI>();
                if (stateLabel != null)
                {
                    stateLabel.text = unlocked ? "ИГРАТЬ" : "ЗАКРЫТ";
                    stateLabel.color = unlocked
                        ? new Color(0.78f, 0.95f, 1f)
                        : new Color(0.72f, 0.74f, 0.80f);
                }
            }

            if (progressLabel != null)
            {
                progressLabel.text = total == 0
                    ? "Нет доступных уровней"
                    : $"Открыто: {unlockedCount} из {total}";
            }
        }

        private void BuildLevelButtons()
        {
            if (levelButtonContainer == null || levelButtonPrefab == null)
            {
                return;
            }

            foreach (var button in _levelButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            _levelButtons.Clear();

            int count = GameFlow.Controller?.LevelCount ?? 0;
            for (int i = 0; i < count; i++)
            {
                int levelIndex = i;
                var button = Instantiate(levelButtonPrefab, levelButtonContainer);
                button.gameObject.SetActive(true);
                button.onClick.AddListener(() => SelectLevel(levelIndex));
                _levelButtons.Add(button);
            }

            levelButtonPrefab.gameObject.SetActive(false);
        }

        private void SelectLevel(int levelIndex)
        {
            var controller = GameFlow.Controller;
            if (controller == null || !controller.IsLevelUnlocked(levelIndex))
            {
                return;
            }

            controller.StartLevelFromMap(levelIndex);
        }

        private void OpenMap()
        {
            if (GameFlow.Controller != null)
            {
                GameFlow.Controller.ReturnToLevelMap();
            }
            else
            {
                ShowMap();
            }
        }

        private void OnDestroy()
        {
            if (openMapButton != null)
            {
                openMapButton.onClick.RemoveListener(OpenMap);
            }
        }
    }
}
