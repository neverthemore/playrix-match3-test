using System;
using System.Collections;
using System.Collections.Generic;
using Match3.Core.Domain;
using Match3.Core.Events;
using Match3.Gameplay.Blocks;
using Match3.Gameplay.Tiles;
using UnityEngine;

namespace Match3.Gameplay.Board
{
    public class BoardController : IBlockTurnContext
    {
        public Board Board { get; private set; }
        public BoardProcessingState State { get; private set; } = BoardProcessingState.Idle;
        public bool IsAcceptingInput => State == BoardProcessingState.Idle;

        public IBoardTimings Timings { get; set; }

        public Func<IReadOnlyDictionary<GoalKey, int>> GoalWeightsProvider { get; set; }

        private readonly EventBus _eventBus;
        private readonly MatchFinder _matchFinder;
        private readonly GravitySystem _gravitySystem;
        private readonly BoosterEffects _boosterEffects;
        private readonly BoardGenerator _generator;
        private readonly BlockBehaviourRegistry _blockRegistry;
        private readonly System.Random _rng;

        private LevelConfig _config;
        private List<TileColor> _activeColors = new List<TileColor>();
        private int _chainIndex;
        private int _turnNumber;

        public BoardController(EventBus eventBus, BlockBehaviourRegistry blockRegistry, int randomSeed = -1)
        {
            _eventBus = eventBus;
            _blockRegistry = blockRegistry ?? new BlockBehaviourRegistry();
            _matchFinder = new MatchFinder();
            _gravitySystem = new GravitySystem();
            _boosterEffects = new BoosterEffects();
            _generator = new BoardGenerator(_matchFinder);
            _rng = randomSeed >= 0 ? new System.Random(randomSeed) : new System.Random();
        }

        public void InitializeLevel(LevelConfig config)
        {
            _config = config;
            _activeColors = config.GetActiveColors();
            _chainIndex = 0;
            _turnNumber = 0;
            Board = _generator.Generate(config, _activeColors, _rng);
            State = BoardProcessingState.Idle;
        }


        public IEnumerator TrySwap(Vector2Int a, Vector2Int b)
        {
            if (!IsAcceptingInput) yield break;

            if (!Board.IsInBounds(a) || !Board.IsInBounds(b) || !Board.AreAdjacent(a, b))
            {
                yield break;
            }

            var cellA = Board.GetCell(a);
            var cellB = Board.GetCell(b);
            if (cellA?.Tile == null || cellB?.Tile == null)
            {
                yield break;
            }

            var tileA = cellA.Tile;
            var tileB = cellB.Tile;

            SetState(BoardProcessingState.Swapping);

            bool bothBoosters = tileA.IsBooster && tileB.IsBooster;
            bool oneIsRainbow = tileA.Booster == BoosterType.RainbowBall || tileB.Booster == BoosterType.RainbowBall;

            Board.SwapTiles(a, b);

            bool createsMatch = _matchFinder.FindAllMatches(Board).Count > 0;

            bool isLegal = createsMatch || bothBoosters || oneIsRainbow;

            if (!isLegal)
            {
                Board.SwapTiles(a, b);
                _eventBus.Publish(new InvalidSwapEvent(a, b));
                SetState(BoardProcessingState.Idle);
                yield break;
            }

            _turnNumber++;
            _chainIndex = 0;

            _eventBus.Publish(new PlayerMoveCommittedEvent(_turnNumber));
            _eventBus.Publish(new TileSwappedEvent(a, b));
            yield return Wait(Timings?.SwapDuration ?? 0f);

            if (bothBoosters)
            {
                yield return ResolveBoosterCombo(a, b);
            }
            else if (oneIsRainbow)
            {
                yield return ResolveRainbowSwap(a, b);
            }
            else
            {
                yield return ResolveMatches(preferredBoosterSpawn: b);
            }

            yield return FinishTurn();
        }

        public IEnumerator TryActivateBooster(Vector2Int pos)
        {
            if (!IsAcceptingInput) yield break;

            var tile = Board.GetTile(pos);
            if (tile == null || !tile.IsBooster) yield break;

            _turnNumber++;
            _chainIndex = 0;

            _eventBus.Publish(new PlayerMoveCommittedEvent(_turnNumber));

            SetState(BoardProcessingState.ResolvingMatches);
            yield return DetonateBoosters(new List<Vector2Int> { pos }, TileColor.None);
            yield return SettleBoard();

            yield return ResolveMatches(null);
            yield return FinishTurn();
        }

        private IEnumerator FinishTurn()
        {
            RunEndOfTurnBlockBehaviours();
            _eventBus.Publish(new TurnCompletedEvent(_turnNumber));

            if (!_matchFinder.HasAnyPossibleMove(Board))
            {
                yield return ReshuffleBoard();
            }

            SetState(BoardProcessingState.Idle);
        }


        private IEnumerator ResolveMatches(Vector2Int? preferredBoosterSpawn)
        {
            while (true)
            {
                SetState(BoardProcessingState.ResolvingMatches);

                var matches = _matchFinder.FindAllMatches(Board);
                if (matches.Count == 0)
                {
                    yield break;
                }

                _chainIndex++;
                _eventBus.Publish(new MatchFoundEvent(matches, _chainIndex));

                var promotions = new List<(Vector2Int pos, BoosterType booster, TileColor color)>();
                var toClear = new HashSet<Vector2Int>();
                var matchedColors = new Dictionary<Vector2Int, TileColor>();

                foreach (var match in matches)
                {
                    var booster = BoosterPromotion.GetPromotion(match);
                    if (booster != BoosterType.None)
                    {
                        var spawn = match.GetBoosterSpawnPosition(preferredBoosterSpawn);
                        promotions.Add((spawn, booster, match.Color));
                    }

                    foreach (var pos in match.Positions)
                    {
                        toClear.Add(pos);
                        matchedColors[pos] = match.Color;
                    }
                }

                DamageBlocksAdjacentTo(matchedColors);

                foreach (var promotion in promotions)
                {
                    toClear.Remove(promotion.pos);
                }

                var boostersInMatch = new List<Vector2Int>();
                foreach (var pos in toClear)
                {
                    var tile = Board.GetTile(pos);
                    if (tile != null && tile.IsBooster)
                    {
                        boostersInMatch.Add(pos);
                    }
                }

                if (boostersInMatch.Count > 0)
                {
                    foreach (var pos in boostersInMatch)
                    {
                        toClear.Remove(pos);
                    }
                    yield return ClearCells(toClear);
                    yield return DetonateBoosters(boostersInMatch, TileColor.None);
                }
                else
                {
                    yield return ClearCells(toClear);
                }

                foreach (var (pos, booster, color) in promotions)
                {
                    var cell = Board.GetCell(pos);
                    if (cell?.Tile == null)
                    {
                        if (cell != null && cell.CanHoldTile)
                        {
                            cell.Tile = new Tile(Board.GenerateNextTileId(), color);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    cell.Tile.PromoteTo(booster);
                    _eventBus.Publish(new BoosterCreatedEvent(pos, booster));
                }

                yield return SettleBoard();

                preferredBoosterSpawn = null;
            }
        }


        private IEnumerator DetonateBoosters(List<Vector2Int> origins, TileColor rainbowColor)
        {
            var detonated = new HashSet<Vector2Int>();
            var wave = new List<Vector2Int>(origins);
            int depth = 0;

            while (wave.Count > 0)
            {
                var blast = new HashSet<Vector2Int>();
                var firedThisWave = new List<(Vector2Int pos, BoosterType booster)>();
                float waveDuration = 0f;

                foreach (var pos in wave)
                {
                    if (!detonated.Add(pos)) continue;

                    var tile = Board.GetTile(pos);
                    if (tile == null || !tile.IsBooster) continue;

                    var booster = tile.Booster;

                    var color = booster == BoosterType.RainbowBall
                        ? (rainbowColor != TileColor.None ? rainbowColor : _boosterEffects.GetMostCommonColor(Board))
                        : TileColor.None;

                    var planeTarget = booster == BoosterType.Plane
                        ? _boosterEffects.FindPlaneTarget(Board, pos, blast, GoalWeightsProvider?.Invoke())
                        : pos;

                    var affected = _boosterEffects.GetAffected(Board, pos, booster, color, planeTarget);

                    ReportBoosterCollected(booster, pos);
                    _eventBus.Publish(new BoosterActivatedEvent(pos, booster, new List<Vector2Int>(affected), depth, planeTarget));

                    blast.UnionWith(affected);
                    blast.Add(pos);
                    firedThisWave.Add((pos, booster));

                    waveDuration = Mathf.Max(waveDuration, Timings?.GetBoosterDuration(booster) ?? 0f);
                }

                if (firedThisWave.Count == 0)
                {
                    yield break;
                }

                yield return Wait(waveDuration);

                var nextWave = new List<Vector2Int>();
                foreach (var pos in blast)
                {
                    if (detonated.Contains(pos)) continue;
                    var tile = Board.GetTile(pos);
                    if (tile != null && tile.IsBooster)
                    {
                        nextWave.Add(pos);
                    }
                }
                foreach (var pos in nextWave)
                {
                    blast.Remove(pos);
                }

                DamageBlocksInBlast(blast);

                yield return ClearCells(blast);

                wave = nextWave;
                depth++;
            }
        }

        private IEnumerator ResolveBoosterCombo(Vector2Int a, Vector2Int b)
        {
            SetState(BoardProcessingState.ResolvingMatches);

            var tileA = Board.GetTile(a);
            var tileB = Board.GetTile(b);
            if (tileA == null || tileB == null) yield break;

            var typeA = tileA.Booster;
            var typeB = tileB.Booster;

            var affected = _boosterEffects.GetComboAffected(Board, a, b, typeA, typeB);

            ReportBoosterCollected(typeA, a);
            ReportBoosterCollected(typeB, b);
            _eventBus.Publish(new BoosterComboEvent(b, typeA, typeB, new List<Vector2Int>(affected)));

            float duration = Mathf.Max(
                Timings?.GetBoosterDuration(typeA) ?? 0f,
                Timings?.GetBoosterDuration(typeB) ?? 0f);
            yield return Wait(duration);

            affected.Add(a);
            affected.Add(b);

            var chained = new List<Vector2Int>();
            foreach (var pos in affected)
            {
                if (pos == a || pos == b) continue;
                var tile = Board.GetTile(pos);
                if (tile != null && tile.IsBooster)
                {
                    chained.Add(pos);
                }
            }
            foreach (var pos in chained)
            {
                affected.Remove(pos);
            }

            DamageBlocksInBlast(affected);
            yield return ClearCells(affected);

            if (chained.Count > 0)
            {
                yield return DetonateBoosters(chained, TileColor.None);
            }

            yield return SettleBoard();
            yield return ResolveMatches(null);
        }

        private IEnumerator ResolveRainbowSwap(Vector2Int a, Vector2Int b)
        {
            SetState(BoardProcessingState.ResolvingMatches);

            var tileA = Board.GetTile(a);
            var tileB = Board.GetTile(b);
            if (tileA == null || tileB == null) yield break;

            bool aIsBall = tileA.Booster == BoosterType.RainbowBall;
            var ballPos = aIsBall ? a : b;
            var otherTile = aIsBall ? tileB : tileA;

            var color = otherTile.IsColorless
                ? _boosterEffects.GetMostCommonColor(Board)
                : otherTile.Color;

            yield return DetonateBoosters(new List<Vector2Int> { ballPos }, color);
            yield return SettleBoard();
            yield return ResolveMatches(null);
        }


        private IEnumerator ClearCells(ICollection<Vector2Int> positions)
        {
            if (positions.Count == 0) yield break;

            var cleared = new List<Vector2Int>(positions.Count);

            foreach (var pos in positions)
            {
                var cell = Board.GetCell(pos);
                if (cell?.Tile == null) continue;
                cleared.Add(pos);
            }

            if (cleared.Count == 0) yield break;

            _eventBus.Publish(new TilesClearedEvent(cleared));

            foreach (var pos in cleared)
            {
                var cell = Board.GetCell(pos);
                if (cell?.Tile == null) continue;

                ReportTileCollected(cell.Tile, pos);
                cell.Tile = null;
            }

            yield return Wait(Timings?.ClearDuration ?? 0f);
        }

        private IEnumerator SettleBoard()
        {
            SetState(BoardProcessingState.ApplyingGravity);

            var result = _gravitySystem.ApplyGravityAndRefill(Board, _activeColors, _rng);
            if (!result.HasChanges)
            {
                yield break;
            }

            _eventBus.Publish(new BoardRefillStartedEvent());
            OnGravityApplied?.Invoke(result);

            yield return Wait(Timings?.GetGravityDuration(result) ?? 0f);

            _eventBus.Publish(new BoardRefillCompletedEvent());
        }

        public event Action<GravityResult> OnGravityApplied;

        private void DamageBlocksAdjacentTo(Dictionary<Vector2Int, TileColor> matchedCells)
        {
            var hits = new Dictionary<Vector2Int, TileColor>();

            foreach (var kvp in matchedCells)
            {
                foreach (var neighbor in Board.GetOrthogonalNeighbors(kvp.Key))
                {
                    var cell = Board.GetCell(neighbor);
                    if (cell == null || !cell.HasBlock) continue;
                    if (hits.ContainsKey(neighbor)) continue;
                    hits[neighbor] = kvp.Value;
                }
            }

            foreach (var kvp in hits)
            {
                ApplyBlockDamage(kvp.Key, new BlockDamage(BlockDamageSource.AdjacentMatch, kvp.Value));
            }
        }

        private void DamageBlocksInBlast(ICollection<Vector2Int> blast)
        {
            foreach (var pos in blast)
            {
                var cell = Board.GetCell(pos);
                if (cell == null || !cell.HasBlock) continue;
                ApplyBlockDamage(pos, new BlockDamage(BlockDamageSource.BoosterBlast, TileColor.None));
            }
        }

        private void ApplyBlockDamage(Vector2Int pos, BlockDamage damage)
        {
            var cell = Board.GetCell(pos);
            var block = cell?.Block;
            if (block == null) return;

            var behaviour = _blockRegistry.Get(block);
            if (!behaviour.CanBeDamagedBy(block, damage))
            {
                return;
            }

            block.Lives--;

            if (block.Lives > 0)
            {
                _eventBus.Publish(new BlockDamagedEvent(pos, block.Kind, block.Lives));
                return;
            }

            var inner = block.Inner;
            cell.Block = inner;

            ReportBlockCollected(block.Kind, pos);
            _eventBus.Publish(new BlockDestroyedEvent(pos, block.Kind, inner?.Kind ?? BlockKind.None));
        }

        private void RunEndOfTurnBlockBehaviours()
        {
            foreach (var cell in Board.AllCells())
            {
                var block = cell.Block;
                if (block == null) continue;

                var behaviour = _blockRegistry.Get(block);
                if (behaviour.OnTurnCompleted(block, this))
                {
                    _eventBus.Publish(new BlockColorChangedEvent(new Vector2Int(cell.X, cell.Y), block.Color));
                }
            }
        }

        public TileColor GetRandomActiveColor()
        {
            if (_activeColors.Count == 0) return TileColor.None;
            return _activeColors[_rng.Next(_activeColors.Count)];
        }

        public TileColor GetRandomActiveColorExcept(TileColor excluded)
        {
            int candidateCount = 0;
            foreach (var color in _activeColors)
            {
                if (color != TileColor.None && color != excluded)
                {
                    candidateCount++;
                }
            }

            if (candidateCount == 0)
            {
                return TileColor.None;
            }

            int selectedIndex = _rng.Next(candidateCount);
            foreach (var color in _activeColors)
            {
                if (color == TileColor.None || color == excluded) continue;
                if (selectedIndex-- == 0) return color;
            }

            return TileColor.None;
        }


        private void ReportTileCollected(Tile tile, Vector2Int pos)
        {
            if (tile.IsBooster) return;
            if (tile.Color == TileColor.None) return;

            _eventBus.Publish(new GoalContributionEvent(GoalKey.ForColor(tile.Color), 1, pos));
        }

        private void ReportBoosterCollected(BoosterType booster, Vector2Int pos)
        {
            if (booster == BoosterType.None) return;
            _eventBus.Publish(new GoalContributionEvent(GoalKey.ForBooster(booster), 1, pos));
        }

        private void ReportBlockCollected(BlockKind kind, Vector2Int pos)
        {
            if (kind == BlockKind.None) return;
            _eventBus.Publish(new GoalContributionEvent(GoalKey.ForBlock(kind), 1, pos));
        }


        private IEnumerator ReshuffleBoard()
        {
            SetState(BoardProcessingState.Shuffling);

            var tiles = new List<Tile>();
            var slots = new List<Cell>();

            foreach (var cell in Board.AllCells())
            {
                if (!cell.CanHoldTile) continue;
                if (cell.Tile != null)
                {
                    tiles.Add(cell.Tile);
                    cell.Tile = null;
                }
                slots.Add(cell);
            }

            const int maxAttempts = 60;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Shuffle(tiles);

                for (int i = 0; i < slots.Count && i < tiles.Count; i++)
                {
                    slots[i].Tile = tiles[i];
                }

                bool clean = _matchFinder.FindAllMatches(Board).Count == 0;
                bool playable = _matchFinder.HasAnyPossibleMove(Board);

                if (clean && playable)
                {
                    break;
                }

                foreach (var slot in slots)
                {
                    slot.Tile = null;
                }
            }

            for (int i = 0; i < slots.Count && i < tiles.Count; i++)
            {
                if (slots[i].Tile == null)
                {
                    slots[i].Tile = tiles[i];
                }
            }

            _eventBus.Publish(new BoardShuffledEvent());
            yield return Wait(Timings?.SwapDuration ?? 0f);
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }


        public void CheatCollectForGoals(IReadOnlyDictionary<GoalKey, int> remaining)
        {
            if (remaining == null) return;

            foreach (var kvp in remaining)
            {
                int needed = kvp.Value;
                if (needed <= 0) continue;

                switch (kvp.Key.Kind)
                {
                    case GoalKind.CollectColor:
                        for (int i = 0; i < needed; i++)
                        {
                            _eventBus.Publish(new GoalContributionEvent(kvp.Key, 1, Vector2Int.zero));
                        }
                        break;

                    case GoalKind.CollectBooster:
                        for (int i = 0; i < needed; i++)
                        {
                            _eventBus.Publish(new GoalContributionEvent(kvp.Key, 1, Vector2Int.zero));
                        }
                        break;

                    case GoalKind.DestroyBlock:
                        CheatDestroyBlocks(kvp.Key.Block, needed);
                        break;
                }
            }
        }

        private void CheatDestroyBlocks(BlockKind kind, int count)
        {
            int destroyed = 0;

            foreach (var cell in Board.AllCells())
            {
                if (destroyed >= count) break;

                var block = cell.Block;
                if (block == null || block.Kind != kind) continue;

                var pos = new Vector2Int(cell.X, cell.Y);
                cell.Block = block.Inner;

                ReportBlockCollected(block.Kind, pos);
                _eventBus.Publish(new BlockDestroyedEvent(pos, block.Kind, block.Inner?.Kind ?? BlockKind.None));
                destroyed++;
            }

            for (int i = destroyed; i < count; i++)
            {
                _eventBus.Publish(new GoalContributionEvent(GoalKey.ForBlock(kind), 1, Vector2Int.zero));
            }
        }


        private IEnumerator Wait(float seconds)
        {
            if (seconds > 0f)
            {
                yield return new WaitForSeconds(seconds);
            }
        }

        private void SetState(BoardProcessingState newState)
        {
            bool wasAccepting = IsAcceptingInput;
            State = newState;

            if (wasAccepting != IsAcceptingInput)
            {
                _eventBus.Publish(new BoardInputLockChangedEvent(!IsAcceptingInput));
            }
        }
    }
}
