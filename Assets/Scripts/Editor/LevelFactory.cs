#if UNITY_EDITOR
using System.Collections.Generic;
using Match3.Core.Domain;
using UnityEditor;
using UnityEngine;

namespace Match3.EditorTools
{
    public static class LevelFactory
    {
        public const string LevelFolder = "Assets/Data/Levels";
        public const string DatabasePath = "Assets/Data/LevelDatabase.asset";

        public static LevelDatabase BuildAll()
        {
            System.IO.Directory.CreateDirectory(LevelFolder);

            var builders = new List<System.Func<LevelConfig, LevelConfig>>
            {
                BuildLevel01, BuildLevel02, BuildLevel03, BuildLevel04, BuildLevel05,
                BuildLevel06, BuildLevel07, BuildLevel08, BuildLevel09, BuildLevel10,
            };

            var levels = new List<LevelConfig>(builders.Count);

            for (int i = 0; i < builders.Count; i++)
            {
                string path = $"{LevelFolder}/Level_{(i + 1):00}.asset";

                var asset = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
                bool isNew = asset == null;
                if (isNew)
                {
                    asset = ScriptableObject.CreateInstance<LevelConfig>();
                }

                builders[i](asset);

                if (isNew)
                {
                    AssetDatabase.CreateAsset(asset, path);
                }
                else
                {
                    EditorUtility.SetDirty(asset);
                }

                levels.Add(asset);
            }

            var database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
            bool databaseIsNew = database == null;
            if (databaseIsNew)
            {
                database = ScriptableObject.CreateInstance<LevelDatabase>();
            }

            database.EditorSetLevels(levels);

            if (databaseIsNew)
            {
                AssetDatabase.CreateAsset(database, DatabasePath);
            }
            else
            {
                EditorUtility.SetDirty(database);
            }

            AssetDatabase.SaveAssets();
            return database;
        }


        private static LevelConfig BuildLevel01(LevelConfig asset)
        {
            asset.EditorConfigure("Level 1", 8, 8, 4, 22,
                new List<LevelGoal>
                {
                    LevelGoal.Color_(TileColor.Red, 25),
                    LevelGoal.Color_(TileColor.Blue, 25),
                },
                new List<LevelCell>());
            return asset;
        }

        private static LevelConfig BuildLevel02(LevelConfig asset)
        {
            asset.EditorConfigure("Level 2", 8, 8, 5, 22,
                new List<LevelGoal>
                {
                    LevelGoal.Color_(TileColor.Green, 30),
                    LevelGoal.Color_(TileColor.Yellow, 30),
                },
                new List<LevelCell>());
            return asset;
        }

        private static LevelConfig BuildLevel03(LevelConfig asset)
        {
            var cells = new List<LevelCell>();
            cells.Add(Booster(2, 3, BoosterType.LineHorizontal, TileColor.Red));
            cells.Add(Booster(6, 4, BoosterType.LineVertical, TileColor.Blue));

            asset.EditorConfigure("Level 3", 8, 8, 5, 24,
                new List<LevelGoal>
                {
                    LevelGoal.Booster_(BoosterType.LineHorizontal, 2),
                    LevelGoal.Booster_(BoosterType.LineVertical, 2),
                    LevelGoal.Color_(TileColor.Purple, 25),
                },
                cells);
            return asset;
        }

        private static LevelConfig BuildLevel04(LevelConfig asset)
        {
            var cells = new List<LevelCell>();
            for (int x = 3; x <= 5; x++)
            {
                for (int y = 3; y <= 5; y++)
                {
                    cells.Add(Block(x, y, new BlockLayerData(BlockKind.Crate)));
                }
            }

            asset.EditorConfigure("Level 4", 9, 9, 5, 24,
                new List<LevelGoal>
                {
                    LevelGoal.Block_(BlockKind.Crate, 9),
                },
                cells);
            return asset;
        }

        private static LevelConfig BuildLevel05(LevelConfig asset)
        {
            var cells = new List<LevelCell>();
            cells.Add(Block(0, 4, new BlockLayerData(BlockKind.Blocker)));
            cells.Add(Block(8, 4, new BlockLayerData(BlockKind.Blocker)));
            cells.Add(Block(4, 0, new BlockLayerData(BlockKind.Blocker)));
            cells.Add(Block(4, 8, new BlockLayerData(BlockKind.Blocker)));

            asset.EditorConfigure("Level 5", 9, 9, 5, 26,
                new List<LevelGoal>
                {
                    LevelGoal.Booster_(BoosterType.Bomb, 2),
                    LevelGoal.Color_(TileColor.Red, 35),
                },
                cells);
            return asset;
        }

        private static LevelConfig BuildLevel06(LevelConfig asset)
        {
            var cells = new List<LevelCell>();
            cells.Add(Block(2, 2, new BlockLayerData(BlockKind.Crate, TileColor.None, 3)));
            cells.Add(Block(6, 2, new BlockLayerData(BlockKind.Crate, TileColor.None, 3)));
            cells.Add(Block(2, 6, new BlockLayerData(BlockKind.Crate, TileColor.None, 3)));
            cells.Add(Block(6, 6, new BlockLayerData(BlockKind.Crate, TileColor.None, 3)));
            cells.Add(Block(4, 4, new BlockLayerData(BlockKind.Crate, TileColor.None, 5)));

            asset.EditorConfigure("Level 6", 9, 9, 5, 26,
                new List<LevelGoal>
                {
                    LevelGoal.Block_(BlockKind.Crate, 5),
                },
                cells);
            return asset;
        }

        private static LevelConfig BuildLevel07(LevelConfig asset)
        {
            var cells = new List<LevelCell>();
            cells.Add(Block(1, 4, new BlockLayerData(BlockKind.ColoredCrate, TileColor.Red, 2)));
            cells.Add(Block(3, 4, new BlockLayerData(BlockKind.ColoredCrate, TileColor.Blue, 2)));
            cells.Add(Block(5, 4, new BlockLayerData(BlockKind.ColoredCrate, TileColor.Green, 2)));
            cells.Add(Block(7, 4, new BlockLayerData(BlockKind.ColoredCrate, TileColor.Yellow, 2)));

            asset.EditorConfigure("Level 7", 9, 9, 4, 28,
                new List<LevelGoal>
                {
                    LevelGoal.Block_(BlockKind.ColoredCrate, 4),
                },
                cells);
            return asset;
        }

        private static LevelConfig BuildLevel08(LevelConfig asset)
        {
            var cells = new List<LevelCell>();
            cells.Add(Block(2, 3, new BlockLayerData(BlockKind.ChameleonCrate, TileColor.Red, 2)));
            cells.Add(Block(6, 3, new BlockLayerData(BlockKind.ChameleonCrate, TileColor.Blue, 2)));
            cells.Add(Block(4, 6, new BlockLayerData(BlockKind.ChameleonCrate, TileColor.Green, 2)));

            asset.EditorConfigure("Level 8", 9, 9, 4, 28,
                new List<LevelGoal>
                {
                    LevelGoal.Block_(BlockKind.ChameleonCrate, 3),
                    LevelGoal.Booster_(BoosterType.Plane, 1),
                },
                cells);
            return asset;
        }

        private static LevelConfig BuildLevel09(LevelConfig asset)
        {
            var cells = new List<LevelCell>();

            int size = 9;
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    cells.Add(Hole(x, y));
                    cells.Add(Hole(size - 1 - x, y));
                    cells.Add(Hole(x, size - 1 - y));
                    cells.Add(Hole(size - 1 - x, size - 1 - y));
                }
            }

            cells.Add(Block(4, 4,
                new BlockLayerData(BlockKind.Crate, TileColor.None, 2),
                new BlockLayerData(BlockKind.ColoredCrate, TileColor.Purple, 1)));
            cells.Add(Block(3, 5, new BlockLayerData(BlockKind.Crate, TileColor.None, 2)));
            cells.Add(Block(5, 3, new BlockLayerData(BlockKind.Crate, TileColor.None, 2)));

            asset.EditorConfigure("Level 9", size, size, 5, 30,
                new List<LevelGoal>
                {
                    LevelGoal.Block_(BlockKind.Crate, 3),
                    LevelGoal.Block_(BlockKind.ColoredCrate, 1),
                    LevelGoal.Color_(TileColor.Orange, 30),
                },
                cells,
                new List<TileColor>
                {
                    TileColor.Red,
                    TileColor.Blue,
                    TileColor.Green,
                    TileColor.Purple,
                    TileColor.Orange,
                });
            return asset;
        }

        private static LevelConfig BuildLevel10(LevelConfig asset)
        {
            var cells = new List<LevelCell>();

            for (int y = 2; y <= 6; y += 2)
            {
                cells.Add(Block(3, y, new BlockLayerData(BlockKind.Blocker)));
                cells.Add(Block(6, y, new BlockLayerData(BlockKind.Blocker)));
            }

            cells.Add(Block(1, 8,
                new BlockLayerData(BlockKind.Crate, TileColor.None, 2),
                new BlockLayerData(BlockKind.ColoredCrate, TileColor.Red, 1),
                new BlockLayerData(BlockKind.ChameleonCrate, TileColor.Blue, 1)));

            cells.Add(Block(8, 8,
                new BlockLayerData(BlockKind.Crate, TileColor.None, 2),
                new BlockLayerData(BlockKind.ColoredCrate, TileColor.Green, 1)));

            cells.Add(Block(4, 1, new BlockLayerData(BlockKind.ChameleonCrate, TileColor.Yellow, 2)));
            cells.Add(Block(5, 1, new BlockLayerData(BlockKind.ColoredCrate, TileColor.Purple, 2)));

            asset.EditorConfigure("Level 10", 10, 10, 6, 32,
                new List<LevelGoal>
                {
                    LevelGoal.Booster_(BoosterType.RainbowBall, 1),
                    LevelGoal.Block_(BlockKind.Crate, 2),
                    LevelGoal.Block_(BlockKind.ColoredCrate, 3),
                    LevelGoal.Block_(BlockKind.ChameleonCrate, 2),
                },
                cells);
            return asset;
        }


        private static LevelCell Hole(int x, int y)
        {
            return new LevelCell { x = x, y = y, content = CellContent.Hole };
        }

        private static LevelCell Booster(int x, int y, BoosterType booster, TileColor color)
        {
            return new LevelCell
            {
                x = x,
                y = y,
                content = CellContent.Booster,
                booster = booster,
                chipColor = color
            };
        }

        private static LevelCell Block(int x, int y, params BlockLayerData[] layers)
        {
            return new LevelCell
            {
                x = x,
                y = y,
                content = CellContent.Block,
                blockLayers = new List<BlockLayerData>(layers)
            };
        }
    }
}
#endif
