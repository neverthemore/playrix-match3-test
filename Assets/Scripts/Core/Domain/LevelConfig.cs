using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3.Core.Domain
{
    public enum CellContent
    {
        RandomChip = 0,
        FixedChip = 1,
        Booster = 2,
        Block = 3,
        Hole = 4,
    }

    [Serializable]
    public class LevelCell
    {
        [Tooltip("EN: Column, 0 = left. / RU: Столбец, 0 = слева.")]
        public int x;

        [Tooltip("EN: Row, 0 = BOTTOM. / RU: Строка, 0 = СНИЗУ.")]
        public int y;

        [Tooltip("EN: What sits in this cell. / RU: Что находится в этой клетке.")]
        public CellContent content = CellContent.RandomChip;

        [Tooltip("EN: Chip color when content = FixedChip. / RU: Цвет фишки при content = FixedChip.")]
        public TileColor chipColor = TileColor.Red;

        [Tooltip("EN: Booster when content = Booster. / RU: Бустер при content = Booster.")]
        public BoosterType booster = BoosterType.Bomb;

        [Tooltip("EN: Box layers, outermost first, when content = Block. / RU: Слои ящика (внешний первым) при content = Block.")]
        public List<BlockLayerData> blockLayers = new List<BlockLayerData>();
    }


    [CreateAssetMenu(fileName = "Level_00", menuName = "Match3/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Identity / Идентификация")]
        [SerializeField, Tooltip("EN: Shown in the HUD. / RU: Отображается в HUD.")]
        private string displayName = "Level";

        [Header("Board / Поле")]
        [SerializeField, Range(4, 12), Tooltip("EN: Board width in cells. / RU: Ширина поля в клетках.")]
        private int width = 8;

        [SerializeField, Range(4, 12), Tooltip("EN: Board height in cells. / RU: Высота поля в клетках.")]
        private int height = 8;

        [SerializeField, Range(3, 6), Tooltip("EN: How many of the colors below are actually used. / RU: Сколько цветов из списка ниже реально используется.")]
        private int colorCount = 5;

        [SerializeField, Tooltip("EN: Color palette; the first 'colorCount' entries are used. / RU: Палитра; используются первые 'colorCount' записей.")]
        private List<TileColor> colorPalette = new List<TileColor>
        {
            TileColor.Red,
            TileColor.Blue,
            TileColor.Green,
            TileColor.Yellow,
            TileColor.Purple,
            TileColor.Orange
        };

        [Header("Map / Карта уровня")]
        [SerializeField, Tooltip("EN: Only the non-default cells need listing. / RU: Перечислять нужно только нестандартные клетки.")]
        private List<LevelCell> cells = new List<LevelCell>();

        [Header("Rules / Правила")]
        [SerializeField, Min(1), Tooltip("EN: Move limit for this level. / RU: Лимит ходов на уровне.")]
        private int moveLimit = 25;

        [SerializeField, Tooltip("EN: All goals must be met to win. / RU: Для победы нужно выполнить все цели.")]
        private List<LevelGoal> goals = new List<LevelGoal>();


        public string DisplayName => displayName;
        public int Width => width;
        public int Height => height;
        public int MoveLimit => moveLimit;
        public IReadOnlyList<LevelCell> Cells => cells;
        public IReadOnlyList<LevelGoal> Goals => goals;


        public List<TileColor> GetActiveColors()
        {
            var result = new List<TileColor>(colorCount);
            foreach (var color in colorPalette)
            {
                if (result.Count >= colorCount) break;
                if (color == TileColor.None || result.Contains(color)) continue;
                result.Add(color);
            }
            if (result.Count < 3)
            {
                result.Clear();
                result.Add(TileColor.Red);
                result.Add(TileColor.Blue);
                result.Add(TileColor.Green);
            }
            return result;
        }

        public LevelCell FindCell(int x, int y)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].x == x && cells[i].y == y)
                {
                    return cells[i];
                }
            }
            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (goals == null) return;

            var activeColors = GetActiveColors();
            foreach (var goal in goals)
            {
                if (goal == null || goal.kind != GoalKind.CollectColor) continue;
                if (activeColors.Contains(goal.color)) continue;

                Debug.LogError(
                    $"[LevelConfig] '{name}' requires {goal.color}, but that color is not " +
                    $"active in its {colorCount}-color palette. The goal cannot be completed.",
                    this);
            }
        }

        public void EditorConfigure(string displayName, int width, int height, int colorCount,
            int moveLimit, List<LevelGoal> goals, List<LevelCell> cells,
            List<TileColor> colorPalette = null)
        {
            this.displayName = displayName;
            this.width = width;
            this.height = height;
            this.colorCount = colorCount;
            this.moveLimit = moveLimit;
            this.goals = goals ?? new List<LevelGoal>();
            this.cells = cells ?? new List<LevelCell>();

            this.colorPalette = colorPalette != null
                ? new List<TileColor>(colorPalette)
                : new List<TileColor>
                {
                    TileColor.Red,
                    TileColor.Blue,
                    TileColor.Green,
                    TileColor.Yellow,
                    TileColor.Purple,
                    TileColor.Orange
                };
        }
#endif
    }
}
