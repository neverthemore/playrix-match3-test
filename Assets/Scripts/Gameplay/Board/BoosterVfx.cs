using System.Collections.Generic;
using DG.Tweening;
using Match3.Core.Domain;
using Match3.Gameplay.Tiles;
using UnityEngine;

namespace Match3.Gameplay.Board
{
    public class BoosterVfx : MonoBehaviour
    {
        [Header("Setup / Настройка")]
        [SerializeField, Tooltip("EN: Parent for spawned effect sprites. / RU: Родитель для создаваемых спрайтов эффектов.")]
        private Transform effectRoot;

        [SerializeField, Tooltip("EN: Sorting order for effects; must sit above chips. / RU: Порядок сортировки эффектов; должен быть выше фишек.")]
        private int sortingOrder = 50;

        [Header("Colors / Цвета")]
        [SerializeField] private Color lineColor = new Color(1f, 0.95f, 0.55f);
        [SerializeField] private Color bombColor = new Color(1f, 0.62f, 0.25f);
        [SerializeField] private Color rainbowColor = new Color(0.85f, 0.6f, 1f);
        [SerializeField] private Color planeColor = new Color(0.62f, 0.88f, 1f);
        [SerializeField] private Color comboColor = Color.white;

        private TileDatabase _database;
        private readonly Stack<SpriteRenderer> _pool = new Stack<SpriteRenderer>();

        public void Setup(TileDatabase database)
        {
            _database = database;
            if (effectRoot == null)
            {
                effectRoot = transform;
            }
        }

        public void PlayBooster(BoosterType booster, Vector2Int origin, Vector2Int target,
            IReadOnlyList<Vector2Int> affected, float cellSize, System.Func<Vector2Int, Vector3> toWorld, float duration)
        {
            switch (booster)
            {
                case BoosterType.LineHorizontal:
                case BoosterType.LineVertical:
                    PlayLine(booster == BoosterType.LineHorizontal, origin, affected, cellSize, toWorld, duration);
                    break;

                case BoosterType.Bomb:
                    PlayShockwave(toWorld(origin), cellSize * 5.2f, bombColor, duration);
                    break;

                case BoosterType.RainbowBall:
                    PlayRainbow(origin, affected, cellSize, toWorld, duration);
                    break;

                case BoosterType.Plane:
                    PlayPlane(origin, target, cellSize, toWorld, duration);
                    break;
            }
        }

        public void PlayCombo(Vector2Int center, IReadOnlyList<Vector2Int> affected,
            float cellSize, System.Func<Vector2Int, Vector3> toWorld, float duration)
        {
            PlayShockwave(toWorld(center), cellSize * 6f, comboColor, duration);

            int step = Mathf.Max(1, affected.Count / 24);
            for (int i = 0; i < affected.Count; i += step)
            {
                PlaySparkle(toWorld(affected[i]), cellSize * 0.8f, comboColor, duration * 0.8f);
            }
        }


        private void PlayLine(bool horizontal, Vector2Int origin, IReadOnlyList<Vector2Int> affected,
            float cellSize, System.Func<Vector2Int, Vector3> toWorld, float duration)
        {
            int min = int.MaxValue;
            int max = int.MinValue;
            foreach (var cell in affected)
            {
                int coordinate = horizontal ? cell.x : cell.y;
                if (horizontal && cell.y != origin.y) continue;
                if (!horizontal && cell.x != origin.x) continue;
                min = Mathf.Min(min, coordinate);
                max = Mathf.Max(max, coordinate);
            }
            if (min > max) return;

            var from = horizontal ? new Vector2Int(min, origin.y) : new Vector2Int(origin.x, min);
            var to = horizontal ? new Vector2Int(max, origin.y) : new Vector2Int(origin.x, max);

            var start = toWorld(from);
            var end = toWorld(to);
            var center = (start + end) * 0.5f;
            float length = Vector3.Distance(start, end) + cellSize;

            var renderer = Rent();
            renderer.transform.position = center;
            renderer.transform.rotation = Quaternion.identity;
            renderer.color = lineColor;

            var thickness = cellSize * 0.55f;
            var finalScale = horizontal
                ? new Vector3(length, thickness, 1f)
                : new Vector3(thickness, length, 1f);

            renderer.transform.localScale = horizontal
                ? new Vector3(0f, thickness, 1f)
                : new Vector3(thickness, 0f, 1f);

            var sequence = DOTween.Sequence();
            sequence.Append(renderer.transform.DOScale(finalScale, duration * 0.35f).SetEase(Ease.OutQuad));
            sequence.Append(renderer.DOColor(WithAlpha(lineColor, 0f), duration * 0.65f));
            sequence.OnComplete(() => Recycle(renderer));
        }

        private void PlayShockwave(Vector3 worldPosition, float diameter, Color color, float duration)
        {
            var renderer = Rent();
            renderer.transform.position = worldPosition;
            renderer.transform.rotation = Quaternion.identity;
            renderer.transform.localScale = Vector3.one * (diameter * 0.2f);
            renderer.color = color;

            var sequence = DOTween.Sequence();
            sequence.Append(renderer.transform.DOScale(Vector3.one * diameter, duration).SetEase(Ease.OutQuad));
            sequence.Join(renderer.DOColor(WithAlpha(color, 0f), duration).SetEase(Ease.InQuad));
            sequence.OnComplete(() => Recycle(renderer));
        }

        private void PlayRainbow(Vector2Int origin, IReadOnlyList<Vector2Int> affected,
            float cellSize, System.Func<Vector2Int, Vector3> toWorld, float duration)
        {
            var start = toWorld(origin);
            PlayShockwave(start, cellSize * 3f, rainbowColor, duration * 0.7f);

            foreach (var cell in affected)
            {
                if (cell == origin) continue;

                var renderer = Rent();
                renderer.transform.position = start;
                renderer.transform.rotation = Quaternion.identity;
                renderer.transform.localScale = Vector3.one * (cellSize * 0.35f);
                renderer.color = rainbowColor;

                var sequence = DOTween.Sequence();
                sequence.Append(renderer.transform.DOMove(toWorld(cell), duration * 0.8f).SetEase(Ease.InQuad));
                sequence.Join(renderer.DOColor(WithAlpha(rainbowColor, 0f), duration * 0.8f).SetEase(Ease.InQuad));
                sequence.OnComplete(() => Recycle(renderer));
            }
        }

        private void PlayPlane(Vector2Int origin, Vector2Int target,
            float cellSize, System.Func<Vector2Int, Vector3> toWorld, float duration)
        {
            var start = toWorld(origin);
            var end = toWorld(target);

            var renderer = Rent();
            renderer.transform.position = start;
            renderer.transform.localScale = Vector3.one * (cellSize * 0.7f);
            renderer.color = planeColor;

            var direction = end - start;
            float angle = direction.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
                : 0f;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            var sequence = DOTween.Sequence();
            sequence.Append(renderer.transform.DOMove(end, duration * 0.75f).SetEase(Ease.InOutQuad));
            sequence.AppendCallback(() => PlayShockwave(end, cellSize * 2.6f, planeColor, duration * 0.4f));
            sequence.Append(renderer.DOColor(WithAlpha(planeColor, 0f), duration * 0.25f));
            sequence.OnComplete(() => Recycle(renderer));
        }

        private void PlaySparkle(Vector3 worldPosition, float size, Color color, float duration)
        {
            var renderer = Rent();
            renderer.transform.position = worldPosition;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            renderer.transform.localScale = Vector3.one * size;
            renderer.color = color;

            var sequence = DOTween.Sequence();
            sequence.Append(renderer.transform.DOScale(Vector3.one * size * 1.8f, duration).SetEase(Ease.OutQuad));
            sequence.Join(renderer.DOColor(WithAlpha(color, 0f), duration));
            sequence.OnComplete(() => Recycle(renderer));
        }


        private SpriteRenderer Rent()
        {
            SpriteRenderer renderer;
            if (_pool.Count > 0)
            {
                renderer = _pool.Pop();
                renderer.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("BoosterVfx");
                go.transform.SetParent(effectRoot, false);
                renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = sortingOrder;
            }

            renderer.sprite = _database != null ? _database.HighlightSprite : null;
            return renderer;
        }

        private void Recycle(SpriteRenderer renderer)
        {
            if (renderer == null) return;

            renderer.transform.DOKill();
            renderer.DOKill();
            renderer.gameObject.SetActive(false);
            _pool.Push(renderer);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        private void OnDestroy()
        {
            foreach (var renderer in _pool)
            {
                if (renderer == null) continue;
                renderer.transform.DOKill();
                renderer.DOKill();
            }
            _pool.Clear();
        }
    }
}
