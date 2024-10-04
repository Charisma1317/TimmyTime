using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using QuikGraph;
using static Grid;

internal struct NodeConnection
{
    public NodeRecord FromNode;
    public NodeRecord ToNode;
    public float Cost { get; set; }

    public NodeConnection(NodeRecord from, NodeRecord to)
    {
        FromNode = from;
        ToNode = to;
        Cost = 0;
    }
}

internal struct NodeRecord
{
    public GridCell Node;
    public List<NodeConnection> Connections;
    public float CostSoFar;
    public float EstimatedTotalCost;
}

internal struct PathRecord
{
    public NodeRecord Record;
    public NodeConnection Connection;
}

internal struct CellCheckData
{
    public GridCell Cell;
    public GameObject Attached;
    public GridCell[] EdgeData;
}

internal class Level : GameObject
{
    //private readonly HashSet<GridCell> _edgesColored = new HashSet<GridCell>();

    public readonly Dictionary<GameObject, CompositeCollider> CompositeColliders =
        new Dictionary<GameObject, CompositeCollider>();

    public readonly int UniformGridHeight;

    public readonly int UniformGridWidth;
    public Queue<CellCheckData> CellsToCheck = new Queue<CellCheckData>();

    public AdjacencyGraph<GridCell, Edge<GridCell>> Graph;

    public Level(int levelNumber, Grid grid)
    {
        LevelNumber = levelNumber;
        Grid = grid;

        UniformGridWidth = 5 + grid.Width / 2;
        UniformGridHeight = 5 + grid.Height / 2;

        UniformGrid = new UniformGridCell[UniformGridWidth, UniformGridHeight];
        ObjectsOnGrid = new Dictionary<GameObject, HashSet<UniformGridCell>>();
        Graph = new AdjacencyGraph<GridCell, Edge<GridCell>>();
    }

    public string Name { get; set; }
    public int LevelNumber { get; set; }
    public float Score { get; set; } = 0;
    public Grid Grid { get; }

    public UniformGridCell[,] UniformGrid { get; }

    public Dictionary<GameObject, HashSet<UniformGridCell>> ObjectsOnGrid { get; }

    public List<GameObject> MovingObjects { get; set; } = new List<GameObject>();

    public Dictionary<GameObject, Vector2> OriginalPositions { get; set; } = new Dictionary<GameObject, Vector2>();

    public override void DestroyInternal()
    {
        var movingObjectsCopy = new List<GameObject>(MovingObjects);

        foreach (var obj in Grid)
        {
            if (!obj.Occupied) continue;
            obj.GameObject.DestroyInternal();
        }

        foreach (var obj in movingObjectsCopy)
        {
            PrefabManager.PrefabArguments.Remove(obj);
            obj.DestroyInternal();
        }

        ObjectsOnGrid.Clear();
        MovingObjects.Clear();
        OriginalPositions.Clear();
        CompositeColliders.Clear();

        GameManager.Remove(this);
        if (PrefabManager.PrefabArguments.ContainsKey(this))
            PrefabManager.PrefabArguments.Remove(this);
    }

    public Vector2 ClosestPositionOnGrid(Vector2 point)
    {
        if (OriginalPositions.Count == 0) return new Vector2(-1, -1);
        var min = OriginalPositions.First().Value;
        var minDist = (point - min).Length();

        foreach (var (obj, pos) in OriginalPositions)
        {
            var dist = (point - pos).Length();
            if (dist > minDist) continue;

            min = pos;
            minDist = dist;
        }

        return min;
    }

    public GridCell ClosestCellFromPosition(Vector2 point)
    {
        var x = (int)point.X / TileSizeX;
        var y = (int)point.Y / TileSizeY;
        if (x < 0 || y < 0) return null;

        if (x >= Grid.Width || y >= Grid.Height) return null;

        return Grid[x, y];
    }

    public List<GridCell> CellsFromBounds(Bounds2 bounds)
    {
        var cells = new List<GridCell>();

        var topLeftX = (int)(bounds.Min.X / TileSizeX);
        var topLeftY = (int)(bounds.Min.Y / TileSizeY);

        var bottomRightX = (int)(bounds.Max.X / TileSizeX);
        var bottomRightY = (int)(bounds.Max.Y / TileSizeY);

        cells.Add(Grid[topLeftX, topLeftY]);

        if (topLeftX != bottomRightX)
            // no corr needed on x
            for (var x = topLeftX + 1; x <= bottomRightX; x++)
                cells.Add(Grid[x, topLeftY]);

        if (topLeftY != bottomRightY)
            // no corr needed on y
            for (var y = topLeftY + 1; y <= bottomRightY; y++)
                cells.Add(Grid[topLeftX, y]);

        return cells;
    }

    public void EditMode()
    {
        // IRREVERSIBLE
        foreach (var obj in Grid)
        {
            if (!obj.Occupied) continue;

            // rip all components except transform and sprite renderer
            var components = obj.GameObject.GetComponents();
            foreach (var component in components)
            {
                if (component is Transform || component is SpriteRenderer) continue;
                obj.GameObject.RemoveComponent(component);
            }
        }

        foreach (var obj in MovingObjects)
        {
            // rip all components except transform and sprite renderer
            var components = obj.GetComponents();
            foreach (var component in components)
            {
                if (component is Transform || component is SpriteRenderer) continue;
                obj.RemoveComponent(component);
            }
        }
    }

    public void ResetUniformGrid()
    {
        for (var y = 0; y < UniformGridHeight; y++)
            for (var x = 0; x < UniformGridWidth; x++)
                UniformGrid[x, y] = new UniformGridCell((x, y), this);

        ObjectsOnGrid.Clear();
    }

    public override void Update()
    {
        base.Update();
        if (Grid == null) return;

        //note to rohit: i have no god damned clue what is happening here
        //it doesn't work
        //try a diff solution please
        // -> one that works preferably
        /*lock (Graph)
        {
            while (CellsToCheck.TryDequeue(out var cellCheck))
            {
                var cell = cellCheck.Cell;
                var obj = cellCheck.Attached;

                if (obj == null) continue;

                foreach (var objCell in CellsFromBounds(obj.Transform.Bounds))
                {
                    if (cell == objCell) continue;
                    if (Graph.ContainsVertex(cell)) continue;
                    lock (Graph)
                    {
                        Graph.AddVertex(cell);
                        ConnectNodes(cell);

                        foreach (var c in Graph.OutEdges(cell))
                        {
                            var target = c.Target;
                            if (!Graph.TryGetEdge(target, cell, out var edge)) ConnectNodes(target);
                        }
                    }
                }
            }
        }*/

        // check if the grid got changed
        if (!Grid.Updated) return;

        Debug.WriteLine("loading colliders");

        GetComponents<CompositeCollider>().ForEach(RemoveComponent);
        LoadColliders();

        lock (Graph)
        {
            Graph.Clear();

            var q = new Queue<GridCell>();

            var ledges = new Queue<(GridCell, List<Vector2>)>();

            for (var x = 0; x < Grid.Width; x++)
                for (var y = 0; y < Grid.Height; y++)
                {
                    Graph.AddVertex(Grid[x, y]);

                    if (Grid[x, y].Occupied) continue;
                    if (y + 1 >= Grid.Height || !Grid[x, y + 1].Occupied) continue;

                    q.Enqueue(Grid[x, y]);
                }


            while (q.Count > 0)
            {
                var next = q.Dequeue();
                if (!Graph.IsOutEdgesEmpty(next)) continue;

                ConnectNodes(next);

                // is this an edge?
                if (!IsLedge(next, out var filtered)) continue;

                ledges.Enqueue((next, filtered));
            }


            var ignore = new HashSet<GridCell>();
            while (ledges.Count > 0)
            {
                var (next, filtered) = ledges.Dequeue();

                // find closest edge below this one
                foreach (var pos in filtered)
                {
                    var gridCell = Grid[(int)pos.X, (int)pos.Y];

                    var minDist = double.PositiveInfinity;
                    GridCell minCell = null;
                    foreach (var vertex in Graph.Vertices)
                    {
                        if (ignore.Contains(vertex)) continue;
                        if (Graph.IsOutEdgesEmpty(vertex)) continue;
                        if (vertex.GridPosition.Y < pos.Y) continue;
                        var distance = (vertex.GridPosition - pos).Length();
                        if (!(distance < minDist)) continue;

                        minDist = distance;
                        minCell = vertex;
                    }

                    if (minCell == null) continue;
                    Graph.AddVertex(gridCell);
                    var up = Grid[(int)pos.X, (int)pos.Y - 1];
                    Graph.AddVertex(up);

                    ignore.Add(gridCell);
                    ignore.Add(up);

                    Graph.AddEdgeRange(new[]
                    {
                        new Edge<GridCell>(gridCell, next),
                        new Edge<GridCell>(next, gridCell),
                        new Edge<GridCell>(next, up),
                        new Edge<GridCell>(up, gridCell),
                        new Edge<GridCell>(gridCell, up),
                        new Edge<GridCell>(gridCell, minCell)
                    });

                    if (minDist < 64f) Graph.AddEdge(new Edge<GridCell>(minCell, gridCell));
                }
            }
        }

        Grid.Updated = false;

        GC.Collect();
    }

    private bool IsLedge(GridCell next, out List<Vector2> f)
    {
        f = new List<Vector2>();

        var d = next.GridPosition + Vector2.Down;
        var c = Grid[(int)d.X, (int)d.Y];
        if (!c.Occupied) return false;

        var pos1 = next.GridPosition + new Vector2(1, 0);
        var pos2 = next.GridPosition + new Vector2(-1, 0);

        var s = new[] { pos1, pos2 };

        var filtered = (from pos in s
                        where IsInGrid(Grid, pos)
                        where !Grid[(int)pos.X, (int)pos.Y].Occupied
                        select pos).ToList();

        if (!filtered.Any()) return false;

        pos1 = next.GridPosition + new Vector2(1, 1);
        pos2 = next.GridPosition + new Vector2(-1, 1);
        s = new[] { pos1, pos2 };

        filtered = (from pos in s
                    where IsInGrid(Grid, pos)
                    where !Grid[(int)pos.X, (int)pos.Y].Occupied
                    select pos).ToList();

        if (!filtered.Any()) return false;

        var isEdge = false;

        foreach (var pos in filtered)
        {
            if (Grid[(int)pos.X, (int)pos.Y].Occupied) continue;
            isEdge = true;
            break;
        }

        f = filtered;
        return isEdge;
    }

    private bool IsInGrid(Grid g, Vector2 gridPos)
    {
        if (gridPos.X < 0 || gridPos.X >= g.Width) return false;
        if (gridPos.Y < 0 || gridPos.Y >= g.Height) return false;

        return true;
    }

    public void DrawPath(List<Vector2> path)
    {
        /*_edgesColored.Clear();
        foreach (var e in path)
            _edgesColored.Add(Grid[(int)(e.X / TileSizeX), (int)(e.Y / TileSizeY)]);*/
    }

    public override void Draw()
    {
        // DEBUG
        /*lock (Graph)
        {
            if (GameManager.MainCamera == null) return;
            var camPos = GameManager.MainCamera.Transform.Position;
            foreach (var edge in Graph.Edges)
            {
                var from = edge.Source.Center;
                var to = edge.Target.Center;
                var color = new Color(255, 255, 255);
                if (_edgesColored.Contains(edge.Source) || _edgesColored.Contains(edge.Target)) color = Color.Green;
                var otherColor = color;
                if (IsLedge(edge.Source, out var _))
                {
                    otherColor = Color.Red;
                }


                Engine.DrawLine(from - camPos, to - camPos, color);
                Engine.DrawRectSolid(new Bounds2(from - camPos - new Vector2(2.5f, 2.5f), new Vector2(5, 5)), otherColor);

            }
        }*/
    }

    public override string ToString()
    {
        // print grid
        var sb = new StringBuilder();

        sb.Append("Level " + LevelNumber + " - " + Name);
        sb.Append("\n");

        for (var y = 0; y < Grid.Height; y++)
        {
            for (var x = 0; x < Grid.Width; x++)
            {
                var cell = Grid[x, y];
                sb.Append(cell.Occupied ? "X" : " ");
            }

            sb.Append("\n");
        }

        return sb.ToString();
    }

    public List<GameObject> GetObjectsInGridSpace(int x, int y)
    {
        if (x >= UniformGrid.GetLength(0) || y >= UniformGrid.GetLength(1)) return new List<GameObject>();
        if (x < 0 || y < 0) return new List<GameObject>();

        var uniformCell = UniformGrid[x, y];
        return uniformCell.ObjectsInCell;
    }

    public List<GameObject> GetObjectsInSpaces(HashSet<(int, int)> spaces)
    {
        var objs = new List<GameObject>();
        foreach (var space in spaces) objs.AddRange(GetObjectsInGridSpace(space.Item1, space.Item2));

        return objs;
    }

    public List<GameObject> GetObjects(GameObject obj)
    {
        if (!ObjectsOnGrid.ContainsKey(obj)) return new List<GameObject>();

        var cells = ObjectsOnGrid[obj];
        var objs = new List<GameObject>();
        foreach (var cell in cells) objs.AddRange(cell.ObjectsInCell);

        return objs;
    }

    public HashSet<UniformGridCell> GetSpaces(GameObject obj)
    {
        if (!ObjectsOnGrid.ContainsKey(obj)) return new HashSet<UniformGridCell>();

        var cells = ObjectsOnGrid[obj];
        return cells;
    }

    public List<GameObject> GetObjectsAroundObject(GameObject obj)
    {
        if (!ObjectsOnGrid.ContainsKey(obj)) return new List<GameObject>();
        var cells = ObjectsOnGrid[obj];
        var objs = GetObjects(obj);

        // get all the objects in the cells around the object
        foreach (var cell in cells)
        {
            var x = cell.Position.Item1;
            var y = cell.Position.Item2;

            if (x - 1 >= 0) objs.AddRange(GetObjectsInGridSpace(x - 1, y));

            if (x + 1 < UniformGrid.GetLength(0)) objs.AddRange(GetObjectsInGridSpace(x + 1, y));

            if (y - 1 >= 0) objs.AddRange(GetObjectsInGridSpace(x, y - 1));

            if (y + 1 < UniformGrid.GetLength(1)) objs.AddRange(GetObjectsInGridSpace(x, y + 1));
        }

        return objs;
    }

    public void AddToGrid(GameObject obj, int x, int y)
    {
        if (x >= UniformGrid.GetLength(0) || y >= UniformGrid.GetLength(1)) return;
        if (x < 0 || y < 0) return;

        var uniformCell = UniformGrid[x, y];
        uniformCell.ObjectsInCell.Add(obj);

        HashSet<UniformGridCell> arr;
        if (ObjectsOnGrid.TryGetValue(obj, out var value))
        {
            arr = value;
        }
        else
        {
            arr = new HashSet<UniformGridCell>();
            ObjectsOnGrid.Add(obj, arr);
        }

        if (arr.Contains(uniformCell)) return;
        arr.Add(uniformCell);

        ObjectsOnGrid[obj] = arr;
    }

    public void RemoveFromGrid(GameObject obj)
    {
        if (!ObjectsOnGrid.ContainsKey(obj)) return;

        var cells = ObjectsOnGrid[obj];
        foreach (var cell in cells) cell.ObjectsInCell.Remove(obj);

        ObjectsOnGrid.Remove(obj);
    }

    public void UpdateOnGrid(GameObject obj)
    {
        // update graph

        //note to rohit: i have no god damned clue what is happening here
        //it doesn't work
        //try a diff solution please
        if (obj.HasComponent<BoxCollider>() && !obj.BoxCollider.IsTrigger && obj.Label == Label.Other)
        {
            var movingPlatform = obj.HasComponent<MovingPlatform>() && obj.GetComponent<MovingPlatform>().Moving;

            var cellsFromBounds = CellsFromBounds(obj.Transform.Bounds);
            int i = 0;
            foreach (var cell in cellsFromBounds)
            {
                try
                {
                    var bbbbb = obj.Transform.Position;
                    var cccccc = cell.Position;
                } catch (Exception _) { }

                var dist = (obj.Transform.Position - cell.Position).Length();
                lock (Graph)
                {
                    if (!(dist <= obj.Transform.Size.Length()) || !Graph.ContainsVertex(cell)) continue;
                    var outEdges = (from c in Graph.OutEdges(cell)
                               select c.Target).ToArray();

                    Graph.RemoveVertex(cell);

                    var up = cell.GridPosition + Vector2.Up;

                    var upCell = Grid[(int)up.X, (int)up.Y];

                    Graph.AddVertex(upCell);

                    if (i + 1 < cellsFromBounds.Count)
                    {
                        if (cellsFromBounds[i + 1].GridPosition != null)
                        {
                            var nextCellPos = cellsFromBounds[i + 1].GridPosition + Vector2.Up;
                            var nextCell = Grid[(int)nextCellPos.X, (int)nextCellPos.Y];
                            Graph.AddVertex(nextCell);
                            if (Graph.TryGetEdge(upCell, nextCell, out var _)) continue;

                            Graph.AddEdge(new Edge<GridCell>(upCell, nextCell));
                            Graph.AddEdge(new Edge<GridCell>(nextCell, upCell));
                        }
                    }

                    // remove connection or check in loop if the record exists in records dictionary when pathfinding
                    CellsToCheck.Enqueue(new CellCheckData
                    {
                        Cell = cell,
                        Attached = obj,
                        EdgeData = outEdges
                    });

                    i++;
                }
            }
        }

        List<UniformGridCell> cellsCurrIn;
        if (!ObjectsOnGrid.TryGetValue(obj, out var curr))
        {
            cellsCurrIn = new List<UniformGridCell>();
            ObjectsOnGrid.Add(obj, new HashSet<UniformGridCell>());
        }
        else
        {
            cellsCurrIn = curr.ToList();
        }

        var spaces = GetGridSpaces(obj.Transform.Bounds);
        foreach (var cell in cellsCurrIn)
        {
            if (spaces.Contains(cell.Position)) continue;

            cell.ObjectsInCell.Remove(obj);
            ObjectsOnGrid[obj].Remove(cell);
        }

        HashSet<UniformGridCell> s;
        if (!ObjectsOnGrid.TryGetValue(obj, out s)) s = new HashSet<UniformGridCell>();

        if (spaces.Count == s.Count) return;

        var l = s.Select(c => c.Position).ToList();
        foreach (var space in spaces)
        {
            if (l.Contains(space)) continue;
            AddToGrid(obj, space.Item1, space.Item2);
        }
    }

    public HashSet<(int, int)> GetGridSpaces(Bounds2 pos)
    {
        var set = new HashSet<(int, int)>();

        var topLeftX = (int)(pos.Min.X / 64);
        var topLeftY = (int)(pos.Min.Y / 64);

        var bottomRightX = (int)(pos.Max.X / 64);
        var bottomRightY = (int)(pos.Max.Y / 64);

        set.Add((topLeftX, topLeftY));

        if (topLeftX != bottomRightX)
            // no corr needed on x
            for (var x = topLeftX + 1; x <= bottomRightX; x++)
                set.Add((x, topLeftY));

        if (topLeftY != bottomRightY)
            // no corr needed on y
            for (var y = topLeftY + 1; y <= bottomRightY; y++)
                set.Add((topLeftX, y));

        return set;
    }

    public void LoadUniformGrid()
    {
        ResetUniformGrid();
        for (var y = 0; y < Grid.Height; y++)
            for (var x = 0; x < Grid.Width; x++)
            {
                var cell = Grid[x, y];
                if (!cell.Occupied) continue;

                var obj = cell.GameObject;

                AddToGrid(obj, x / 2, y / 2);
            }

        foreach (var movingObj in MovingObjects)
        {
            var pos = movingObj.Transform.Position;
            var gridPosX = pos.X / 64;
            var gridPosY = pos.Y / 64;

            var x = (int)gridPosX;
            var y = (int)gridPosY;

            var corrX = gridPosX - x;
            var corrY = gridPosY - y;

            // first grid space
            AddToGrid(movingObj, x, y);

            var err = 0.001f;

            if (corrX > err && corrY <= err)
                // we bleed into the 4th quadrant
                AddToGrid(movingObj, x + 1, y);

            if (corrX > err && corrY > err)
                // we bleed into the 3rd quadrant
                AddToGrid(movingObj, x + 1, y + 1);

            if (corrX <= err && corrY > err)
                // we bleed into the 2nd quadrant
                AddToGrid(movingObj, x, y + 1);
        }
    }

    public void Load()
    {
        foreach (var gridCell in Grid)
        {
            if (!gridCell.Occupied) continue;

            GameManager.Load(gridCell.GameObject);
        }

        foreach (var movingObject in MovingObjects)
        {
            movingObject.Transform.Position = OriginalPositions[movingObject];
            if (movingObject.HasComponent<RigidBody>())
            {
                movingObject.RigidBody.Position = OriginalPositions[movingObject];
                movingObject.RigidBody.Grounded = false;
                movingObject.RigidBody.Velocity = Vector2.Zero;
            }

            if (movingObject.HasComponent<MovingEntity>())
                movingObject.GetComponent<MovingEntity>().WallStick = (false, null, Direction.None);

            GameManager.Load(movingObject);

            if (movingObject.HasComponent<Player>()) Game.MainPlayer = movingObject;
        }

        LoadUniformGrid();
    }


    private void ConnectNodes(GridCell start)
    {
        var cells = Grid.GridCells;
        for (var xOffset = -1; xOffset < 2; xOffset++)
        {
            var x = (int)start.GridPosition[0] + xOffset;
            if (x < 0 || x >= cells.GetLength(0)) continue;
            for (var yOffset = -1; yOffset < 2; yOffset++)
            {
                var y = (int)start.GridPosition[1] + yOffset;
                if (y < 0 || y >= cells.GetLength(1)) continue;

                var cell = cells[x, y];
                if (cell == start) continue;
                if (cell.Occupied && CompositeColliders.ContainsKey(cell.GameObject)) continue;

                if (CellsToCheck.Select(data => data.Cell == cell).Any()) continue;

                if (Graph.TryGetEdge(start, cell, out var _)) continue;
                if (Graph.TryGetEdge(cell, start, out var _)) continue;

                if (y + 1 >= Grid.Height || !cells[x, y + 1].Occupied) continue;

                if (!Graph.ContainsVertex(start)) Graph.AddVertex(start);
                if (!Graph.ContainsVertex(cell)) Graph.AddVertex(cell);

                Graph.AddEdge(new Edge<GridCell>(start, cell));
                Graph.AddEdge(new Edge<GridCell>(cell, start));
            }
        }
    }


    public void LoadColliders()
    {
        // this is a tilemap, none of the tiles in the grid have a box collider,
        // we have to create a composite collider for each row and column of tiles
        // Loop through each row
        CompositeColliders.Clear();

        var oneGroupStragglers = new List<GridCell>();
        for (var y = 0; y < Grid.Height; y++)
        {
            var contiguousCells = new List<GridCell>();

            // Loop through each column in the row
            for (var x = 0; x < Grid.Width; x++)
            {
                var cell = Grid[x, y];

                if (cell.Occupied)
                {
                    contiguousCells.Add(cell);
                }
                else
                {
                    if (contiguousCells.Count <= 0) continue;

                    if (contiguousCells.Count == 1)
                    {
                        oneGroupStragglers.Add(contiguousCells[0]);
                        contiguousCells.Clear();
                        continue;
                    }

                    CompositeCollider.CreateCompositeCollider(this, CompositeColliders, contiguousCells);
                    contiguousCells.Clear();
                }
            }

            // Check if the last cells in the row form a contiguous group
            if (contiguousCells.Count <= 0) continue;

            if (contiguousCells.Count == 1)
            {
                oneGroupStragglers.Add(contiguousCells[0]);
                continue;
            }

            CompositeCollider.CreateCompositeCollider(this, CompositeColliders, contiguousCells);
        }

        // Loop through each column
        for (var x = 0; x < Grid.Width; x++)
        {
            var contiguousCells = new List<GridCell>();

            // Loop through each row in the column
            for (var y = 0; y < Grid.Height; y++)
            {
                var cell = Grid[x, y];

                if (!oneGroupStragglers.Contains(cell) && cell.Occupied) continue;

                if (cell.Occupied)
                {
                    contiguousCells.Add(cell);
                }
                else
                {
                    if (contiguousCells.Count <= 0) continue;

                    CompositeCollider.CreateCompositeCollider(this, CompositeColliders, contiguousCells);
                    contiguousCells.Clear();
                }
            }

            // Check if the last cells in the column form a contiguous group
            if (contiguousCells.Count <= 0) continue;

            CompositeCollider.CreateCompositeCollider(this, CompositeColliders, contiguousCells);
        }
    }


    public class UniformGridCell
    {
        private readonly Level _level;

        public UniformGridCell((int, int) gridPos, Level level)
        {
            _level = level;
            Position = gridPos;
            ObjectsInCell = new List<GameObject>();
        }

        public (int, int) Position { get; set; }
        public List<GameObject> ObjectsInCell { get; }

        public Vector2 GamePosition => new Vector2(Position.Item1 * 64, Position.Item2 * 64);

        public override string ToString()
        {
            return $"({Position.Item1 + ", " + Position.Item2})";
        }

        public static UniformGridCell operator +(UniformGridCell a, Vector2 b)
        {
            return a._level.UniformGrid[a.Position.Item1 + (int)b.X, a.Position.Item2 + (int)b.Y];
        }
    }
}