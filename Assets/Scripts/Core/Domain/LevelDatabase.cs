using System.Collections.Generic;
using UnityEngine;

namespace Match3.Core.Domain
{
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "Match3/Level Database")]
    public class LevelDatabase : ScriptableObject
    {
        [SerializeField, Tooltip("EN: Levels in play order. / RU: Уровни в порядке прохождения.")]
        private List<LevelConfig> levels = new List<LevelConfig>();

        public IReadOnlyList<LevelConfig> Levels => levels;
        public int Count => levels.Count;

        public LevelConfig GetLevel(int index)
        {
            if (levels.Count == 0) return null;
            return levels[Mathf.Clamp(index, 0, levels.Count - 1)];
        }

        public int GetNextIndex(int currentIndex)
        {
            if (levels.Count == 0) return 0;
            return (currentIndex + 1) % levels.Count;
        }

#if UNITY_EDITOR
        public void EditorSetLevels(List<LevelConfig> newLevels)
        {
            levels = newLevels ?? new List<LevelConfig>();
        }
#endif
    }
}
