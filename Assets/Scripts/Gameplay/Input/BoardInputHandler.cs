using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Match3.Gameplay.Input
{
    public class BoardInputHandler : MonoBehaviour, ISwapInputSource
    {
        [SerializeField, Tooltip("EN: Camera viewing the board. / RU: Камера, смотрящая на поле.")]
        private Camera targetCamera;

        [SerializeField, Tooltip("EN: Drag distance (in cells) that fires a swap. / RU: Дистанция перетаскивания (в клетках), вызывающая обмен.")]
        private float dragThresholdInCells = 0.35f;

        public event Action<Vector2Int, Vector2Int> SwapRequested;

        public event Action<Vector2Int?> SelectionChanged;

        private Func<Vector3, Vector2Int?> _worldToBoard;
        private Func<Vector2Int, Vector3> _boardToWorld;

        private bool _inputEnabled = true;
        private bool _pointerDown;
        private bool _swapFiredThisPress;
        private Vector2Int _pressedCell;
        private Vector3 _pressedWorldPos;
        private Vector2Int? _selected;

        public void Configure(Func<Vector3, Vector2Int?> worldToBoard, Func<Vector2Int, Vector3> boardToWorld)
        {
            _worldToBoard = worldToBoard;
            _boardToWorld = boardToWorld;
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                _pointerDown = false;
                ClearSelection();
            }
        }

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (!_inputEnabled || _worldToBoard == null) return;

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUi()) return;
                HandlePointerDown(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButton(0) && _pointerDown && !_swapFiredThisPress)
            {
                HandlePointerDrag(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0) && _pointerDown)
            {
                HandlePointerUp(UnityEngine.Input.mousePosition);
            }
        }

        private bool IsPointerOverUi()
        {
            if (EventSystem.current == null) return false;

            if (UnityEngine.Input.touchCount > 0)
            {
                return EventSystem.current.IsPointerOverGameObject(UnityEngine.Input.GetTouch(0).fingerId);
            }
            return EventSystem.current.IsPointerOverGameObject();
        }

        private void HandlePointerDown(Vector3 screenPos)
        {
            var worldPos = ScreenToWorld(screenPos);
            var cell = _worldToBoard(worldPos);
            if (!cell.HasValue) return;

            _pointerDown = true;
            _swapFiredThisPress = false;
            _pressedCell = cell.Value;
            _pressedWorldPos = worldPos;
        }

        private void HandlePointerDrag(Vector3 screenPos)
        {
            var worldPos = ScreenToWorld(screenPos);
            var delta = worldPos - _pressedWorldPos;

            float threshold = GetCellSize() * dragThresholdInCells;
            if (delta.magnitude < threshold) return;

            var direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? new Vector2Int(delta.x > 0f ? 1 : -1, 0)
                : new Vector2Int(0, delta.y > 0f ? 1 : -1);

            _swapFiredThisPress = true;
            ClearSelection();
            SwapRequested?.Invoke(_pressedCell, _pressedCell + direction);
        }

        private void HandlePointerUp(Vector3 screenPos)
        {
            _pointerDown = false;
            if (_swapFiredThisPress) return;

            var cell = _worldToBoard(ScreenToWorld(screenPos));
            if (!cell.HasValue)
            {
                ClearSelection();
                return;
            }

            if (!_selected.HasValue)
            {
                SetSelection(cell.Value);
                return;
            }

            if (_selected.Value == cell.Value)
            {
                ClearSelection();
                return;
            }

            var from = _selected.Value;
            var delta = cell.Value - from;

            if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 1)
            {
                ClearSelection();
                SwapRequested?.Invoke(from, cell.Value);
            }
            else
            {
                SetSelection(cell.Value);
            }
        }

        private void SetSelection(Vector2Int pos)
        {
            _selected = pos;
            SelectionChanged?.Invoke(pos);
        }

        private void ClearSelection()
        {
            if (!_selected.HasValue) return;
            _selected = null;
            SelectionChanged?.Invoke(null);
        }

        private Vector3 ScreenToWorld(Vector3 screenPos)
        {
            if (targetCamera == null) return Vector3.zero;

            screenPos.z = -targetCamera.transform.position.z;
            var world = targetCamera.ScreenToWorldPoint(screenPos);
            world.z = 0f;
            return world;
        }

        private float GetCellSize()
        {
            if (_boardToWorld == null) return 1f;
            var origin = _boardToWorld(Vector2Int.zero);
            var neighbor = _boardToWorld(new Vector2Int(1, 0));
            float size = Vector3.Distance(origin, neighbor);
            return size > 0.0001f ? size : 1f;
        }
    }
}
