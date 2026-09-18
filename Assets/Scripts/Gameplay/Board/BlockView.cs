using DG.Tweening;
using Match3.Core.Domain;
using Match3.Gameplay.Blocks;
using Match3.Gameplay.Tiles;
using UnityEngine;

namespace Match3.Gameplay.Board
{
    public class BlockView : MonoBehaviour
    {
        [Header("Renderers / Рендереры")]
        [SerializeField, Tooltip("EN: The box body. / RU: Тело ящика.")]
        private SpriteRenderer baseSprite;

        [SerializeField, Tooltip("EN: Color swatch for colored / chameleon crates. / RU: Цветная метка для цветного ящика и хамелеона.")]
        private SpriteRenderer colorSwatch;

        [SerializeField, Tooltip("EN: Crack overlay shown as lives are lost. / RU: Трещины, проявляющиеся по мере потери жизней.")]
        private SpriteRenderer crackSprite;

        public Vector2Int BoardPosition { get; private set; }

        private TileDatabase _database;
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void Initialize(BlockInstance block, Vector2Int boardPosition, TileDatabase database)
        {
            _database = database;
            BoardPosition = boardPosition;
            transform.localScale = _baseScale;
            Refresh(block);
        }

        public void Refresh(BlockInstance block)
        {
            if (block == null || _database == null) return;

            baseSprite.sprite = _database.GetBlockSprite(block.Kind);
            baseSprite.color = Color.white;

            bool showsColor = block.Kind == BlockKind.ColoredCrate || block.Kind == BlockKind.ChameleonCrate;
            if (colorSwatch != null)
            {
                colorSwatch.enabled = showsColor;
                if (showsColor)
                {
                    colorSwatch.color = _database.GetTint(block.Color);
                }
            }

            if (crackSprite != null)
            {
                crackSprite.sprite = _database.CrackOverlay;
                float damageRatio = block.MaxLives <= 1
                    ? 0f
                    : 1f - ((block.Lives - 1f) / (block.MaxLives - 1f));

                crackSprite.enabled = damageRatio > 0f;
                crackSprite.color = new Color(1f, 1f, 1f, damageRatio * 0.9f);
            }
        }

        public Tween AnimateDamage(float duration)
        {
            transform.DOKill();
            transform.localScale = _baseScale;

            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOShakePosition(duration, 0.12f, 18, 90f, false, true));
            sequence.Join(transform.DOPunchScale(_baseScale * 0.18f, duration, 6, 0.7f));
            return sequence;
        }

        public Tween AnimateDestroy(float duration)
        {
            transform.DOKill();

            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(_baseScale * 1.3f, duration * 0.25f).SetEase(Ease.OutQuad));
            sequence.Append(transform.DOScale(Vector3.zero, duration * 0.75f).SetEase(Ease.InBack, 2f));
            sequence.Join(transform.DORotate(new Vector3(0f, 0f, Random.Range(-90f, 90f)), duration));
            sequence.Join(baseSprite.DOColor(new Color(1f, 1f, 1f, 0f), duration * 0.75f));

            if (colorSwatch != null && colorSwatch.enabled)
            {
                var target = colorSwatch.color;
                sequence.Join(colorSwatch.DOColor(new Color(target.r, target.g, target.b, 0f), duration * 0.75f));
            }
            if (crackSprite != null && crackSprite.enabled)
            {
                var target = crackSprite.color;
                sequence.Join(crackSprite.DOColor(new Color(target.r, target.g, target.b, 0f), duration * 0.75f));
            }

            return sequence;
        }

        public Tween AnimateColorChange(float duration)
        {
            transform.DOKill();
            transform.localScale = _baseScale;
            return transform.DOPunchRotation(new Vector3(0f, 0f, 14f), duration, 8, 0.9f);
        }

        private void OnDestroy()
        {
            transform.DOKill();
            if (baseSprite != null) baseSprite.DOKill();
            if (colorSwatch != null) colorSwatch.DOKill();
            if (crackSprite != null) crackSprite.DOKill();
        }
    }
}
