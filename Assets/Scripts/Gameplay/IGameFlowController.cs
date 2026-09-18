namespace Match3.Gameplay
{
    public interface IGameFlowController
    {
        int CurrentLevelIndex { get; }

        int LevelCount { get; }

        bool IsInLevel { get; }

        bool IsLevelUnlocked(int levelIndex);

        void StartLevelFromMap(int levelIndex);

        void ReturnToLevelMap();

        void RestartLevel();


        void CheatWinLevel();

        void CheatLoseLevel();

        void CheatGoToLevel(int levelIndex);
    }
}
