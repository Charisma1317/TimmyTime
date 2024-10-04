using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

internal class JsonLevelWriter
{

    public static void ConvertLevelToJson(Level level)
    {
        var num = level.LevelNumber;
        var levelPath = Engine.GetAssetPath("levels/" + num);
        var targetPath = Path.Combine(levelPath, "new-data.json");

        var tilesCount = 0;
        var grid = level.Grid;
        for (var i = 0; i < grid.Height; i++)
            for (var j = 0; j < grid.Width; j++)
            {
                var cell = grid[j, i];
                if (cell.Occupied) tilesCount++;
            }

        tilesCount += level.MovingObjects.Count;

        var data = new BasicLevel
        {
            Name = level.Name,
            Width = grid.Width,
            Height = grid.Height,
            Data = new LevelData[tilesCount]
        };

        var index = 0;
        for (var i = 0; i < grid.Height; i++)
            for (var j = 0; j < grid.Width; j++)
            {
                var cell = grid[j, i];
                if (!cell.Occupied) continue;

                var tile = cell.GameObject;
                var tileLock = TileLockType.Set;

                data.Data[index] = new LevelData
                {
                    TileLock = tileLock,
                    TilePrefab = tile.OriginalPrefab,
                    Params = PrefabManager.GetPrefabParams(tile)
                };

                index++;
            }

        foreach (var obj in level.MovingObjects)
        {
            data.Data[index] = new LevelData
            {
                TileLock = TileLockType.Free,
                TilePrefab = obj.OriginalPrefab,
                Params = PrefabManager.GetPrefabParams(obj)
            };

            index++;
        }

        var dataJson = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(targetPath, dataJson);

        Engine.CopyToBaseDirectory(targetPath);

        Debug.WriteLine($"Level {num} (name={level.Name}) converted to JSON");
    }

    [Obsolete("Only use if necessary-- we switched from text to json")]
    public static void ConvertAllTextToJson()
    {
        foreach (var level in Directory.GetDirectories(Engine.GetAssetPath("levels")))
        {
            var levelNumber = int.Parse(Path.GetFileName(level));
            if (File.Exists(Path.Combine(level, "new-data.json"))) continue;

            ConvertTextToJsonLevel(levelNumber);
        }
    }

    [Obsolete("Only use if necessary-- we switched from text to json")]
    public static void ConvertTextToJsonLevel(int num)
    {
        var level = LevelReader.ReadLevel(num);

        ConvertLevelToJson(level);

        // clear all memory
        level.DestroyInternal();
    }
}