using DG.Tweening;
using Match3.Core.Domain;
using Match3.Gameplay.Tiles;
using UnityEngine;

namespace Match3.Gameplay.Board
{
    public class TileView : MonoBehaviour
    {
        [Header("Renderers / Рендереры")]
        [SerializeField, Tooltip("EN: The chip body. / RU: Тело фишки.")]
        private SpriteRenderer baseSprite;

        [SerializeField, Tooltip("EN: Booster mark drawn over the body. / RU: Знак бустера поверх тела.")]
        private SpriteRenderer overlaySprite;

        [SerializeField, Tooltip("EN: Ring shown while this chip is selected. / RU: Кольцо, пока фишка выделена.")]
        private SpriteRenderer selectionRing;

        public Tile Tile { get; private set; }
        public Vector2Int BoardPosition { get; private set; }

        private TileDatabase _database;

        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void Initialize(Tile tile, Vector2Int boardPosition, TileDatabase database)
        {
            _database = database;
            SetTile(tile, boardPosition);
            if (selectionRing != null)
            {
                selectionRing.enabled = false;
            }
        }

        public void SetTile(Tile tile, Vector2Int boardPosition)
        {
            Tile = tile;
            BoardPosition = boardPosition;
            RefreshVisuals();
        }

        public void SetBoardPosition(Vector2Int boardPosition)
        {
            BoardPosition = boardPosition;
        }

        public void RefreshVisuals()
        {
            if (Tile == null || _database == null) return;

            if (Tile.Booster == BoosterType.RainbowBall)
            {
                baseSprite.sprite = _database.RainbowBallSprite;
                baseSprite.color = Color.white;
            }
            else
            {
                var definition = _database.GetDefinition(Tile.Color);
                if (definition != null)
                {
                    baseSprite.sprite = definition.Sprite;
                    baseSprite.color = definition.Tint;
                }
            }

            if (overlaySprite != null)
            {
                var overlay = _database.GetBoosterOverlay(Tile.Booster);
                overlaySprite.sprite = overlay;
                overlaySprite.enabled = overlay != null;
                overlaySprite.color = Color.white;
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectionRing == null) return;

            selectionRing.enabled = selected;
            if (selected)
            {
                selectionRing.sprite = _database != null ? _database.HighlightSprite : selectionRing.sprite;
                selectionRing.transform.localScale = Vector3.one;
                selectionRing.transform.DOKill();
                selectionRing.transform
                    .DOScale(Vector3.one * 1.15f, 0.45f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
            else
            {
                selectionRing.transform.DOKill();
                selectionRing.transform.localScale = Vector3.one;
            }
        }


        public Tween AnimateSwapTo(Vector3 worldPosition, float duration)
        {
            transform.DOKill();
            return transform.DOMove(worldPosition, duration).SetEase(Ease.OutQuad);
        }

        public Tween AnimateRejectedSwap(Vector3 originalPosition, Vector3 direction, float distance, float duration)
        {
            transform.DOKill();
            transform.position = originalPosition;

            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOMove(originalPosition + direction * distance, duration * 0.4f).SetEase(Ease.OutQuad));
            sequence.Append(transform.DOMove(originalPosition, duration * 0.6f).SetEase(Ease.OutBack, 2f));
            return sequence;
        }

        public Tween AnimateFall(Vector3 worldPosition, float duration)
        {
            transform.DOKill();

            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOMove(worldPosition, duration).SetEase(Ease.InQuad));

            sequence.Append(transform.DOScale(new Vector3(_baseScale.x * 1.18f, _baseScale.y * 0.82f, _baseScale.z), 0.07f));
            sequence.Append(transform.DOScale(_baseScale, 0.12f).SetEase(Ease.OutBack, 3f));
            return sequence;
        }

        public Tween AnimateSpawnFall(Vector3 fromPosition, Vector3 toPosition, float duration)
        {
            transform.DOKill();
            transform.position = fromPosition;
            transform.localScale = _baseScale;
            ResetAlpha();
            return AnimateFall(toPosition, duration);
        }


        public Tween AnimateClear(float duration)
        {
            transform.DOKill();

            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(_baseScale * 1.25f, duration * 0.3f).SetEase(Ease.OutQuad));
            sequence.Append(transform.DOScale(Vector3.zero, duration * 0.7f).SetEase(Ease.InBack, 2f));

            sequence.Join(baseSprite.DOColor(WithAlpha(baseSprite.color, 0f), duration * 0.7f));

            if (overlaySprite != null && overlaySprite.enabled)
            {
                sequence.Join(overlaySprite.DOColor(WithAlpha(overlaySprite.color, 0f), duration * 0.7f));
            }

            return sequence;
        }

        public Tween AnimatePromotion()
        {
            transform.DOKill();
            transform.localScale = _baseScale;
            return transform.DOPunchScale(_baseScale * 0.45f, 0.45f, 8, 0.8f);
        }

        public Tween AnimateBoosterActivation(float duration)
        {
            transform.DOKill();

            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(_baseScale * 0.75f, duration * 0.35f).SetEase(Ease.InQuad));
            sequence.Append(transform.DOScale(_baseScale * 1.45f, duration * 0.35f).SetEase(Ease.OutQuad));
            sequence.Join(baseSprite.DOColor(Color.white, duration * 0.35f));
            return sequence;
        }

        public void PlayHint()
        {
            transform.DOKill();
            transform.localScale = _baseScale;
            transform.DOScale(_baseScale * 1.15f, 0.32f)
                .SetLoops(6, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        public void StopHint()
        {
            transform.DOKill();
            transform.localScale = _baseScale;
        }

        public void ResetVisualState()
        {
            transform.DOKill();
            transform.localScale = _baseScale;
            transform.rotation = Quaternion.identity;
            ResetAlpha();
            SetSelected(false);
        }

        private void ResetAlpha()
        {
            if (baseSprite != null)
            {
                baseSprite.color = WithAlpha(baseSprite.color, 1f);
            }
            if (overlaySprite != null)
            {
                overlaySprite.color = WithAlpha(overlaySprite.color, 1f);
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        private void OnDestroy()
        {
            transform.DOKill();
            if (baseSprite != null) baseSprite.DOKill();
            if (overlaySprite != null) overlaySprite.DOKill();
            if (selectionRing != null) selectionRing.transform.DOKill();
        }
    }
}
