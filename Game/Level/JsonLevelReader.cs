using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

internal class JsonLevelReader
{
    private static readonly Dictionary<int, Level> _levels = new Dictionary<int, Level>();

    public static void ClearCache()
    {
        foreach (var level in _levels.Values)
        {
            level.DestroyInternal();
        }

        _levels.Clear();
    }
    public static Dictionary<int, string> GetLevelNames()
    {
        var names = new Dictionary<int, string>();
        foreach (var level in Directory.GetDirectories(Engine.GetAssetPath("levels")))
        {
            var levelNumber = int.Parse(Path.GetFileName(level));
            var dataJson = File.ReadAllText(Path.Combine(level, "new-data.json"));
            var data = JsonConvert.DeserializeObject<BasicLevel>(dataJson);
            names.Add(levelNumber, data.Name);
        }

        return names;
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
        if (_levels.ContainsKey(level)) return;

        var l = ReadLevel(level);
        _levels.Add(level, l);
        l.Grid.Updated = true;
    }

    public static bool HasLevel(int level)
    {
        return _levels.ContainsKey(level);
    }

    public static void DeleteLevel(int level)
    {
        if (!_levels.ContainsKey(level)) return;

        var l = _levels[level];
        l.DestroyInternal();
        _levels.Remove(level);
    }

    public static Level ReadLevel(int num)
    {
        if (_levels.TryGetValue(num, out var readLevel)) return readLevel;

        var folder = Engine.GetAssetPath("levels/" + num);
        var dataJson = File.ReadAllText(Path.Combine(folder, "new-data.json"));

        var data = JsonConvert.DeserializeObject<BasicLevel>(dataJson);

        var level = new Level(num, new Grid(data.Width, data.Height))
        {
            Name = data.Name
        };

        var grid = level.Grid;

        foreach (var tileData in data.Data)
        {
            var tileLock = tileData.TileLock;
            var tilePrefab = tileData.TilePrefab;
            JObject tileParams = tileData.Params;

            var parsedParams = new object[tileParams.Count];

            var pos = new Vector2(-1, -1);

            var k = 0;
            foreach (var p in tileParams.Properties())
            {
                var typeName = p.Name.Split('$')[1];
                var type = Type.GetType(typeName);

                if (type == null)
                {
                    Debug.WriteLine($"Type {typeName} not found");
                    k++;
                    continue;
                }

                if (p.Name.Split("$")[0].Equals("Position")) pos = p.Value.ToObject<Vector2>();

                var value = p.Value.ToObject(type);
                parsedParams[k++] = value;
            }

            if (pos == new Vector2(-1, -1)) throw new Exception($"Position property not found for {tilePrefab}");

            var cell = grid[(int)(pos.X / Grid.TileSizeX), (int)(pos.Y / Grid.TileSizeY)];

            var obj = PrefabManager.Create(tilePrefab, parsedParams);
            if (tileLock == TileLockType.Set)
            {
                cell.GameObject = obj;
            }
            else
            {
                level.MovingObjects.Add(obj);
                level.OriginalPositions.Add(obj, obj.Transform.Position);
            }
        }

        Debug.WriteLine(level.ToString());

        return level;
    }
}

internal struct BasicLevel
{
    public string Name;
    public int Width;
    public int Height;
    public LevelData[] Data;
}

internal struct LevelData
{
    public TileLockType TileLock;
    public string TilePrefab;
    public dynamic Params;
}

[JsonConverter(typeof(StringEnumConverter))]
internal enum TileLockType
{
    Free,
    Set
}