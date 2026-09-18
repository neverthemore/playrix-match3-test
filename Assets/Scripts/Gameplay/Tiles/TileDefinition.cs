using Match3.Core.Domain;
using UnityEngine;

namespace Match3.Gameplay.Tiles
{
    [CreateAssetMenu(fileName = "TileDef", menuName = "Match3/Tile Definition")]
    public class TileDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("EN: Which chip color this describes. / RU: Какой цвет фишки описывается.")]
        private TileColor color = TileColor.Red;

        [SerializeField, Tooltip("EN: REPLACE THIS to change the chip's art. / RU: ЗАМЕНИТЕ ЭТО, чтобы сменить арт фишки.")]
        private Sprite sprite;

        [SerializeField, Tooltip("EN: Multiplied over the sprite; keep white for pre-colored art. / RU: Умножается на спрайт; для готового цветного арта оставьте белым.")]
        private Color tint = Color.white;

        [SerializeField, Tooltip("EN: Semantic color used by UI swatches when the chip sprite itself is not drawn. / RU: Цвет для UI-меток там, где сам спрайт фишки не рисуется.")]
        private Color swatchTint = Color.white;

        public TileColor TileColorValue => color;
        public Sprite Sprite => sprite;
        public Color Tint => tint;
        public Color SwatchTint => swatchTint;

#if UNITY_EDITOR
        public void EditorConfigure(TileColor color, Sprite sprite, Color tint, Color swatchTint)
        {
            this.color = color;
            this.sprite = sprite;
            this.tint = tint;
            this.swatchTint = swatchTint;
        }
#endif
    }
}
