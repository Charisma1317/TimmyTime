using QuikGraph;
using QuikGraph.Algorithms.Observers;
using QuikGraph.Algorithms.ShortestPath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks.Dataflow;

internal class MoveToEntity : Goal<List<MoveDirection>>
{
    private readonly Type _toMoveToType;
    private GameObject _toMoveTo;

    private Vector2 _startPos;
    private Vector2 _endPos;

    private List<Vector2> _path;

    public MoveToEntity(GameObject parent, Type moveToType) : base(parent)
    {
        _toMoveToType = moveToType;
    }

    public override List<MoveDirection> Execute()
    {
        _toMoveTo = GameManager.LoadedObjectsByType[_toMoveToType].FirstOrDefault();

        if (_toMoveTo == null) return new List<MoveDirection>();

        var level = LevelManager.CurrentLevel;
        var start = level.ClosestCellFromPosition(_parent.Transform.Center);
        var end = level.ClosestCellFromPosition(_toMoveTo.Transform.Center);
        //var list = AStar(start, end, 0);
        var pathToTake = new List<MoveDirection>();

        if (start == null || end == null) return pathToTake;


        var lengthCheck = (_endPos - end.Position).Length() > 5f;
        var edgeExistsCheck = false;

        if (_path != null && _path.Count > 1)
        {
            var c = level.ClosestCellFromPosition(_path[1]);
            edgeExistsCheck = !level.Graph.ContainsVertex(c); // if the edge doesn't exist anymore then recalc path
        }

        // 5f is an arbitrary distance from the prev end goal
        if (_path == null || _path.Count == 0 ||
            /* RECALC PATH IF PLAYER MOVED */ lengthCheck ||
            /* RECALC PATH IF EDGE DIED */ edgeExistsCheck)
        {
            _path = AStar(start, end);
            _startPos = start.Position;
            _endPos = end.Position;
            // level.DrawPath(_path);
        }

        if (_path.Count <= 1) return pathToTake;

        var p = _path;

        var pos = _parent.Transform.Center;

        var vector = p[1];
        var cell = level.ClosestCellFromPosition(_path[1]);

        // check if a jump is going to be needed at one point
        if (vector.Y < start.Position.Y)
        {
            pathToTake.Add(MoveDirection.Jump);
        }

        if (Math.Abs(cell.GridPosition.Y - start.GridPosition.Y) < 0.001f)
        {
            var b = new Vector2(vector.X, pos.Y);
            // check distance to next point
            if ((b - pos).Length() < 3f)
            {
                _path.RemoveAt(1);
                if (_path.Count > 1)
                    vector = _path[1];
                else
                {
                    _path = null;
                }
            }
        }

        var isDrop = vector.Y > start.Position.Y;
        var viable = isDrop;

        if (!viable)
        {
            var under = level.Grid[(int)cell.GridPosition.X, (int)cell.GridPosition.Y + 1];
            var underPos = under?.Position ?? new Vector2(cell.Position.X, cell.Position.Y + Grid.TileSizeY);

            if (under != null && under.Occupied)
            {
                viable = true;
            }

            var uniformGridSpace = level.GetGridSpaces(new Bounds2(underPos, new Vector2(Grid.TileSizeX, Grid.TileSizeY)));

            var objs = level.GetObjectsInSpaces(uniformGridSpace);


            foreach (var o in objs)
            {
                if (o == _toMoveTo || o == _parent) continue;
                if (o.Label != Label.Other) continue;
                if (!o.HasComponent<BoxCollider>() || o.BoxCollider.IsTrigger) continue;

                var pppp = _parent.Transform.Position;

                var distLeftToLeft = (o.Transform.Position - pppp).Length();
                var distRightToLeft = (o.Transform.Right - pppp).Length();

                /*_parent.SpriteRenderer.Draw(() =>
                {
                    var camPos = GameManager.MainCamera.Transform.Position;

                    var str = (o.Transform.Position - pppp).Length() + "px";
                    var strr = (o.Transform.Right - pppp).Length() + "px";
                    var pp = ((o.Transform.Position + pppp) / 2) - camPos;
                    var ppp = ((o.Transform.Right + pppp) / 2) - camPos;
                    //Engine.DrawString(str, pp, Color.White, Game.SmallFont);
                    Engine.DrawString(strr, ppp, Color.White, Game.SmallFont);
                });*/

                var minDist = Math.Min(distLeftToLeft, distRightToLeft);
                var distConst = 32;
                if (minDist > distConst) continue;

                viable = true;
                break;
            }

            var objsNearUs = level.GetObjectsAroundObject(_parent);
            foreach (var o in objsNearUs)
            {
                if (o.Transform.Position.Y <= start.Position.Y) continue;
                if (!o.HasComponent<MovingPlatform>()) continue;

                var origin = _parent.Transform.Position;
                var distLeftToLeft = (o.Transform.Position - origin).Length();
                var distRightToLeft = (o.Transform.Right - origin).Length();

                if (distLeftToLeft < distRightToLeft) origin = _parent.Transform.Right;

                Ray rayCast = new Ray(start.Center, Vector2.Down);
                Collision c = rayCast.Cast(o.Transform.Bounds);
                if (!c.IsHit) continue;

                viable = true;
                break;
            }
        }

        if (vector.X < pos.X && viable)
        {
            pathToTake.Add(MoveDirection.Left);
        }

        if (vector.X > pos.X && viable)
        {
            pathToTake.Add(MoveDirection.Right);
        }

        return pathToTake;
    }

    private double Heuristic(Grid.GridCell node)
    {
        // Manhattan distance
        return Math.Abs(node.Position.X - _toMoveTo.Transform.Position.X) +
               Math.Abs(node.Position.Y - _toMoveTo.Transform.Position.Y);
    }

    private List<Vector2> AStar(Grid.GridCell start, Grid.GridCell end)
    {
        var level = LevelManager.CurrentLevel;

        if (level.Grid.Updated) return new List<Vector2>();

        if (start == null || end == null) return new List<Vector2>();

        if (!level.Graph.ContainsVertex(start) || !level.Graph.ContainsVertex(end)) return new List<Vector2>();

        var aStar = new AStarShortestPathAlgorithm<Grid.GridCell, Edge<Grid.GridCell>>(level.Graph, GetWeight, Heuristic);

        // Creating the observer
        var vis = new VertexPredecessorRecorderObserver<Grid.GridCell, Edge<Grid.GridCell>>();

        // Compute and record shortest paths
        using (vis.Attach(aStar))
        {
            aStar.Compute(start);
        }

        /*_parent.SpriteRenderer.Draw(() =>
        {
            var camPos = GameManager.MainCamera.Transform.Position;

            Engine.DrawRectEmpty(new Bounds2(start.Position - camPos, new Vector2(32, 32)), Color.Blue);
            Engine.DrawRectEmpty(new Bounds2(end.Position - camPos, new Vector2(32, 32)), Color.Red);
        });*/

        var p = new List<Vector2>();

        var s = _parent.Transform.Position;
        var e = _toMoveTo.Transform.Center;

        // vis can create all the shortest path in the graph
        if (vis.TryGetPath(end, out var path))
        {
            var edges = path.ToList();
            foreach (var edge in edges)
            {
                var a = edge.Source.Position + new Vector2(Grid.TileSizeX / 2f, 0);
                var b = edge.Target.Position + new Vector2(Grid.TileSizeX / 2f, 0);

                p.Add(a);
                p.Add(b);
            }
        }

        if (p.Count <= 1) return p.ToList();

        var temp = e;
        // set y to be on the same level as the _parent
        temp.Y = p.Last().Y;

        if (p.Count > 1)
            p.Add(temp);

        level.DrawPath(p);

        return p.ToList();
    }


    private double GetWeight(Edge<Grid.GridCell> edge)
    {
        const int baseCost = 1;

        var isJump = edge.Source.Position.Y > edge.Target.Position.Y;

        // Additional cost for jumping early in the path
        var earlyJumpPenalty = 0;
        if (isJump)
        {
            earlyJumpPenalty = 1000;
        }

        return baseCost + earlyJumpPenalty;
    }


}
