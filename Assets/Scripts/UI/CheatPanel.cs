using System.Collections.Generic;
using Match3.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.UI
{
    public class CheatPanel : MonoBehaviour
    {
        [Header("Toggle / Переключатель")]
        [SerializeField, Tooltip("EN: Opens and closes the panel body. / RU: Открывает и закрывает тело панели.")]
        private Button toggleButton;

        [SerializeField] private GameObject panelBody;

        [Header("Actions / Действия")]
        [SerializeField] private Button winButton;
        [SerializeField] private Button loseButton;

        [Header("Level jump / Переход к уровню")]
        [SerializeField, Tooltip("EN: Parent for the generated level buttons. / RU: Родитель для сгенерированных кнопок уровней.")]
        private Transform levelButtonContainer;

        [SerializeField, Tooltip("EN: Template for one level button. / RU: Шаблон одной кнопки уровня.")]
        private Button levelButtonPrefab;

        [SerializeField] private TextMeshProUGUI currentLevelLabel;

        private readonly List<Button> _levelButtons = new List<Button>();
        private bool _isOpen;

        private void Awake()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(Toggle);
            }

            if (winButton != null)
            {
                winButton.onClick.AddListener(() => GameFlow.Controller?.CheatWinLevel());
            }

            if (loseButton != null)
            {
                loseButton.onClick.AddListener(() => GameFlow.Controller?.CheatLoseLevel());
            }

            SetOpen(false);
        }

        private void Start()
        {
            BuildLevelButtons();
        }

        private void BuildLevelButtons()
        {
            if (levelButtonContainer == null || levelButtonPrefab == null) return;

            foreach (var button in _levelButtons)
            {
                if (button != null) Destroy(button.gameObject);
            }
            _levelButtons.Clear();

            int count = GameFlow.Controller?.LevelCount ?? 0;
            for (int i = 0; i < count; i++)
            {
                int levelIndex = i;

                var button = Instantiate(levelButtonPrefab, levelButtonContainer);
                button.gameObject.SetActive(true);

                var label = button.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = (levelIndex + 1).ToString();
                }

                button.onClick.AddListener(() =>
                {
                    GameFlow.Controller?.CheatGoToLevel(levelIndex);
                    RefreshCurrentLevelLabel();
                });

                _levelButtons.Add(button);
            }

            levelButtonPrefab.gameObject.SetActive(false);

            RefreshCurrentLevelLabel();
        }

        private void Toggle()
        {
            SetOpen(!_isOpen);
            if (_isOpen)
            {
                RefreshCurrentLevelLabel();
            }
        }

        private void SetOpen(bool open)
        {
            _isOpen = open;
            if (panelBody != null)
            {
                panelBody.SetActive(open);
            }
        }

        private void RefreshCurrentLevelLabel()
        {
            if (currentLevelLabel == null) return;

            int current = GameFlow.Controller?.CurrentLevelIndex ?? 0;
            int total = GameFlow.Controller?.LevelCount ?? 0;
            currentLevelLabel.text = $"Level {current + 1} / {total}";
        }
    }
}
