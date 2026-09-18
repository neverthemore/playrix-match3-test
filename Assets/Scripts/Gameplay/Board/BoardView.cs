using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Match3.Core.Domain;
using Match3.Core.Events;
using Match3.Gameplay.Blocks;
using Match3.Gameplay.Input;
using Match3.Gameplay.Tiles;
using Match3.Utils;
using UnityEngine;

namespace Match3.Gameplay.Board
{
    public class BoardView : MonoBehaviour, IBoardTimings
    {
        [Header("Prefabs / Префабы")]
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private BlockView blockPrefab;
        [SerializeField] private SpriteRenderer cellBackgroundPrefab;

        [Header("References / Ссылки")]
        [SerializeField] private TileDatabase tileDatabase;
        [SerializeField] private BoardInputHandler inputHandler;
        [SerializeField] private BoosterVfx boosterVfx;
        [SerializeField] private Transform boardRoot;
        [SerializeField] private Camera boardCamera;

        [Header("Layout / Раскладка")]
        [SerializeField, Tooltip("EN: World size of one cell. / RU: Мировой размер одной клетки.")]
        private float cellSize = 1f;

        [SerializeField, Tooltip("EN: Fit the board to the camera automatically. / RU: Автоматически вписывать поле в камеру.")]
        private bool autoFitCamera = true;

        [SerializeField, Tooltip("EN: Free space kept around the board when auto-fitting, in cells. / RU: Свободное место вокруг поля при авто-вписывании, в клетках.")]
        private float cameraPadding = 1.6f;

        [Header("Timing / Тайминги")]
        [SerializeField] private float swapDuration = 0.18f;
        [SerializeField] private float clearDuration = 0.26f;

        [SerializeField, Tooltip("EN: Fall time per cell of distance. / RU: Время падения на одну клетку расстояния.")]
        private float fallDurationPerCell = 0.065f;

        [SerializeField, Tooltip("EN: Shortest possible fall. / RU: Минимально возможная длительность падения.")]
        private float minFallDuration = 0.16f;

        [SerializeField, Tooltip("EN: Longest possible fall, however far a chip drops. / RU: Максимальная длительность падения, как бы далеко фишка ни летела.")]
        private float maxFallDuration = 0.5f;

        [SerializeField, Tooltip("EN: Extra beat after chips land, so cascades stay readable. / RU: Дополнительная пауза после приземления, чтобы каскады читались.")]
        private float settleDelay = 0.06f;

        [SerializeField] private float boosterDuration = 0.3f;
        [SerializeField] private float rainbowDuration = 0.42f;
        [SerializeField] private float planeDuration = 0.5f;

        [Header("Hints / Подсказки")]
        [SerializeField, Tooltip("EN: Idle seconds before a move is suggested. / RU: Секунд простоя до показа подсказки.")]
        private float hintIdleSeconds = 5f;

        private BoardController _controller;
        private EventBus _eventBus;

        private TileView[,] _tileViews;

        private BlockView[,] _blockViews;

        private ComponentPool<TileView> _tilePool;

        private Vector2Int? _selected;
        private float _idleTimer;
        private Vector2Int? _hintA;
        private Vector2Int? _hintB;
        private readonly MatchFinder _hintFinder = new MatchFinder();


        public void Setup(BoardController controller, EventBus eventBus)
        {
            Teardown();

            _controller = controller;
            _eventBus = eventBus;

            var board = controller.Board;
            _tileViews = new TileView[board.Width, board.Height];
            _blockViews = new BlockView[board.Width, board.Height];

            if (boardRoot == null) boardRoot = transform;
            _tilePool = new ComponentPool<TileView>(tilePrefab, boardRoot, board.Width * board.Height);

            if (boosterVfx != null) boosterVfx.Setup(tileDatabase);

            _controller.Timings = this;
            _controller.OnGravityApplied += OnGravityApplied;

            _eventBus.Subscribe<TileSwappedEvent>(OnTileSwapped);
            _eventBus.Subscribe<InvalidSwapEvent>(OnInvalidSwap);
            _eventBus.Subscribe<TilesClearedEvent>(OnTilesCleared);
            _eventBus.Subscribe<BoosterCreatedEvent>(OnBoosterCreated);
            _eventBus.Subscribe<BoosterActivatedEvent>(OnBoosterActivated);
            _eventBus.Subscribe<BoosterComboEvent>(OnBoosterCombo);
            _eventBus.Subscribe<BlockDamagedEvent>(OnBlockDamaged);
            _eventBus.Subscribe<BlockDestroyedEvent>(OnBlockDestroyed);
            _eventBus.Subscribe<BlockColorChangedEvent>(OnBlockColorChanged);
            _eventBus.Subscribe<BoardShuffledEvent>(OnBoardShuffled);
            _eventBus.Subscribe<BoardInputLockChangedEvent>(OnInputLockChanged);

            if (inputHandler != null)
            {
                inputHandler.Configure(WorldToBoard, BoardToWorld);
                inputHandler.SwapRequested += OnSwapRequested;
                inputHandler.SelectionChanged += OnSelectionChanged;
                inputHandler.SetInputEnabled(true);
            }

            BuildBoardVisuals();
            FitCamera();
        }

        public void Teardown()
        {
            if (_controller != null)
            {
                _controller.OnGravityApplied -= OnGravityApplied;
                if (ReferenceEquals(_controller.Timings, this))
                {
                    _controller.Timings = null;
                }
                _controller = null;
            }

            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<TileSwappedEvent>(OnTileSwapped);
                _eventBus.Unsubscribe<InvalidSwapEvent>(OnInvalidSwap);
                _eventBus.Unsubscribe<TilesClearedEvent>(OnTilesCleared);
                _eventBus.Unsubscribe<BoosterCreatedEvent>(OnBoosterCreated);
                _eventBus.Unsubscribe<BoosterActivatedEvent>(OnBoosterActivated);
                _eventBus.Unsubscribe<BoosterComboEvent>(OnBoosterCombo);
                _eventBus.Unsubscribe<BlockDamagedEvent>(OnBlockDamaged);
                _eventBus.Unsubscribe<BlockDestroyedEvent>(OnBlockDestroyed);
                _eventBus.Unsubscribe<BlockColorChangedEvent>(OnBlockColorChanged);
                _eventBus.Unsubscribe<BoardShuffledEvent>(OnBoardShuffled);
                _eventBus.Unsubscribe<BoardInputLockChangedEvent>(OnInputLockChanged);
                _eventBus = null;
            }

            if (inputHandler != null)
            {
                inputHandler.SwapRequested -= OnSwapRequested;
                inputHandler.SelectionChanged -= OnSelectionChanged;
            }

            if (boardRoot != null)
            {
                for (int i = boardRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(boardRoot.GetChild(i).gameObject);
                }
            }

            _tileViews = null;
            _blockViews = null;
            _tilePool = null;
            _selected = null;
            _hintA = null;
            _hintB = null;
        }

        private void OnDestroy()
        {
            Teardown();
        }

        private void BuildBoardVisuals()
        {
            var board = _controller.Board;

            foreach (var cell in board.AllCells())
            {
                var pos = new Vector2Int(cell.X, cell.Y);

                if (!cell.IsHole && cellBackgroundPrefab != null)
                {
                    var background = Instantiate(cellBackgroundPrefab, BoardToWorld(pos), Quaternion.identity, boardRoot);
                    background.sprite = tileDatabase.CellBackgroundSprite;
                    background.transform.localScale = Vector3.one * cellSize;
                }

                if (cell.HasBlock)
                {
                    SpawnBlockView(cell.Block, pos);
                }

                if (cell.Tile != null)
                {
                    var view = SpawnTileView(cell.Tile, pos);
                    view.transform.position = BoardToWorld(pos);
                }
            }
        }

        private void FitCamera()
        {
            if (!autoFitCamera || boardCamera == null || !boardCamera.orthographic) return;

            var board = _controller.Board;
            float boardWidth = board.Width * cellSize;
            float boardHeight = board.Height * cellSize;

            float requiredHalfHeight = (boardHeight + cameraPadding * cellSize) * 0.5f;
            float requiredHalfWidth = (boardWidth + cameraPadding * cellSize) * 0.5f;

            float halfHeightForWidth = requiredHalfWidth / Mathf.Max(0.0001f, boardCamera.aspect);
            boardCamera.orthographicSize = Mathf.Max(requiredHalfHeight, halfHeightForWidth);

            var center = new Vector3((board.Width - 1) * cellSize * 0.5f, (board.Height - 1) * cellSize * 0.5f, -10f);
            boardCamera.transform.position = center;
        }


        public Vector3 BoardToWorld(Vector2Int boardPos)
        {
            return new Vector3(boardPos.x * cellSize, boardPos.y * cellSize, 0f);
        }

        private Vector2Int? WorldToBoard(Vector3 worldPos)
        {
            int x = Mathf.RoundToInt(worldPos.x / cellSize);
            int y = Mathf.RoundToInt(worldPos.y / cellSize);
            var pos = new Vector2Int(x, y);

            if (_controller?.Board == null || !_controller.Board.IsInBounds(pos))
            {
                return null;
            }
            return pos;
        }


        public float SwapDuration => swapDuration;

        public float ClearDuration => clearDuration;

        public float GetBoosterDuration(BoosterType booster)
        {
            switch (booster)
            {
                case BoosterType.RainbowBall: return rainbowDuration;
                case BoosterType.Plane: return planeDuration;
                default: return boosterDuration;
            }
        }

        public float GetGravityDuration(GravityResult result)
        {
            return GetFallDuration(result.LongestDistance) + settleDelay;
        }

        private float GetFallDuration(int distanceInCells)
        {
            return Mathf.Clamp(distanceInCells * fallDurationPerCell, minFallDuration, maxFallDuration);
        }


        private void OnSwapRequested(Vector2Int a, Vector2Int b)
        {
            ResetIdle();
            if (_controller == null || !_controller.IsAcceptingInput) return;
            StartCoroutine(_controller.TrySwap(a, b));
        }

        private void OnSelectionChanged(Vector2Int? pos)
        {
            ResetIdle();

            if (_selected.HasValue)
            {
                GetTileView(_selected.Value)?.SetSelected(false);
            }

            _selected = pos;

            if (pos.HasValue)
            {
                GetTileView(pos.Value)?.SetSelected(true);
            }
        }

        private void OnInputLockChanged(BoardInputLockChangedEvent evt)
        {
            if (inputHandler != null)
            {
                inputHandler.SetInputEnabled(!evt.IsLocked);
            }

            if (evt.IsLocked)
            {
                ClearHint();
                if (_selected.HasValue)
                {
                    GetTileView(_selected.Value)?.SetSelected(false);
                    _selected = null;
                }
            }
            ResetIdle();
        }


        private void OnTileSwapped(TileSwappedEvent evt)
        {
            var viewA = GetTileView(evt.PositionA);
            var viewB = GetTileView(evt.PositionB);
            if (viewA == null || viewB == null) return;

            SetTileView(evt.PositionA, viewB);
            SetTileView(evt.PositionB, viewA);

            viewA.AnimateSwapTo(BoardToWorld(evt.PositionB), swapDuration);
            viewB.AnimateSwapTo(BoardToWorld(evt.PositionA), swapDuration);
        }

        private void OnInvalidSwap(InvalidSwapEvent evt)
        {
            var viewA = GetTileView(evt.PositionA);
            var viewB = GetTileView(evt.PositionB);
            if (viewA == null || viewB == null) return;

            var worldA = BoardToWorld(evt.PositionA);
            var worldB = BoardToWorld(evt.PositionB);
            var direction = (worldB - worldA).normalized;

            viewA.AnimateRejectedSwap(worldA, direction, cellSize * 0.32f, swapDuration * 1.6f);
            viewB.AnimateRejectedSwap(worldB, -direction, cellSize * 0.32f, swapDuration * 1.6f);
        }

        private void OnTilesCleared(TilesClearedEvent evt)
        {
            foreach (var pos in evt.Positions)
            {
                var view = GetTileView(pos);
                if (view == null) continue;

                SetTileView(pos, null);

                var captured = view;
                captured.AnimateClear(clearDuration).OnComplete(() =>
                {
                    captured.ResetVisualState();
                    _tilePool?.Release(captured);
                });
            }
        }

        private void OnBoosterCreated(BoosterCreatedEvent evt)
        {
            var view = GetTileView(evt.Position);
            if (view == null) return;

            view.RefreshVisuals();
            view.AnimatePromotion();
        }

        private void OnBoosterActivated(BoosterActivatedEvent evt)
        {
            GetTileView(evt.Position)?.AnimateBoosterActivation(GetBoosterDuration(evt.Booster));

            if (boosterVfx != null)
            {
                boosterVfx.PlayBooster(evt.Booster, evt.Position, evt.TargetCell, evt.AffectedCells,
                    cellSize, BoardToWorld, GetBoosterDuration(evt.Booster));
            }
        }

        private void OnBoosterCombo(BoosterComboEvent evt)
        {
            if (boosterVfx != null)
            {
                float duration = Mathf.Max(GetBoosterDuration(evt.FirstBooster), GetBoosterDuration(evt.SecondBooster));
                boosterVfx.PlayCombo(evt.Position, evt.AffectedCells, cellSize, BoardToWorld, duration);
            }
        }

        private void OnBlockDamaged(BlockDamagedEvent evt)
        {
            var view = GetBlockView(evt.Position);
            if (view == null) return;

            view.Refresh(_controller.Board.GetCell(evt.Position)?.Block);
            view.AnimateDamage(clearDuration);
        }

        private void OnBlockDestroyed(BlockDestroyedEvent evt)
        {
            var view = GetBlockView(evt.Position);
            SetBlockView(evt.Position, null);

            if (view != null)
            {
                var captured = view;
                captured.AnimateDestroy(clearDuration).OnComplete(() =>
                {
                    if (captured != null) Destroy(captured.gameObject);
                });
            }

            if (evt.RevealedKind != BlockKind.None)
            {
                var inner = _controller.Board.GetCell(evt.Position)?.Block;
                if (inner != null)
                {
                    SpawnBlockView(inner, evt.Position);
                }
            }
        }

        private void OnBlockColorChanged(BlockColorChangedEvent evt)
        {
            var view = GetBlockView(evt.Position);
            if (view == null) return;

            view.Refresh(_controller.Board.GetCell(evt.Position)?.Block);
            view.AnimateColorChange(clearDuration);
        }

        private void OnBoardShuffled(BoardShuffledEvent evt)
        {
            var board = _controller.Board;
            var remaining = new Dictionary<int, TileView>();

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var view = _tileViews[x, y];
                    if (view?.Tile != null)
                    {
                        remaining[view.Tile.Id] = view;
                    }
                    _tileViews[x, y] = null;
                }
            }

            foreach (var cell in board.AllCells())
            {
                if (cell.Tile == null) continue;
                var pos = new Vector2Int(cell.X, cell.Y);

                if (remaining.TryGetValue(cell.Tile.Id, out var view))
                {
                    view.SetTile(cell.Tile, pos);
                    view.AnimateSwapTo(BoardToWorld(pos), swapDuration * 2f);
                    SetTileView(pos, view);
                    remaining.Remove(cell.Tile.Id);
                }
                else
                {
                    var spawned = SpawnTileView(cell.Tile, pos);
                    spawned.transform.position = BoardToWorld(pos);
                }
            }

            foreach (var orphan in remaining.Values)
            {
                orphan.ResetVisualState();
                _tilePool?.Release(orphan);
            }
        }

        private void OnGravityApplied(GravityResult result)
        {
            foreach (var move in result.Movements)
            {
                float duration = GetFallDuration(move.Distance);

                if (move.IsSpawned)
                {
                    var view = SpawnTileView(move.Tile, move.To);
                    view.AnimateSpawnFall(BoardToWorld(move.From), BoardToWorld(move.To), duration);
                    continue;
                }

                var existing = GetTileView(move.From);
                if (existing == null) continue;

                SetTileView(move.From, null);
                SetTileView(move.To, existing);
                existing.SetBoardPosition(move.To);
                existing.AnimateFall(BoardToWorld(move.To), duration);
            }
        }


        private void Update()
        {
            if (_controller == null || !_controller.IsAcceptingInput)
            {
                _idleTimer = 0f;
                return;
            }

            _idleTimer += Time.deltaTime;
            if (_idleTimer >= hintIdleSeconds && !_hintA.HasValue)
            {
                ShowHint();
            }
        }

        private void ShowHint()
        {
            var move = _hintFinder.FindFirstMove(_controller.Board);
            if (!move.HasValue) return;

            _hintA = move.Value.a;
            _hintB = move.Value.b;

            GetTileView(_hintA.Value)?.PlayHint();
            GetTileView(_hintB.Value)?.PlayHint();
        }

        private void ClearHint()
        {
            if (_hintA.HasValue) GetTileView(_hintA.Value)?.StopHint();
            if (_hintB.HasValue) GetTileView(_hintB.Value)?.StopHint();
            _hintA = null;
            _hintB = null;
        }

        private void ResetIdle()
        {
            _idleTimer = 0f;
            ClearHint();
        }


        private TileView SpawnTileView(Tile tile, Vector2Int pos)
        {
            var view = _tilePool.Get();
            view.Initialize(tile, pos, tileDatabase);
            view.transform.localScale = Vector3.one * cellSize;
            SetTileView(pos, view);
            return view;
        }

        private void SpawnBlockView(BlockInstance block, Vector2Int pos)
        {
            if (blockPrefab == null) return;

            var view = Instantiate(blockPrefab, BoardToWorld(pos), Quaternion.identity, boardRoot);
            view.transform.localScale = Vector3.one * cellSize;
            view.Initialize(block, pos, tileDatabase);
            SetBlockView(pos, view);
        }

        private TileView GetTileView(Vector2Int pos)
        {
            if (_tileViews == null || !IsInside(pos)) return null;
            return _tileViews[pos.x, pos.y];
        }

        private void SetTileView(Vector2Int pos, TileView view)
        {
            if (_tileViews == null || !IsInside(pos)) return;
            _tileViews[pos.x, pos.y] = view;
        }

        private BlockView GetBlockView(Vector2Int pos)
        {
            if (_blockViews == null || !IsInside(pos)) return null;
            return _blockViews[pos.x, pos.y];
        }

        private void SetBlockView(Vector2Int pos, BlockView view)
        {
            if (_blockViews == null || !IsInside(pos)) return;
            _blockViews[pos.x, pos.y] = view;
        }

        private bool IsInside(Vector2Int pos)
        {
            return _controller?.Board != null && _controller.Board.IsInBounds(pos);
        }
    }
}
