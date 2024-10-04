using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;

internal class PrefabManager
{
    public static Dictionary<GameObject, object[]> PrefabArguments = new Dictionary<GameObject, object[]>();
    public static Dictionary<string, MethodInfo> Prefabs { get; private set; }
    public static Dictionary<string, Prefab> PrefabAttributes { get; private set; }


    public static Dictionary<string, object> GetPrefabParams(string prefabName)
    {
        if (!Prefabs.ContainsKey(prefabName)) throw new Exception($"Prefab {prefabName} not found");

        var method = Prefabs[prefabName];
        var parameters = method.GetParameters();

        var result = new Dictionary<string, object>();
        foreach (var p in parameters)
        {
            // capitalize first letter
            var name = p.Name.First().ToString().ToUpper() + p.Name.Substring(1);
            var type = p.ParameterType.ToString();
            result.Add(name + "$" + type, p.DefaultValue);
        }

        return result;
    }

    public static bool HasArgument(string prefabName, string argumentName, Type argumentType)
    {
        if (!Prefabs.ContainsKey(prefabName)) throw new Exception($"Prefab {prefabName} not found");

        var parameters = GetPrefabParams(prefabName);

        return parameters.ContainsKey(argumentName + "$" + argumentType.ToString());
    }

    public static Dictionary<string, object> GetPrefabParams(GameObject obj)
    {
        var prefabName = obj.OriginalPrefab;
        if (!Prefabs.ContainsKey(prefabName)) throw new Exception($"Prefab {prefabName} not found");

        if (!PrefabArguments.ContainsKey(obj))
            throw new Exception($"Prefab {prefabName} not found in creation registry");

        var method = Prefabs[prefabName];
        var parameters = method.GetParameters();

        var result = new Dictionary<string, object>();

        var args = PrefabArguments[obj];

        var i = 0;
        foreach (var p in parameters)
        {
            // capitalize first letter
            var name = p.Name.First().ToString().ToUpper() + p.Name.Substring(1);
            var type = p.ParameterType.ToString();
            var value = args[i];

            result.Add(name + "$" + type, value);
            i++;
        }

        return result;
    }


    public static void CachePrefabs()
    {
        Prefabs = new Dictionary<string, MethodInfo>();
        PrefabAttributes = new Dictionary<string, Prefab>();

        var types = typeof(PrefabManager).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes(typeof(Prefab), false).Length > 0)
            .ToList();

        foreach (var m in types)
        {
            var prefab = m.GetCustomAttribute<Prefab>();
            var typeAttribute = prefab.ComponentType.Name;
            Prefabs.Add(typeAttribute, m);
            PrefabAttributes.Add(typeAttribute, prefab);

            Debug.WriteLine($"Cached prefab {typeAttribute}");
        }
    }

    public static GameObject Create(string prefabName, params object[] args)
    {
        if (!Prefabs.ContainsKey(prefabName)) throw new Exception($"Prefab {prefabName} not found");

        var method = Prefabs[prefabName];

        // required args
        var parameters = method.GetParameters();
        if (parameters.Length != args.Length)
        {
            // add default values for ONLY missing args
            var newArgs = new List<object>(args);
            var c = 0;
            foreach (var p in parameters)
            {
                if (c < args.Length)
                {
                    c++;
                    continue;
                }

                if (newArgs.Count == parameters.Length) break;

                if (newArgs.Count > parameters.Length)
                    throw new Exception($"Too many arguments for prefab {prefabName}");

                newArgs.Add(GetDefault(p.ParameterType));
            }

            args = newArgs.ToArray();
        }

        var obj = (GameObject)method.Invoke(null, args);
        obj.OriginalPrefab = prefabName;

        var free = obj.HasComponent<MovingEntity>() || (obj.HasComponent<RigidBody>() && !obj.RigidBody.IsStatic);
        obj.Free = free || PrefabAttributes[prefabName].OverrideFree;

        PrefabArguments.Add(obj, args);

        return obj;
    }

    // creates a gameobject with the current prefab arguments it was created with
    // & destroys the old one
    // returns the new object
    public static GameObject UpdateObject(GameObject obj)
    {
        if (!PrefabArguments.ContainsKey(obj))
            throw new Exception($"Prefab {obj.OriginalPrefab} not found in creation registry");

        var args = PrefabArguments[obj];
        var prefab = obj.OriginalPrefab;
        var method = Prefabs[prefab];

        var free = obj.Free;

        obj.Delete();
        obj = (GameObject)method.Invoke(null, args);

        obj.OriginalPrefab = prefab;
        obj.Free = free;
        PrefabArguments.Add(obj, args);

        if (obj.Free)
        {
            LevelManager.CurrentLevel.MovingObjects.Add(obj);
            LevelManager.CurrentLevel.OriginalPositions.Add(obj, obj.Transform.Position);
        }
        else
        {
            var gridTileWidth = Grid.TileSizeX;
            var gridTileHeight = Grid.TileSizeY;
            var pos = obj.Transform.Position;

            var gridX = (int)(pos.X / gridTileWidth);
            var gridY = (int)(pos.Y / gridTileHeight);

            if (gridX < 0 || gridX >= LevelManager.CurrentLevel.Grid.Width ||
                gridY < 0 || gridY >= LevelManager.CurrentLevel.Grid.Height)
                throw new Exception($"Object {obj} is out of bounds");

            var cell = LevelManager.CurrentLevel.Grid[gridX, gridY];
            cell.GameObject = obj;
        }

        GameManager.Load(obj);

        return obj;
    }

    public static object GetDefault(Type type)
    {
        return Activator.CreateInstance(type);
    }
}