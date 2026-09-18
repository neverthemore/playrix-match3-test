using System.Collections;
using Match3.Core.Domain;
using Match3.Core.Events;
using Match3.Gameplay;
using Match3.Gameplay.Blocks;
using Match3.Gameplay.Board;
using Match3.UI;
using UnityEngine;

namespace Match3.App
{
   
    public class GameManager : MonoBehaviour, IGameFlowController
    {
        [Header("Data / Данные")]
        [SerializeField, Tooltip("EN: All levels, in play order. / RU: Все уровни в порядке прохождения.")]
        private LevelDatabase levelDatabase;

        [SerializeField, Tooltip("EN: Initial map focus (0 = first). / RU: Начальный выбранный уровень на карте (0 = первый).")]
        private int startLevelIndex;

        [SerializeField, Tooltip("EN: Fixed RNG seed for reproducible boards; -1 = random. / RU: Фиксированное зерно ГСЧ для воспроизводимых полей; -1 = случайное.")]
        private int randomSeed = -1;

        [Header("Scene references / Ссылки на сцену")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private HudController hudController;
        [SerializeField] private LevelResultScreen resultScreen;
        [SerializeField] private LevelMapController levelMapController;

        public EventBus EventBus { get; private set; }
        public BoardController BoardController { get; private set; }
        public GameSession GameSession { get; private set; }

        public int CurrentLevelIndex { get; private set; }
        public int LevelCount => levelDatabase != null ? levelDatabase.Count : 0;
        public bool IsInLevel { get; private set; }

        private const string ProgressKey = "Match3.HighestUnlockedLevel";
        private int _highestUnlockedLevel;

     
        private BlockBehaviourRegistry _blockRegistry;

        private bool _levelEnding;

        private void Awake()
        {
            EventBus = new EventBus();
            _blockRegistry = new BlockBehaviourRegistry();
   
            GameFlow.Register(this);
        }

        private void OnDestroy()
        {
            GameFlow.Unregister(this);
            CleanupLevel();
        }

        private void Start()
        {
            if (levelDatabase == null || levelDatabase.Count == 0)
            {
                Debug.LogError("[GameManager] No LevelDatabase assigned - nothing to play.");
                return;
            }

            LoadProgress();
            CurrentLevelIndex = Mathf.Clamp(startLevelIndex, 0, levelDatabase.Count - 1);

            if (levelMapController != null)
            {
                levelMapController.Initialize();
            }
            else
            {
                StartLevel(CurrentLevelIndex);
            }
        }


        private void LoadProgress()
        {
            int stored = PlayerPrefs.GetInt(ProgressKey, 0);
            _highestUnlockedLevel = Mathf.Clamp(stored, 0, Mathf.Max(0, LevelCount - 1));
        }

        public bool IsLevelUnlocked(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex < LevelCount && levelIndex <= _highestUnlockedLevel;
        }

        private void UnlockAfterCompletion(int completedLevel)
        {
            if (LevelCount == 0) return;

            int newlyUnlocked = Mathf.Clamp(completedLevel + 1, 0, LevelCount - 1);
            if (newlyUnlocked <= _highestUnlockedLevel)
            {
                return;
            }

            _highestUnlockedLevel = newlyUnlocked;
            PlayerPrefs.SetInt(ProgressKey, _highestUnlockedLevel);
            PlayerPrefs.Save();
            levelMapController?.RefreshProgress();
        }


        private void StartLevel(int levelIndex)
        {
            if (levelDatabase == null || levelDatabase.Count == 0)
            {
                return;
            }

            CleanupLevel();

            var config = levelDatabase.GetLevel(levelIndex);
            if (config == null)
            {
                Debug.LogError($"[GameManager] Level {levelIndex} not found in the database.");
                return;
            }

            CurrentLevelIndex = Mathf.Clamp(levelIndex, 0, levelDatabase.Count - 1);
            _levelEnding = false;
            IsInLevel = true;
            levelMapController?.HideMap();

            BoardController = new BoardController(EventBus, _blockRegistry, randomSeed);
            BoardController.InitializeLevel(config);

            GameSession = new GameSession(EventBus);

            BoardController.GoalWeightsProvider = () => GameSession.RemainingGoals;

            EventBus.Subscribe<LevelWonEvent>(OnLevelWon);
            EventBus.Subscribe<LevelLostEvent>(OnLevelLost);

            if (boardView != null)
            {
                boardView.gameObject.SetActive(true);
                boardView.Setup(BoardController, EventBus);
            }

            if (hudController != null)
            {
                hudController.gameObject.SetActive(true);
                var hudConfig = config;
                hudController.Setup(EventBus, hudConfig, CurrentLevelIndex);
            }

            if (resultScreen != null)
            {
                resultScreen.Hide();
            }

            GameSession.StartLevel(config, CurrentLevelIndex);
        }

        private void CleanupLevel()
        {
            if (EventBus != null)
            {
                EventBus.Unsubscribe<LevelWonEvent>(OnLevelWon);
                EventBus.Unsubscribe<LevelLostEvent>(OnLevelLost);
            }

            GameSession?.Shutdown();
            GameSession = null;

            if (BoardController != null)
            {
                BoardController.GoalWeightsProvider = null;
                BoardController = null;
            }

            if (boardView != null)
            {
                boardView.Teardown();
                boardView.gameObject.SetActive(false);
            }

            if (hudController != null)
            {
                hudController.Shutdown();
                hudController.gameObject.SetActive(false);
            }

            IsInLevel = false;
        }

        private void OnLevelWon(LevelWonEvent evt)
        {
            if (_levelEnding) return;
            _levelEnding = true;
            UnlockAfterCompletion(evt.LevelIndex);
            StartCoroutine(ShowResultWhenBoardSettles(won: true));
        }

        private void OnLevelLost(LevelLostEvent evt)
        {
            if (_levelEnding) return;
            _levelEnding = true;
            StartCoroutine(ShowResultWhenBoardSettles(won: false));
        }


        private IEnumerator ShowResultWhenBoardSettles(bool won)
        {
            while (BoardController != null && !BoardController.IsAcceptingInput)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.45f);

            int finishedLevel = CurrentLevelIndex;

            if (resultScreen != null)
            {
                resultScreen.Show(won, finishedLevel, () => AdvanceAfterResult(won));
            }
            else
            {
                AdvanceAfterResult(won);
            }
        }

        private void AdvanceAfterResult(bool won)
        {
            int nextIndex = won ? levelDatabase.GetNextIndex(CurrentLevelIndex) : CurrentLevelIndex;
            StartLevel(nextIndex);
        }


        public void StartLevelFromMap(int levelIndex)
        {
            if (!IsLevelUnlocked(levelIndex))
            {
                return;
            }

            StopAllCoroutines();
            resultScreen?.Hide();
            StartLevel(levelIndex);
        }

        public void ReturnToLevelMap()
        {
            StopAllCoroutines();
            resultScreen?.Hide();
            CleanupLevel();
            levelMapController?.ShowMap();
        }

        public void RestartLevel()
        {
            StartLevel(CurrentLevelIndex);
        }

        public void CheatWinLevel()
        {
            GameSession?.CheatForceWin();
        }

        public void CheatLoseLevel()
        {
            GameSession?.CheatForceLose();
        }

        public void CheatGoToLevel(int levelIndex)
        {
            if (levelDatabase == null || levelDatabase.Count == 0) return;

            if (resultScreen != null)
            {
                resultScreen.Hide();
            }

            StopAllCoroutines();
            StartLevel(Mathf.Clamp(levelIndex, 0, levelDatabase.Count - 1));
        }
    }
}
