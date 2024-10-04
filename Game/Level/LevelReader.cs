 using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

[Obsolete("Use JsonLevelReader instead")]
internal class LevelReader
{
    private static readonly Dictionary<int, Level> _levels = new Dictionary<int, Level>();

    public static bool HasLevel(int level)
    {
        return _levels.ContainsKey(level);
    }

    public static void UncacheLevel(int level)
    {
        if (!_levels.ContainsKey(level)) return;

        var l = _levels[level];
        l.DestroyInternal();
        _levels.Remove(level);
    }

    public static void CacheLevels()
    {
        foreach (var level in Directory.GetDirectories(Engine.GetAssetPath("levels")))
        {
            var levelNumber = int.Parse(Path.GetFileName(level));
            CacheLevel(levelNumber);
        }
    }

    public static void CacheLevel(int level)
    {
        _levels.Add(level, ReadLevel(level));
    }

    public static Level ReadLevel(int levelNumber)
    {
        if (_levels.TryGetValue(levelNumber, out var readLevel)) return readLevel;

        var folder = Engine.GetAssetPath("levels");
        if (!Directory.Exists(folder)) throw new Exception("Levels folder not found");

        folder = Path.Combine(folder, $"{levelNumber}");
        if (!Directory.Exists(folder)) throw new Exception($"Level {levelNumber} not found");

        var dataJson = File.ReadAllText(Path.Combine(folder, "data.json"));
        var data = JsonConvert.DeserializeObject<LevelData>(dataJson);

        var levelMappingsJson = File.ReadAllText(Path.Combine(folder, $"{data.TileMappings}"));
        var levelMappings = JsonConvert.DeserializeObject<LevelMappings>(levelMappingsJson);

        var grid = new Grid(data.GridWidth, data.GridHeight);
        var level = new Level(levelNumber, grid);

        // go line by line in tile set
        var levelTileSet = File.ReadAllLines(Path.Combine(folder, $"{data.TileSet}"));

        var y = 0;
        foreach (var line in levelTileSet)
        {
            // each character in line is a tile

            Grid.GridCell leftSimpleBound = null;
            Grid.GridCell simpleMovingObj = null;
            string objType = null;
            var len = 0;

            var x = 0;
            for (var z = 0; z < line.Length; z++)
            {
                var c = line[z].ToString();
                var currCell = grid[x, y];
                switch (c)
                {
                    case " ":
                        // empty tile
                        break;
                    // bounded tile
                    case "(":
                        // left simple bound
                        leftSimpleBound = currCell;
                        break;
                    case "F":
                        if (simpleMovingObj == null) simpleMovingObj = currCell;

                        objType = c;
                        len++;

                        if (leftSimpleBound == null)
                        {
                            // this platform does not move
                            var isNextF = x + 1 < line.Length && line[x + 1] == 'F';
                            if (isNextF) break;

                            var obj = PrefabManager.Create(levelMappings.Mappings[objType], simpleMovingObj.Position,
                                len, null);
                            level.MovingObjects.Add(obj);
                            level.OriginalPositions.Add(obj, obj.Transform.Position);

                            simpleMovingObj = null;
                            objType = null;
                            len = 0;
                        }

                        break;
                    case "<":
                    case ">":
                        var right = c == ">";
                        var l = x + 1;
                        while (l < line.Length && line[l] == line[z]) l++;

                        var mapping = levelMappings.Mappings[c];
                        mapping = mapping.Replace("Left", "");
                        mapping = mapping.Replace("Right", "");

                        var conveyer = PrefabManager.Create(mapping, currCell.Position, l - x, right);

                        level.MovingObjects.Add(conveyer);
                        level.OriginalPositions.Add(conveyer, conveyer.Transform.Position);

                        x = l - 1;
                        z = l - 1;
                        break;
                    case "B":
                        simpleMovingObj = currCell;
                        objType = c;
                        break;
                    // right simple bound
                    case ")" when leftSimpleBound == null || simpleMovingObj == null:
                        throw new Exception("Invalid bounded object.");
                    // create tile
                    case ")":
                    {
                        var movingEnemy = simpleMovingObj;
                        var leftBound = leftSimpleBound;

                        GameObject obj;
                        if (len > 0)
                            // this is a moving platform
                            obj = PrefabManager.Create(levelMappings.Mappings[objType], movingEnemy.Position, len,
                                new MovingBounds { Left = leftBound.Position, Right = currCell.Position });
                        else
                            // this is a moving enemy
                            obj = PrefabManager.Create(levelMappings.Mappings[objType], movingEnemy.Position,
                                new MovingBounds { Left = leftBound.Position, Right = currCell.Position });

                        level.MovingObjects.Add(obj);
                        level.OriginalPositions.Add(obj, obj.Transform.Position);

                        leftSimpleBound = null;
                        simpleMovingObj = null;
                        objType = null;
                        len = 0;
                        break;
                    }
                    default:
                    {
                        var tile = CreateTile(grid[x, y].Position, c, levelMappings);

                        // detach any moving entities from this the tilemap
                        if (!tile.HasComponent<MovingEntity>())
                        {
                            currCell.GameObject = tile;
                        }
                        else
                        {
                            level.MovingObjects.Add(tile);
                            level.OriginalPositions.Add(tile, tile.Transform.Position);
                        }

                        break;
                    }
                }

                x++;
            }

            y++;
        }

        // print grid
        Debug.WriteLine(level.ToString());

        level.LoadColliders();

        return level;
    }

    private static GameObject CreateTile(Vector2 pos, string tileChar, LevelMappings mappings)
    {
        if (!mappings.Mappings.TryGetValue(tileChar, out var prefabName))
            throw new Exception($"Tile mapping for {tileChar} not found.");

        return PrefabManager.Create(prefabName, pos);
    }

    private struct LevelMappings
    {
        public Dictionary<string, string> Mappings;
    }

    private struct LevelData
    {
        public string Name;
        public int GridWidth;
        public int GridHeight;
        public string TileSet;
        public string TileMappings;
    }
}