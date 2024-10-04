using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;


public enum Label
{
    Enemy,
    Player,
    Projectile,
    Other
}

internal class GameObject
{
    private readonly Dictionary<Type, List<Component>> _components;

    private readonly List<Component> _originalComponents;

    private bool _firstUpdate = true;

    public string OriginalPrefab = "default";

    public Label Label = Label.Other;

    public bool Free;

    public GameObject()
    {
        Id = GameManager.Add();
        _originalComponents = new List<Component>();
        _components = new Dictionary<Type, List<Component>>();
        AddComponent<Transform>();
    }

    public long Id { get; private set; }

    public Transform Transform => GetComponent<Transform>();

    // nullable because not every game object has a rigid body or a collider or a sprite renderer -----
    public SpriteRenderer SpriteRenderer => GetComponent<SpriteRenderer>();
    public RigidBody RigidBody => GetComponent<RigidBody>();
    public BoxCollider BoxCollider => GetComponent<BoxCollider>();

    public void SetColor(Color color)
    {
        if (SpriteRenderer != null)
            SpriteRenderer.Color = color;
    }

    public virtual void Destroy()
    {
        GameManager.QueueDestroy(this);
    }

    public virtual void DestroyInternal()
    {
        if (HasComponent<Player>() && GetComponent<Damageable>().Health <= 0) GameManager.Lose();

        ResetComponentState();

        var level = LevelManager.CurrentLevel;
        level?.RemoveFromGrid(this);
        GameManager.Remove(this);
    }

    public virtual void Delete()
    {
        Destroy();

        var level = LevelManager.CurrentLevel;
        level.MovingObjects.Remove(this);
        level.OriginalPositions.Remove(this);

        if (PrefabManager.PrefabArguments.ContainsKey(this))
            PrefabManager.PrefabArguments.Remove(this);
    }

    public void ResetComponentState()
    {
        var comps = GetComponents().ToList();
        foreach (var comp in comps) comp.FirstUpdate = true;
    }

    public void OnCollision(Collision collision)
    {
        if (!GameManager.LoadedObjects.ContainsValue(this)) return;
        var a = new List<Component>(_components.Values.SelectMany(comps => comps));

        foreach (var comp in a) comp.OnCollision(collision);
    }

    public void OnTrigger(Collision collision)
    {
        if (!GameManager.LoadedObjects.ContainsValue(this)) return;
        var a = new List<Component>(_components.Values.SelectMany(comps => comps));

        foreach (var comp in a) comp.OnTrigger(collision);
    }

    public virtual void Update()
    {
        // we need to make a copy of the list because components can be added during the loop
        var a = new List<Component>(_components.Values.SelectMany(comps => comps));
        foreach (var comp in a)
        {
            if (comp.FirstUpdate)
            {
                comp.Start();
                comp.FirstUpdate = false;
                continue;
            }

            comp.Update();
        }
    }

    public void StartIfNeeded()
    {
        var a = new List<Component>(_components.Values.SelectMany(comps => comps).Where(comp => comp.FirstUpdate));
        if (_firstUpdate)
        {
            _originalComponents.AddRange(a.ToList());
            _firstUpdate = false;
        }

        foreach (var comp in a)
        {
            comp.Start();
            comp.FirstUpdate = false;
        }
    }

    public void FixedUpdate()
    {
        var a = new List<Component>(_components.Values.SelectMany(comps => comps));
        foreach (var comp in a)
        {
            if (comp.FirstUpdate)
            {
                comp.Start();
                comp.FirstUpdate = false;
                continue;
            }

            comp.FixedUpdate();
        }
    }

    public virtual void Draw()
    {
        foreach (var comp in _components.Values.SelectMany(comps => comps))
        {
            if (!(comp is RendererComponent r)) continue;
            r.Render();
        }
    }

    public List<RendererComponent> GetRendererComponents()
    {
        return _components.Values.SelectMany(comps => comps)
            .Where(comp => comp is RendererComponent)
            .Cast<RendererComponent>()
            .ToList();
    }

    public Component AddComponent(Component component)
    {
        component.Parent = this;

        if (!_components.ContainsKey(component.GetType())) _components.Add(component.GetType(), new List<Component>());

        _components[component.GetType()].Add(component);

        GameManager.UpdateObject(this);

        return component;
    }

    public T AddComponent<T>() where T : Component
    {
        var ins = (T)Activator.CreateInstance(typeof(T), null);
        return (T)AddComponent(ins);
    }

    public void RemoveComponent<T>() where T : Component
    {
        if (!_components.ContainsKey(typeof(T))) return;

        var list = _components[typeof(T)];
        list.RemoveAt(0);

        if (list.Count == 0) _components.Remove(typeof(T));

        GameManager.UpdateObject(this);
    }

    public void RemoveComponent(Component component)
    {
        if (!_components.ContainsKey(component.GetType().UnderlyingSystemType)) return;

        var list = _components[component.GetType().UnderlyingSystemType];
        list.Remove(component);

        if (list.Count == 0) _components.Remove(component.GetType().UnderlyingSystemType);

        GameManager.UpdateObject(this);
    }

    public T GetComponent<T>() where T : Component
    {
        return (T)GetComponent(typeof(T));
    }

    public Component GetComponent(Type type)
    {
        if (!_components.ContainsKey(type)) return null;

        var components = _components[type].ToList();
        return components.Count > 0 ? components.First() : null;
    }

    public List<T> GetComponents<T>() where T : Component
    {
        if (!_components.ContainsKey(typeof(T))) return new List<T>();
        return _components[typeof(T)].Cast<T>().ToList();
    }

    public List<Component> GetComponents(params Type[] types)
    {
        return new List<Component>(_components.Values.SelectMany(comps => comps)
            .Where(comp => types.Contains(comp.GetType())));
    }

    public List<Component> GetComponents()
    {
        return new List<Component>(_components.Values.SelectMany(comps => comps));
    }

    public bool HasComponent(Type componentType)
    {
        return _components.ContainsKey(componentType);
    }

    public bool HasComponent<T>() where T : Component
    {
        return HasComponent(typeof(T));
    }


    /// <summary>
    /// Finds the nearest gameobject within the maxDepth [not inclusive] containing any of the labels specified
    /// </summary>
    /// <param name="maxDepth">The grid spaces that we're okay to search in</param>
    /// <param name="labels">The label of what types of gameObject we're searching for</param>
    public FindResult FindNearestObject(float maxDepth, params Label[] labels)
    {
        var level = LevelManager.CurrentLevel;

        var currDepth = 0;
        var searchBounds = Transform.Bounds;
        var gameObjs = level.GetObjectsInSpaces(level.GetGridSpaces(searchBounds));

        var gameObjsSearched = new HashSet<GameObject>();

        var gridWidth = 64;
        var gridHeight = 64;

        GameObject closest = null;
        var minDist = float.PositiveInfinity;

        while (currDepth < maxDepth)
        {
            foreach (var obj in gameObjs
                         .Where(obj => obj != this)
                         .Where(obj => labels.Contains(obj.Label)))
            {
                gameObjsSearched.Add(obj);

                var dist = (Transform.Position - obj.Transform.Position).Length();

                if (!(dist < minDist)) continue;

                minDist = dist;
                closest = obj;
            }

            if (closest != null) break;

            currDepth++;

            // look for more gameObjects
            searchBounds = new Bounds2(
                searchBounds.Position - new Vector2(gridWidth, gridHeight),
                searchBounds.Size + new Vector2(gridWidth, gridHeight)
            );
            gameObjs = level.GetObjectsInSpaces(level.GetGridSpaces(searchBounds))
                .Except(gameObjsSearched).ToList();
        }

        return new FindResult
        {
            Result = closest,
            Distance = minDist
        };
    }


    public struct FindResult
    {
        public bool Found => Result != null;
        public GameObject Result { get; set; }
        public float Distance;
    }
}