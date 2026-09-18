using System.Collections.Generic;
using Match3.Core.Domain;
using UnityEngine;

namespace Match3.Gameplay.Tiles
{
    [CreateAssetMenu(fileName = "TileDatabase", menuName = "Match3/Tile Database")]
    public class TileDatabase : ScriptableObject
    {
        [Header("Chips / Фишки")]
        [SerializeField, Tooltip("EN: One definition per color. / RU: По одному определению на цвет.")]
        private List<TileDefinition> definitions = new List<TileDefinition>();

        [Header("Booster overlays / Наложения бустеров")]
        [Tooltip("EN: Drawn ON TOP of the colored chip, so a booster keeps showing its color. / RU: Рисуется ПОВЕРХ цветной фишки, чтобы бустер сохранял свой цвет.")]
        [SerializeField] private Sprite lineHorizontalOverlay;
        [SerializeField] private Sprite lineVerticalOverlay;
        [SerializeField] private Sprite bombOverlay;
        [SerializeField] private Sprite planeOverlay;

        [Tooltip("EN: The rainbow ball REPLACES the chip sprite - it is colorless. / RU: Радужный шар ЗАМЕНЯЕТ спрайт фишки - он бесцветен.")]
        [SerializeField] private Sprite rainbowBallSprite;

        [Header("Blocks / Ящики")]
        [SerializeField] private Sprite crateSprite;
        [SerializeField] private Sprite coloredCrateSprite;
        [SerializeField] private Sprite chameleonCrateSprite;
        [SerializeField] private Sprite blockerSprite;

        [Tooltip("EN: Overlaid on a damaged multi-life box. / RU: Накладывается на повреждённый многожизненный ящик.")]
        [SerializeField] private Sprite crackOverlay;

        [Header("Board / Поле")]
        [SerializeField] private Sprite cellBackgroundSprite;

        [Tooltip("EN: Used for the selection ring and booster blast flashes. / RU: Используется для кольца выделения и вспышек взрывов.")]
        [SerializeField] private Sprite highlightSprite;

        private Dictionary<TileColor, TileDefinition> _lookup;

        public Sprite RainbowBallSprite => rainbowBallSprite;
        public Sprite CellBackgroundSprite => cellBackgroundSprite;
        public Sprite HighlightSprite => highlightSprite;
        public Sprite CrackOverlay => crackOverlay;

        public Sprite GetBoosterOverlay(BoosterType booster)
        {
            switch (booster)
            {
                case BoosterType.LineHorizontal: return lineHorizontalOverlay;
                case BoosterType.LineVertical: return lineVerticalOverlay;
                case BoosterType.Bomb: return bombOverlay;
                case BoosterType.Plane: return planeOverlay;
                default: return null;
            }
        }

        public Sprite GetBlockSprite(BlockKind kind)
        {
            switch (kind)
            {
                case BlockKind.Crate: return crateSprite;
                case BlockKind.ColoredCrate: return coloredCrateSprite;
                case BlockKind.ChameleonCrate: return chameleonCrateSprite;
                case BlockKind.Blocker: return blockerSprite;
                default: return null;
            }
        }

        public TileDefinition GetDefinition(TileColor color)
        {
            BuildLookupIfNeeded();
            return _lookup.TryGetValue(color, out var definition) ? definition : null;
        }

        public Color GetTint(TileColor color)
        {
            var definition = GetDefinition(color);
            return definition != null ? definition.SwatchTint : Color.white;
        }

        private void BuildLookupIfNeeded()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<TileColor, TileDefinition>();
            foreach (var definition in definitions)
            {
                if (definition == null) continue;
                if (!_lookup.ContainsKey(definition.TileColorValue))
                {
                    _lookup.Add(definition.TileColorValue, definition);
                }
                else
                {
                    Debug.LogWarning($"[TileDatabase] Duplicate definition for {definition.TileColorValue} - ignored.");
                }
            }
        }

        private void OnValidate()
        {
            _lookup = null;
        }

#if UNITY_EDITOR
        public void EditorConfigure(List<TileDefinition> defs, Sprite lineH, Sprite lineV, Sprite bomb,
            Sprite plane, Sprite rainbow, Sprite crate, Sprite colored, Sprite chameleon, Sprite blocker,
            Sprite crack, Sprite cellBg, Sprite highlight)
        {
            definitions = defs;
            lineHorizontalOverlay = lineH;
            lineVerticalOverlay = lineV;
            bombOverlay = bomb;
            planeOverlay = plane;
            rainbowBallSprite = rainbow;
            crateSprite = crate;
            coloredCrateSprite = colored;
            chameleonCrateSprite = chameleon;
            blockerSprite = blocker;
            crackOverlay = crack;
            cellBackgroundSprite = cellBg;
            highlightSprite = highlight;
            _lookup = null;
        }
#endif
    }
}
