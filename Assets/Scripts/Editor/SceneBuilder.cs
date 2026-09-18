#if UNITY_EDITOR
using System.Collections.Generic;
using Match3.App;
using Match3.Core.Domain;
using Match3.Gameplay.Board;
using Match3.Gameplay.Input;
using Match3.Gameplay.Tiles;
using Match3.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.EditorTools
{
    public static class SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string PrefabFolder = "Assets/Prefabs";
        private const string TilePrefabPath = PrefabFolder + "/TileView.prefab";
        private const string BlockPrefabPath = PrefabFolder + "/BlockView.prefab";
        private const string CellBackgroundPrefabPath = PrefabFolder + "/CellBackground.prefab";
        private const string GoalItemPrefabPath = PrefabFolder + "/GoalTrackerItem.prefab";
        private const string TileDataFolder = "Assets/Data/Tiles";
        private const string TileDatabasePath = TileDataFolder + "/TileDatabase.asset";
        private const string LevelDatabasePath = "Assets/Data/LevelDatabase.asset";

        private static readonly (TileColor color, Color tint)[] Palette =
        {
            (TileColor.Red,    new Color(0.93f, 0.28f, 0.30f)),
            (TileColor.Blue,   new Color(0.24f, 0.55f, 0.95f)),
            (TileColor.Green,  new Color(0.30f, 0.78f, 0.40f)),
            (TileColor.Yellow, new Color(0.97f, 0.80f, 0.22f)),
            (TileColor.Purple, new Color(0.64f, 0.36f, 0.90f)),
            (TileColor.Orange, new Color(0.97f, 0.57f, 0.20f)),
        };

        [MenuItem("Match3/Build Everything")]
        public static void BuildEverything()
        {
            var database = BuildTileDatabase();
            var levels = LevelFactory.BuildAll();

            var tilePrefab = BuildTilePrefab(database);
            var blockPrefab = BuildBlockPrefab(database);
            var cellBackgroundPrefab = BuildCellBackgroundTemplate(database);
            var goalItemPrefab = BuildGoalItemPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            database = AssetDatabase.LoadAssetAtPath<TileDatabase>(TileDatabasePath);
            levels = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelDatabasePath);

            var tilePrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(TilePrefabPath);
            var blockPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BlockPrefabPath);
            var cellBackgroundPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CellBackgroundPrefabPath);
            var goalItemPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GoalItemPrefabPath);
            tilePrefab = tilePrefabAsset != null ? tilePrefabAsset.GetComponent<TileView>() : null;
            blockPrefab = blockPrefabAsset != null ? blockPrefabAsset.GetComponent<BlockView>() : null;
            cellBackgroundPrefab = cellBackgroundPrefabAsset != null
                ? cellBackgroundPrefabAsset.GetComponent<SpriteRenderer>()
                : null;
            goalItemPrefab = goalItemPrefabAsset != null
                ? goalItemPrefabAsset.GetComponent<GoalTrackerItem>()
                : null;

            if (database == null || levels == null || tilePrefab == null || blockPrefab == null ||
                cellBackgroundPrefab == null || goalItemPrefab == null)
            {
                throw new System.InvalidOperationException(
                    "[SceneBuilder] Required generated assets could not be reloaded after import. " +
                    "The existing scene was left untouched.");
            }

            BuildScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneBuilder] Build complete: sprites, database, 10 levels, prefabs and scene.");
        }


        private static TileDatabase BuildTileDatabase()
        {
            System.IO.Directory.CreateDirectory(TileDataFolder);

            var definitions = new List<TileDefinition>();
            foreach (var (color, tint) in Palette)
            {
                var sprite = PlaceholderSpriteFactory.GetOrCreate(
                    $"Chip_{color}", () => PlaceholderSpriteFactory.PaintChip(tint));

                string path = $"{TileDataFolder}/TileDef_{color}.asset";
                var definition = AssetDatabase.LoadAssetAtPath<TileDefinition>(path);
                bool isNew = definition == null;
                if (isNew)
                {
                    definition = ScriptableObject.CreateInstance<TileDefinition>();
                }

                definition.EditorConfigure(color, sprite, Color.white, tint);

                if (isNew) AssetDatabase.CreateAsset(definition, path);
                else EditorUtility.SetDirty(definition);

                definitions.Add(definition);
            }

            var lineH = PlaceholderSpriteFactory.GetOrCreate("Booster_LineH", () => PlaceholderSpriteFactory.PaintLineArrow(true));
            var lineV = PlaceholderSpriteFactory.GetOrCreate("Booster_LineV", () => PlaceholderSpriteFactory.PaintLineArrow(false));
            var bomb = PlaceholderSpriteFactory.GetOrCreate("Booster_Bomb", () => PlaceholderSpriteFactory.PaintBurst(8, Color.white));
            var plane = PlaceholderSpriteFactory.GetOrCreate("Booster_Plane", () => PlaceholderSpriteFactory.PaintTriangle(Color.white));
            var rainbow = PlaceholderSpriteFactory.GetOrCreate("Booster_Rainbow", PlaceholderSpriteFactory.PaintRainbowBall);

            var crate = PlaceholderSpriteFactory.GetOrCreate("Block_Crate",
                () => PlaceholderSpriteFactory.PaintRoundedSquare(new Color(0.72f, 0.52f, 0.31f), new Color(0.44f, 0.30f, 0.16f), 0.06f, 0.10f, 0.05f));

            var coloredCrate = PlaceholderSpriteFactory.GetOrCreate("Block_ColoredCrate",
                () => PlaceholderSpriteFactory.PaintRoundedSquare(new Color(0.88f, 0.86f, 0.82f), new Color(0.45f, 0.43f, 0.40f), 0.06f, 0.10f, 0.05f));

            var chameleon = PlaceholderSpriteFactory.GetOrCreate("Block_Chameleon",
                () => PlaceholderSpriteFactory.PaintRoundedSquare(new Color(0.80f, 0.84f, 0.88f), new Color(0.35f, 0.40f, 0.46f), 0.06f, 0.22f, 0.05f));

            var blocker = PlaceholderSpriteFactory.GetOrCreate("Block_Blocker",
                () => PlaceholderSpriteFactory.PaintRoundedSquare(new Color(0.42f, 0.43f, 0.47f), new Color(0.22f, 0.23f, 0.26f), 0.04f, 0.06f, 0.06f));

            var cracks = PlaceholderSpriteFactory.GetOrCreate("Block_Cracks", PlaceholderSpriteFactory.PaintCracks);

            var cellBackground = PlaceholderSpriteFactory.GetOrCreate("CellBackground",
                () => PlaceholderSpriteFactory.PaintRoundedSquare(new Color(1f, 1f, 1f, 0.11f), new Color(1f, 1f, 1f, 0.05f), 0.04f, 0.14f, 0.02f));

            var glow = PlaceholderSpriteFactory.GetOrCreate("Glow", PlaceholderSpriteFactory.PaintGlow);

            PlaceholderSpriteFactory.GetOrCreate("UI_Panel", PlaceholderSpriteFactory.PaintUiPanel);
            PlaceholderSpriteFactory.GetOrCreate("UI_Check", () => PlaceholderSpriteFactory.PaintCheckmark(new Color(0.25f, 0.78f, 0.42f)));

            var database = AssetDatabase.LoadAssetAtPath<TileDatabase>(TileDatabasePath);
            bool databaseIsNew = database == null;
            if (databaseIsNew)
            {
                database = ScriptableObject.CreateInstance<TileDatabase>();
            }

            database.EditorConfigure(definitions, lineH, lineV, bomb, plane, rainbow,
                crate, coloredCrate, chameleon, blocker, cracks, cellBackground, glow);

            if (databaseIsNew) AssetDatabase.CreateAsset(database, TileDatabasePath);
            else EditorUtility.SetDirty(database);

            AssetDatabase.SaveAssets();
            return database;
        }


        private static TileView BuildTilePrefab(TileDatabase database)
        {
            System.IO.Directory.CreateDirectory(PrefabFolder);
            string path = TilePrefabPath;

            var root = new GameObject("TileView");
            var view = root.AddComponent<TileView>();

            var baseSprite = CreateSpriteChild(root.transform, "Base", 10);
            var overlay = CreateSpriteChild(root.transform, "Overlay", 11);
            var ring = CreateSpriteChild(root.transform, "SelectionRing", 9);

            baseSprite.sprite = database.GetDefinition(TileColor.Red)?.Sprite;
            overlay.enabled = false;
            ring.sprite = database.HighlightSprite;
            ring.color = new Color(1f, 1f, 1f, 0.55f);
            ring.transform.localScale = Vector3.one * 1.25f;
            ring.enabled = false;

            Bind(view, "baseSprite", baseSprite);
            Bind(view, "overlaySprite", overlay);
            Bind(view, "selectionRing", ring);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<TileView>();
        }

        private static BlockView BuildBlockPrefab(TileDatabase database)
        {
            string path = BlockPrefabPath;

            var root = new GameObject("BlockView");
            var view = root.AddComponent<BlockView>();

            var baseSprite = CreateSpriteChild(root.transform, "Base", 20);
            var swatch = CreateSpriteChild(root.transform, "ColorSwatch", 21);
            var cracks = CreateSpriteChild(root.transform, "Cracks", 22);

            baseSprite.sprite = database.GetBlockSprite(BlockKind.Crate);

            swatch.sprite = database.HighlightSprite;
            swatch.transform.localScale = Vector3.one * 0.5f;
            swatch.enabled = false;

            cracks.sprite = database.CrackOverlay;
            cracks.enabled = false;

            Bind(view, "baseSprite", baseSprite);
            Bind(view, "colorSwatch", swatch);
            Bind(view, "crackSprite", cracks);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<BlockView>();
        }

        private static SpriteRenderer CreateSpriteChild(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }


        private static void BuildScene()
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.22f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var boardGo = new GameObject("Board");
            var boardView = boardGo.AddComponent<BoardView>();
            var inputHandler = boardGo.AddComponent<BoardInputHandler>();
            var vfx = boardGo.AddComponent<BoosterVfx>();

            var boardRoot = new GameObject("BoardRoot").transform;
            boardRoot.SetParent(boardGo.transform, false);

            var vfxRoot = new GameObject("VfxRoot").transform;
            vfxRoot.SetParent(boardGo.transform, false);

            Bind(boardView, "inputHandler", inputHandler);
            Bind(boardView, "boosterVfx", vfx);
            Bind(boardView, "boardRoot", boardRoot);
            Bind(boardView, "boardCamera", camera);

            Bind(inputHandler, "targetCamera", camera);
            Bind(vfx, "effectRoot", vfxRoot);

            var canvas = BuildCanvas();
            var hud = BuildHud(canvas);
            var resultScreen = BuildResultScreen(canvas);
            var cheatPanel = BuildCheatPanel(canvas);
            var levelMap = BuildLevelMap(canvas, boardGo, cheatPanel);

            var managerGo = new GameObject("GameManager");
            var manager = managerGo.AddComponent<GameManager>();
            Bind(manager, "boardView", boardView);
            Bind(manager, "hudController", hud);
            Bind(manager, "resultScreen", resultScreen);

            var stableDatabase = AssetDatabase.LoadAssetAtPath<TileDatabase>(TileDatabasePath);
            var stableLevels = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelDatabasePath);
            var stableTilePrefab = LoadPrefabComponent<TileView>(TilePrefabPath);
            var stableBlockPrefab = LoadPrefabComponent<BlockView>(BlockPrefabPath);
            var stableCellBackground = LoadPrefabComponent<SpriteRenderer>(CellBackgroundPrefabPath);
            var stableGoalItem = LoadPrefabComponent<GoalTrackerItem>(GoalItemPrefabPath);

            if (stableDatabase == null || stableLevels == null || stableTilePrefab == null ||
                stableBlockPrefab == null || stableCellBackground == null || stableGoalItem == null)
            {
                throw new System.InvalidOperationException(
                    "[SceneBuilder] Persistent assets could not be loaded for final scene wiring.");
            }

            Bind(boardView, "tilePrefab", stableTilePrefab);
            Bind(boardView, "blockPrefab", stableBlockPrefab);
            Bind(boardView, "cellBackgroundPrefab", stableCellBackground);
            Bind(boardView, "tileDatabase", stableDatabase);
            Bind(hud, "goalItemPrefab", stableGoalItem);
            Bind(hud, "tileDatabase", stableDatabase);
            Bind(manager, "levelDatabase", stableLevels);
            Bind(manager, "levelMapController", levelMap);

            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static SpriteRenderer BuildCellBackgroundTemplate(TileDatabase database)
        {
            string path = CellBackgroundPrefabPath;

            var go = new GameObject("CellBackground");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = database.CellBackgroundSprite;
            renderer.sortingOrder = 0;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<SpriteRenderer>();
        }

        private static Canvas BuildCanvas()
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            return canvas;
        }

        private static HudController BuildHud(Canvas canvas)
        {
            var hudGo = CreateUiChild(canvas.transform, "HUD");
            var hudRect = hudGo.GetComponent<RectTransform>();
            Stretch(hudRect);

            var hud = hudGo.AddComponent<HudController>();

            var topBar = CreateUiChild(hudGo.transform, "TopBar");
            var topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.anchoredPosition = new Vector2(0f, -20f);
            topRect.sizeDelta = new Vector2(-40f, 150f);

            var levelLabel = CreateLabel(topBar.transform, "LevelLabel", "Level 1", 46f, TextAlignmentOptions.TopLeft);
            var levelRect = levelLabel.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0f, 0f);
            levelRect.anchorMax = new Vector2(0.6f, 1f);
            levelRect.offsetMin = new Vector2(20f, 0f);
            levelRect.offsetMax = Vector2.zero;

            var movesBadge = CreateUiChild(topBar.transform, "MovesBadge");
            var badgeImage = movesBadge.AddComponent<Image>();
            badgeImage.sprite = LoadSprite("UI_Panel");
            badgeImage.type = Image.Type.Sliced;
            badgeImage.color = new Color(0.22f, 0.56f, 0.92f);

            var badgeRect = movesBadge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1f, 0.5f);
            badgeRect.anchorMax = new Vector2(1f, 0.5f);
            badgeRect.pivot = new Vector2(1f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(-20f, 0f);
            badgeRect.sizeDelta = new Vector2(150f, 110f);

            var movesLabel = CreateLabel(movesBadge.transform, "MovesLabel", "25", 58f, TextAlignmentOptions.Center);
            Stretch(movesLabel.GetComponent<RectTransform>());

            var goalRow = CreateUiChild(hudGo.transform, "GoalRow");
            var goalRect = goalRow.GetComponent<RectTransform>();

            goalRect.anchorMin = new Vector2(0f, 1f);
            goalRect.anchorMax = new Vector2(0f, 1f);
            goalRect.pivot = new Vector2(0f, 1f);
            goalRect.anchoredPosition = new Vector2(40f, -180f);
            goalRect.sizeDelta = new Vector2(900f, 150f);

            var layout = goalRow.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var levelsButton = CreateButton(hudGo.transform, "LevelsButton", "УРОВНИ", new Color(0.34f, 0.42f, 0.68f));
            var levelsRect = levelsButton.GetComponent<RectTransform>();
            levelsRect.anchorMin = new Vector2(1f, 0f);
            levelsRect.anchorMax = new Vector2(1f, 0f);
            levelsRect.pivot = new Vector2(1f, 0f);
            levelsRect.anchoredPosition = new Vector2(-24f, 24f);
            levelsRect.sizeDelta = new Vector2(220f, 90f);

            Bind(hud, "levelLabel", levelLabel);
            Bind(hud, "movesLabel", movesLabel);
            Bind(hud, "movesBackground", badgeImage);
            Bind(hud, "goalContainer", goalRow.transform);

            return hud;
        }

        private static LevelMapController BuildLevelMap(Canvas canvas, GameObject board, GameObject cheatPanel)
        {
            var mapGo = CreateUiChild(canvas.transform, "LevelMap");
            Stretch(mapGo.GetComponent<RectTransform>());
            var map = mapGo.AddComponent<LevelMapController>();

            var bodyGo = CreateUiChild(mapGo.transform, "MapBody");
            Stretch(bodyGo.GetComponent<RectTransform>());
            var bodyImage = bodyGo.AddComponent<Image>();
            bodyImage.sprite = LoadSprite("UI_Panel");
            bodyImage.type = Image.Type.Sliced;
            bodyImage.color = new Color(0.07f, 0.09f, 0.16f, 0.98f);

            var panelGo = CreateUiChild(bodyGo.transform, "Panel");
            var panelImage = panelGo.AddComponent<Image>();
            panelImage.sprite = LoadSprite("UI_Panel");
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.13f, 0.16f, 0.25f, 0.98f);

            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(900f, 940f);

            var title = CreateLabel(panelGo.transform, "Title", "КАРТА УРОВНЕЙ", 64f, TextAlignmentOptions.Center);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -58f);
            titleRect.sizeDelta = new Vector2(-60f, 90f);

            var progress = CreateLabel(panelGo.transform, "Progress", "1 / 10 unlocked", 30f, TextAlignmentOptions.Center);
            var progressRect = progress.GetComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0f, 1f);
            progressRect.anchorMax = new Vector2(1f, 1f);
            progressRect.pivot = new Vector2(0.5f, 1f);
            progressRect.anchoredPosition = new Vector2(0f, -150f);
            progressRect.sizeDelta = new Vector2(-60f, 52f);
            progress.color = new Color(0.72f, 0.82f, 0.96f);

            var hint = CreateLabel(panelGo.transform, "Hint", "Пройди уровень, чтобы открыть следующий", 24f, TextAlignmentOptions.Center);
            var hintRect = hint.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 1f);
            hintRect.anchorMax = new Vector2(1f, 1f);
            hintRect.pivot = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0f, -205f);
            hintRect.sizeDelta = new Vector2(-60f, 44f);
            hint.color = new Color(0.60f, 0.66f, 0.78f);

            var gridGo = CreateUiChild(panelGo.transform, "LevelGrid");
            var gridRect = gridGo.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = new Vector2(0f, -35f);
            gridRect.sizeDelta = new Vector2(780f, 520f);

            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(136f, 136f);
            grid.spacing = new Vector2(20f, 28f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            var levelButtonTemplate = CreateButton(
                gridGo.transform,
                "LevelButtonTemplate",
                "1",
                new Color(0.22f, 0.56f, 0.92f));
            levelButtonTemplate.gameObject.SetActive(false);

            var stateLabel = CreateLabel(
                levelButtonTemplate.transform,
                "StateLabel",
                "OPEN",
                17f,
                TextAlignmentOptions.Center);
            var stateRect = stateLabel.GetComponent<RectTransform>();
            stateRect.anchorMin = new Vector2(0f, 0f);
            stateRect.anchorMax = new Vector2(1f, 0f);
            stateRect.pivot = new Vector2(0.5f, 0f);
            stateRect.anchoredPosition = new Vector2(0f, 8f);
            stateRect.sizeDelta = new Vector2(-12f, 24f);
            stateLabel.color = new Color(0.78f, 0.95f, 1f);

            var numberLabel = levelButtonTemplate.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (numberLabel != null)
            {
                numberLabel.fontSize = 54f;
                numberLabel.alignment = TextAlignmentOptions.Center;
                var numberRect = numberLabel.GetComponent<RectTransform>();
                numberRect.anchorMin = new Vector2(0f, 0.35f);
                numberRect.anchorMax = new Vector2(1f, 1f);
                numberRect.offsetMin = new Vector2(0f, 0f);
                numberRect.offsetMax = new Vector2(0f, -8f);
            }

            var openMapButton = canvas.transform.Find("HUD/LevelsButton")?.GetComponent<Button>();
            if (openMapButton == null)
            {
                openMapButton = CreateButton(
                    canvas.transform,
                    "LevelsButton",
                    "LEVELS",
                    new Color(0.34f, 0.42f, 0.68f));
                var openRect = openMapButton.GetComponent<RectTransform>();
                openRect.anchorMin = new Vector2(1f, 0f);
                openRect.anchorMax = new Vector2(1f, 0f);
                openRect.pivot = new Vector2(1f, 0f);
                openRect.anchoredPosition = new Vector2(-24f, 24f);
                openRect.sizeDelta = new Vector2(220f, 90f);
            }

            Bind(map, "mapBody", bodyGo);
            Bind(map, "levelButtonContainer", gridGo.transform);
            Bind(map, "levelButtonPrefab", levelButtonTemplate);
            Bind(map, "openMapButton", openMapButton);
            Bind(map, "titleLabel", title);
            Bind(map, "progressLabel", progress);
            Bind(map, "gameplayBoard", board);
            Bind(map, "cheatPanel", cheatPanel);

            return map;
        }

        private static GoalTrackerItem BuildGoalItemPrefab()
        {
            string path = GoalItemPrefabPath;

            var root = new GameObject("GoalTrackerItem", typeof(RectTransform));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(150f, 150f);

            var layoutElement = root.AddComponent<LayoutElement>();
            layoutElement.minWidth = 150f;
            layoutElement.minHeight = 150f;
            layoutElement.preferredWidth = 150f;
            layoutElement.preferredHeight = 150f;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            var item = root.AddComponent<GoalTrackerItem>();

            var background = root.AddComponent<Image>();
            background.sprite = LoadSprite("UI_Panel");
            background.color = new Color(0f, 0f, 0f, 0.35f);

            var iconGo = CreateUiChild(root.transform, "Icon");
            var icon = iconGo.AddComponent<Image>();
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 14f);
            iconRect.sizeDelta = new Vector2(88f, 88f);

            var label = CreateLabel(root.transform, "Progress", "0/10", 34f, TextAlignmentOptions.Center);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = 34f;

            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 8f);
            labelRect.sizeDelta = new Vector2(-20f, 44f);

            var checkGo = CreateUiChild(root.transform, "Checkmark");
            var check = checkGo.AddComponent<Image>();
            check.sprite = LoadSprite("UI_Check");
            var checkRect = checkGo.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.5f, 0f);
            checkRect.anchorMax = new Vector2(0.5f, 0f);
            checkRect.anchoredPosition = new Vector2(0f, 26f);
            checkRect.sizeDelta = new Vector2(56f, 56f);
            checkGo.SetActive(false);

            Bind(item, "icon", icon);
            Bind(item, "progressLabel", label);
            Bind(item, "checkmark", checkGo);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<GoalTrackerItem>();
        }

        private static LevelResultScreen BuildResultScreen(Canvas canvas)
        {
            var screenGo = CreateUiChild(canvas.transform, "ResultScreen");
            Stretch(screenGo.GetComponent<RectTransform>());

            var canvasGroup = screenGo.AddComponent<CanvasGroup>();
            var screen = screenGo.AddComponent<LevelResultScreen>();

            var dim = screenGo.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.65f);

            var panelGo = CreateUiChild(screenGo.transform, "Panel");
            var panelImage = panelGo.AddComponent<Image>();
            panelImage.sprite = LoadSprite("UI_Panel");
            panelImage.color = new Color(0.16f, 0.18f, 0.28f, 0.98f);

            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 560f);

            var title = CreateLabel(panelGo.transform, "Title", "Level complete!", 62f, TextAlignmentOptions.Center);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -70f);
            titleRect.sizeDelta = new Vector2(-60f, 90f);

            var subtitle = CreateLabel(panelGo.transform, "Subtitle", "Level 1 cleared", 38f, TextAlignmentOptions.Center);
            var subtitleRect = subtitle.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0f, 0.5f);
            subtitleRect.anchorMax = new Vector2(1f, 0.5f);
            subtitleRect.anchoredPosition = new Vector2(0f, 20f);
            subtitleRect.sizeDelta = new Vector2(-60f, 70f);

            var button = CreateButton(panelGo.transform, "ActionButton", "Next level", new Color(0.25f, 0.62f, 0.95f));
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 70f);
            buttonRect.sizeDelta = new Vector2(420f, 120f);

            Bind(screen, "canvasGroup", canvasGroup);
            Bind(screen, "panel", panelRect);
            Bind(screen, "titleLabel", title);
            Bind(screen, "subtitleLabel", subtitle);
            Bind(screen, "buttonLabel", button.GetComponentInChildren<TextMeshProUGUI>());
            Bind(screen, "actionButton", button);

            screenGo.SetActive(false);
            return screen;
        }

        private static GameObject BuildCheatPanel(Canvas canvas)
        {
            var panelGo = CreateUiChild(canvas.transform, "CheatPanel");
            Stretch(panelGo.GetComponent<RectTransform>());
            var cheat = panelGo.AddComponent<CheatPanel>();

            var toggle = CreateButton(panelGo.transform, "ToggleButton", "CHEATS", new Color(0.35f, 0.35f, 0.42f));
            var toggleRect = toggle.GetComponent<RectTransform>();
            toggleRect.anchorMin = Vector2.zero;
            toggleRect.anchorMax = Vector2.zero;
            toggleRect.pivot = Vector2.zero;
            toggleRect.anchoredPosition = new Vector2(24f, 24f);
            toggleRect.sizeDelta = new Vector2(220f, 90f);

            var bodyGo = CreateUiChild(panelGo.transform, "Body");
            var bodyImage = bodyGo.AddComponent<Image>();
            bodyImage.sprite = LoadSprite("UI_Panel");
            bodyImage.color = new Color(0.10f, 0.11f, 0.17f, 0.96f);

            var bodyRect = bodyGo.GetComponent<RectTransform>();
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.zero;
            bodyRect.pivot = Vector2.zero;
            bodyRect.anchoredPosition = new Vector2(24f, 130f);
            bodyRect.sizeDelta = new Vector2(620f, 520f);

            var currentLabel = CreateLabel(bodyGo.transform, "CurrentLevel", "Level 1 / 10", 36f, TextAlignmentOptions.Center);
            var currentRect = currentLabel.GetComponent<RectTransform>();
            currentRect.anchorMin = new Vector2(0f, 1f);
            currentRect.anchorMax = new Vector2(1f, 1f);
            currentRect.pivot = new Vector2(0.5f, 1f);
            currentRect.anchoredPosition = new Vector2(0f, -18f);
            currentRect.sizeDelta = new Vector2(-24f, 54f);

            var winButton = CreateButton(bodyGo.transform, "WinButton", "WIN LEVEL", new Color(0.24f, 0.70f, 0.40f));
            var winRect = winButton.GetComponent<RectTransform>();
            winRect.anchorMin = new Vector2(0f, 1f);
            winRect.anchorMax = new Vector2(0.5f, 1f);
            winRect.pivot = new Vector2(0.5f, 1f);
            winRect.anchoredPosition = new Vector2(0f, -86f);
            winRect.offsetMin = new Vector2(18f, -190f);
            winRect.offsetMax = new Vector2(-9f, -86f);

            var loseButton = CreateButton(bodyGo.transform, "LoseButton", "LOSE LEVEL", new Color(0.82f, 0.34f, 0.30f));
            var loseRect = loseButton.GetComponent<RectTransform>();
            loseRect.anchorMin = new Vector2(0.5f, 1f);
            loseRect.anchorMax = new Vector2(1f, 1f);
            loseRect.pivot = new Vector2(0.5f, 1f);
            loseRect.offsetMin = new Vector2(9f, -190f);
            loseRect.offsetMax = new Vector2(-18f, -86f);

            var jumpLabel = CreateLabel(bodyGo.transform, "JumpLabel", "Go to level:", 32f, TextAlignmentOptions.Left);
            var jumpRect = jumpLabel.GetComponent<RectTransform>();
            jumpRect.anchorMin = new Vector2(0f, 1f);
            jumpRect.anchorMax = new Vector2(1f, 1f);
            jumpRect.pivot = new Vector2(0.5f, 1f);
            jumpRect.offsetMin = new Vector2(18f, -222f);
            jumpRect.offsetMax = new Vector2(-18f, -176f);

            var gridGo = CreateUiChild(bodyGo.transform, "LevelGrid");
            var gridRect = gridGo.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 0f);
            gridRect.anchorMax = new Vector2(1f, 1f);
            gridRect.offsetMin = new Vector2(18f, 18f);
            gridRect.offsetMax = new Vector2(-18f, -232f);

            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(100f, 100f);
            grid.spacing = new Vector2(14f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            var levelButtonTemplate = CreateButton(gridGo.transform, "LevelButtonTemplate", "1", new Color(0.30f, 0.44f, 0.72f));
            levelButtonTemplate.gameObject.SetActive(false);

            Bind(cheat, "toggleButton", toggle);
            Bind(cheat, "panelBody", bodyGo);
            Bind(cheat, "winButton", winButton);
            Bind(cheat, "loseButton", loseButton);
            Bind(cheat, "levelButtonContainer", gridGo.transform);
            Bind(cheat, "levelButtonPrefab", levelButtonTemplate);
            Bind(cheat, "currentLevelLabel", currentLabel);

            bodyGo.SetActive(false);
            return panelGo;
        }


        private static GameObject CreateUiChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float size, TextAlignmentOptions alignment)
        {
            var go = CreateUiChild(parent, name);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string text, Color color)
        {
            var go = CreateUiChild(parent, name);

            var image = go.AddComponent<Image>();
            image.sprite = LoadSprite("UI_Panel");
            image.color = color;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var label = CreateLabel(go.transform, "Label", text, 34f, TextAlignmentOptions.Center);
            Stretch(label.GetComponent<RectTransform>());

            return button;
        }

        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{PlaceholderSpriteFactory.SpriteFolder}/{name}.png");
        }

        private static T LoadPrefabComponent<T>(string path) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null ? prefab.GetComponent<T>() : null;
        }

        private static void Bind(Object target, string fieldName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            var property = serialized.FindProperty(fieldName);

            if (property == null)
            {
                Debug.LogError($"[SceneBuilder] {target.GetType().Name} has no serialized field '{fieldName}'.");
                return;
            }

            if (value == null)
            {
                Debug.LogError($"[SceneBuilder] Cannot bind '{fieldName}' on {target.GetType().Name}: " +
                               "the value is null. The asset it points to was not created or not yet " +
                               "imported when the scene was saved.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(target);
            if (target is Component component)
            {
                var scene = component.gameObject.scene;
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }
    }
}
#endif
