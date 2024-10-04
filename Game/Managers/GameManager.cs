using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

internal class GameManager
{
    // entities that are loaded & need to be updated every frame
    public static readonly Dictionary<long, GameObject> LoadedObjects = new Dictionary<long, GameObject>();

    public static readonly Dictionary<Type, List<GameObject>> LoadedObjectsByType =
        new Dictionary<Type, List<GameObject>>();

    public static readonly Dictionary<Label, List<GameObject>> LoadedObjectsByLabel =
        new Dictionary<Label, List<GameObject>>();

    public static GameObject MainCamera;

    private static float FixedUpdateTimer;
    private static long IdCounter;

    public static readonly List<Collider> Colliders = new List<Collider>();

    private static readonly HashSet<GameObject> _objectsToLoad = new HashSet<GameObject>();

    private static readonly Queue<GameObject> _objectsToDestroy = new Queue<GameObject>();

    public static Game.GameResult GameResult { get; set; } = Game.GameResult.Unknown;

    public static void UpdateObjects()
    {
        if (_objectsToDestroy.Count > 0)
        {
            while (_objectsToDestroy.Count > 0)
            {
                var gameObject = _objectsToDestroy.Dequeue();
                gameObject.DestroyInternal();
            }
        }

        if (_objectsToLoad.Count > 0)
            _loadAll();

        HandleInputs();

        var loadedObjectsCopy = LoadedObjects.Values.ToList();

        StartObjects(loadedObjectsCopy);
        FixedUpdate(loadedObjectsCopy);
        UpdateObjects(loadedObjectsCopy);
    }

    public static void Draw()
    {
        // foreach (var loadedObjectsValue in LoadedObjects.Values)
        // {
        //     loadedObjectsValue.Draw();
        // }
        GetObjectsWithComponent<EditorCamera>().ToList().ForEach(camera => camera.Draw());
        GetObjectsWithComponent<Camera>().ToList().ForEach(camera => camera.Draw());
    }

    public static long Add()
    {
        return IdCounter++;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void Load(GameObject gameObject)
    {
        _objectsToLoad.Add(gameObject);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private static void _loadAll()
    {
        foreach (var gameObject in _objectsToLoad)
        {
            LoadedObjects.Add(gameObject.Id, gameObject);
            UpdateObject(gameObject);
        }

        _objectsToLoad.Clear();
    }

    private static void StartObjects(List<GameObject> loadedObjectsCopy)
    {
        // start all entities
        loadedObjectsCopy.AsParallel().ForAll(gameObject => { gameObject.StartIfNeeded(); });
    }

    public static void UpdateObject(GameObject gameObject)
    {
        if (!LoadedObjects.ContainsKey(gameObject.Id)) return;

        if (LoadedObjectsByLabel.TryGetValue(gameObject.Label, out var s))
        {
            s.Add(gameObject);
        }
        else
        {
            LoadedObjectsByLabel.Add(gameObject.Label, new List<GameObject> { gameObject });
        }

        var loadedObjectsByType = LoadedObjectsByType.ToList();

        // remove old components
        foreach (var (type, set) in loadedObjectsByType)
        {
            if (gameObject.HasComponent(type)) continue;
            set.Remove(gameObject);
        }

        // populate new components
        foreach (var component in gameObject.GetComponents())
        {
            if (!LoadedObjectsByType.TryGetValue(component.GetType(), out var set))
            {
                set = new List<GameObject>();
                LoadedObjectsByType.Add(component.GetType(), set);
            }

            if (component is Collider collider) Colliders.Add(collider);

            set.Add(gameObject);
        }

        if ((gameObject.HasComponent<EditorCamera>() || gameObject.HasComponent<Camera>()) && MainCamera == null)
            MainCamera = gameObject;
    }

    public static void QueueDestroy(GameObject gameObject)
    {
        _objectsToDestroy.Enqueue(gameObject);
    }

    public static void Remove(GameObject gameObject)
    {
        LoadedObjects.Remove(gameObject.Id);
        foreach (var component in gameObject.GetComponents())
        {
            if (LoadedObjectsByType.TryGetValue(component.GetType(), out var set))
                set.Remove(gameObject);
            if (component is Collider col) Colliders.Remove(col);
        }

        LoadedObjectsByLabel[gameObject.Label].Remove(gameObject);
    }

    public static ReadOnlyCollection<GameObject> GetObjectsWithComponent<T>() where T : Component
    {
        return LoadedObjectsByType.TryGetValue(typeof(T), out var set)
            ? set.AsReadOnly()
            : new List<GameObject>().AsReadOnly();
    }

    public static List<Collider> GetCollidersWithinBounds(Bounds2 bounds)
    {
        return Colliders.AsParallel().Where(collider => collider.AABB().Overlaps(bounds)).ToList();
    }

    public static List<GameObject> GetObjectsWithinBounds(Bounds2 bounds)
    {
        var objects = LoadedObjects.Values;
        var list = objects.AsParallel().Where(gameObject => gameObject.Transform.Bounds.Overlaps(bounds)).ToList();
        return list;
    }

    private static void HandleInputs()
    {
        if (Engine.GetKeyHeld(Key.LeftControl) && Engine.GetKeyDown(Key.E))
        {
            Win();
            return;
        }

        if (Game.CurrentScreenState != Game.ScreenState.Game) return;

        
        if (Engine.GetKeyHeld(Key.LeftControl) && Engine.GetKeyDown(Key.I)) LevelManager.LoadLevel(0);
        else if (Engine.GetKeyHeld(Key.LeftControl) && Engine.GetKeyDown(Key.R))
            LevelManager.ReloadLevel(LevelManager.CurrentLevel.LevelNumber);
        else if (Engine.GetKeyHeld(Key.LeftControl) && Engine.GetKeyDown(Key.N)) LevelManager.LoadNextLevel();
        else if (Engine.GetKeyDown(Key.U)) LevelManager.LoadLevel(1);
        else if (Engine.GetKeyDown(Key.I)) LevelManager.LoadLevel(2);
        else if (Engine.GetKeyDown(Key.O)) LevelManager.LoadLevel(3);
        else if (Engine.GetKeyDown(Key.P)) LevelManager.LoadLevel(4);
    }

    private static void FixedUpdate(List<GameObject> loadedObjectsCopy)
    {
        FixedUpdateTimer += Engine.TimeDelta;
        while (FixedUpdateTimer >= Physics.IdealFixedDeltaTime)
        {
            loadedObjectsCopy.AsParallel().ForAll(entity => entity.FixedUpdate());
            FixedUpdateTimer -= Physics.IdealFixedDeltaTime;
        }
    }

    private static void UpdateObjects(List<GameObject> loadedObjectsCopy)
    {
        // update all entities
        loadedObjectsCopy.AsParallel().ForAll(gameObject => { gameObject.Update(); });
    }

    public static void Clear()
    {
        foreach (var loadedObject in LoadedObjects.Values)
            loadedObject.ResetComponentState();

        LoadedObjects.Clear();
        LoadedObjectsByType.Clear();
        Colliders.Clear();
        MainCamera = null;
    }

    public static void Lose()
    {
        GameResult = Game.GameResult.Defeat;
        Game.Instance.EndScreen();
    }

    public static void Win()
    {
        GameResult = Game.GameResult.Win;
        Game.Instance.EndScreen();
    }
}