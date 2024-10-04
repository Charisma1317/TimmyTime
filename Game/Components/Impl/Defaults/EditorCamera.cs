using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

internal class EditorCamera : Camera
{
    public EditorState State { get; set; }

    private void HandleKeyboard()
    {
        if (ScreenManager.Screens[0].DisabledKeyboardInput) return;

        if (Engine.GetKeyDown(Key.NumRow1))
        {
            State.Tool = EditorTool.Move;
            State.ToolProps = new MoveTool();
        }

        if (Engine.GetKeyDown(Key.NumRow2))
        {
            State.Tool = EditorTool.Draw;
            State.ToolProps = new DrawTool();
        }

        if (Engine.GetKeyDown(Key.NumRow3))
        {
            State.Tool = EditorTool.Delete;
            State.ToolProps = new DeleteTool();
        }

        if (Engine.GetKeyDown(Key.NumRow4))
        {
            State.Tool = EditorTool.Edit;
            State.ToolProps = new EditTool();
        }

        // update camera position based on inputs
        var speed = 300f * Engine.TimeDelta;

        if (Engine.GetKeyHeld(Key.W))
        {
            Transform.Position += new Vector2(0, -speed);
        }

        if (Engine.GetKeyHeld(Key.S))
        {
            Transform.Position += new Vector2(0, speed);
        }

        if (Engine.GetKeyHeld(Key.A))
        {
            Transform.Position += new Vector2(-speed, 0);
        }

        if (Engine.GetKeyHeld(Key.D))
        {
            Transform.Position += new Vector2(speed, 0);
        }
    }

    private void SaveToFile()
    {
        var editorLevel = LevelManager.CurrentLevel;
        JsonLevelWriter.ConvertLevelToJson(editorLevel);
    }

    private GameObject _hoveringObject;

    private Queue<Bounds2> _highlightsToDraw = new Queue<Bounds2>();

    private Vector2 _mouseStartPos = Vector2.Zero;

    private void HandleMouse()
    {
        var mousePos = Engine.MousePosition;

        if (ScreenManager.Screens[0].DisabledMouseInput) return;

        // highlight hovered object if we're not drawing
        if (State.Tool != EditorTool.Draw)
        {
            var localMouse = new Vector2(mousePos.X + Transform.Position.X,
                mousePos.Y + Transform.Position.Y);

            var hovered = RenderedObjects.FirstOrDefault(obj =>
                obj.SpriteRenderer != null && obj.SpriteRenderer.Bounds.Contains(localMouse));

            _hoveringObject = hovered;
        }

        switch (State.Tool)
        {
            // click
            case EditorTool.Edit when Engine.GetMouseButtonDown(MouseButton.Left):
                ((EditTool)State.ToolProps).Object = _hoveringObject;
                break;
            case EditorTool.Move when Engine.GetMouseButtonDown(MouseButton.Left):
                ((MoveTool)State.ToolProps).Object = _hoveringObject;
                break;
            case EditorTool.Delete when Engine.GetMouseButtonDown(MouseButton.Left):
                ((DeleteTool)State.ToolProps).Object = _hoveringObject;
                break;


            // held
            case EditorTool.Delete when Engine.GetMouseButtonHeld(MouseButton.Left):
                UseDeleteTool();
                break;

            case EditorTool.Move when Engine.GetMouseButtonHeld(MouseButton.Left):
                UseMoveTool();
                break;
            case EditorTool.Draw when Engine.GetMouseButtonHeld(MouseButton.Left):
                UseDrawTool();
                break;

            case EditorTool.Draw when Engine.GetMouseButtonUp(MouseButton.Left):
                FinishLengthDraw();
                _mouseStartPos = Vector2.Zero;
                break;
        }
    }

    private void FinishLengthDraw()
    {
        var drawTool = (DrawTool)State.ToolProps;

        if (drawTool.Prefab == null)
        {
            drawTool.Prefab = "Block"; // default to first prefab
        }

        if (!PrefabManager.HasArgument(drawTool.Prefab, "Length", typeof(int)))
        {
            return;
        }

        var startGridPos = MouseToGrid(_mouseStartPos);
        var startGridX = (int)startGridPos.X;
        var startGridY = (int)startGridPos.Y;

        var mousePos = Engine.MousePosition;
        var endGridPos = MouseToGrid(mousePos);
        var endGridX = (int)endGridPos.X;
        var endGridY = (int)endGridPos.Y;

        var lengthX = Math.Abs(endGridX - startGridX) + 1;

        var level = LevelManager.CurrentLevel;
        if (startGridX >= level.Grid.Width || startGridY >= level.Grid.Height) return;
        if (endGridX >= level.Grid.Width || endGridY >= level.Grid.Height) return;

        // check if any spaces between startX and endX are occupied
        var occupied = false;
        var grid = level.Grid;
        var startX = Math.Min(startGridX, endGridX);
        var endX = Math.Max(startGridX, endGridX);
        for (var i = startX; i <= endX; i++)
        {
            var cell = grid[i, startGridY];
            if (!cell.Occupied) continue;
            occupied = true;
            break;
        }

        if (occupied)
        {
            return;
        }

        var startCell = level.Grid[startGridX, startGridY];
        var endCell = level.Grid[endGridX, endGridY];

        var startPosX = Math.Min(startCell.Position.X, endCell.Position.X);

        var tilePos = new Vector2(startPosX, startCell.Position.Y);

        // draw object
        var obj = PrefabManager.Create(drawTool.Prefab, tilePos, lengthX);

        // strip obj of components
        var components = obj.GetComponents();
        foreach (var comp in components)
        {
            if (comp is Transform || comp is SpriteRenderer) continue;
            obj.RemoveComponent(comp);
        }

        // add to moving objects
        LevelManager.CurrentLevel.MovingObjects.Add(obj);
        LevelManager.CurrentLevel.OriginalPositions.Add(obj, obj.Transform.Position);

        SaveToFile();

        GameManager.Load(obj);
    }

    private void UseDrawTool()
    {
        var drawTool = (DrawTool)State.ToolProps;

        if (_mouseStartPos == Vector2.Zero)
        {
            _mouseStartPos = Engine.MousePosition;
        }

        if (drawTool.Prefab == null)
        {
            drawTool.Prefab = "Block"; // default to first prefab
        }

        var mousePos = Engine.MousePosition;
        var gridTileWidth = Grid.TileSizeX;
        var gridTileHeight = Grid.TileSizeY;

        var localMouse = new Vector2(mousePos.X + Transform.Position.X,
            mousePos.Y + Transform.Position.Y);

        var gridX = (int)(localMouse.X / gridTileWidth);
        var gridY = (int)(localMouse.Y / gridTileHeight);

        var level = LevelManager.CurrentLevel;
        if (gridX >= level.Grid.Width || gridY >= level.Grid.Height) return;

        var dist = (localMouse - level.ClosestPositionOnGrid(localMouse)).Length();
        if (dist < 16f) return;
        if (level.Grid[gridX, gridY].Occupied) return;

        var tilePos = new Vector2(gridTileWidth * gridX, gridTileHeight * gridY);

        // check if prefab has a length argument
        if (PrefabManager.HasArgument(drawTool.Prefab, "Length", typeof(int)))
        {
            var startGridPos = MouseToGrid(_mouseStartPos);
            var startGridX = (int)startGridPos.X;
            var startGridY = (int)startGridPos.Y;
            var startCell = level.Grid[startGridX, startGridY];

            var endGridPos = MouseToGrid(mousePos);
            var endGridX = (int)endGridPos.X;
            var endGridY = (int)endGridPos.Y;
            var endCell = level.Grid[endGridX, endGridY];

            var len = Math.Abs(endGridX - startGridX) + 1;

            var startX = Math.Min(startCell.Position.X, endCell.Position.X);

            var localPos = new Vector2(startX - Transform.Position.X + CameraBorder,
                startCell.Position.Y - Transform.Position.Y + CameraBorder);

            var newBounds = new Bounds2(localPos, new Vector2(gridTileWidth * (len), gridTileHeight));

            // draw highlight
            DrawHighlight(newBounds);
            return;
        }

        // draw object
        var obj = PrefabManager.Create(drawTool.Prefab, tilePos);

        // strip obj of components
        var components = obj.GetComponents();
        foreach (var comp in components)
        {
            if (comp is Transform || comp is SpriteRenderer) continue;
            obj.RemoveComponent(comp);
        }

        var freeTile = obj.Free;

        if (!freeTile)
        {
            // add to grid
            var cell = LevelManager.CurrentLevel.Grid[gridX, gridY];
            cell.GameObject = obj;

            LevelManager.CurrentLevel.Grid.Updated = true;
        }
        else
        {
            obj.Transform.Center = localMouse;
            PrefabManager.PrefabArguments[obj][0] = obj.Transform.Position;
            // add to moving objects
            LevelManager.CurrentLevel.MovingObjects.Add(obj);
            LevelManager.CurrentLevel.OriginalPositions.Add(obj, obj.Transform.Position);
        }

        SaveToFile();

        GameManager.Load(obj);
    }

    private void UseDeleteTool()
    {
        var deleteTool = (DeleteTool)State.ToolProps;
        if (deleteTool.Object == null && _hoveringObject != null) deleteTool.Object = _hoveringObject;

        if (deleteTool.Object == null) return;

        var mousePos = Engine.MousePosition;
        var localMouse = new Vector2(mousePos.X + Transform.Position.X,
            mousePos.Y + Transform.Position.Y);
        var level = LevelManager.CurrentLevel;

        var gameObj = deleteTool.Object;
        var pos = gameObj.Transform.Position;

        var freeTile = gameObj.Free;

        if (freeTile)
        {
            level.MovingObjects.Remove(gameObj);
            level.OriginalPositions.Remove(gameObj);

            gameObj.Delete();

            deleteTool.Object = null;

            SaveToFile();
            return;
        }

        var gridTileWidth = Grid.TileSizeX;
        var gridTileHeight = Grid.TileSizeY;

        // remove from original grid position
        var originalGridX = (int)(pos.X / gridTileWidth);
        var originalGridY = (int)(pos.Y / gridTileHeight);

        var originalCell = level.Grid[originalGridX, originalGridY];
        originalCell.GameObject = null;
        deleteTool.Object = null;

        gameObj.DestroyInternal();

        SaveToFile();

        // update colliders
        level.Grid.Updated = true;
    }

    private void UseMoveTool()
    {
        var moveTool = (MoveTool)State.ToolProps;
        if (moveTool.Object == null && _hoveringObject != null) moveTool.Object = _hoveringObject;

        if (moveTool.Object == null) return;

        var mousePos = Engine.MousePosition;
        var localMouse = new Vector2(mousePos.X + Transform.Position.X,
            mousePos.Y + Transform.Position.Y);
        var level = LevelManager.CurrentLevel;

        // move around on grid
        var gridTileWidth = Grid.TileSizeX;
        var gridTileHeight = Grid.TileSizeY;

        var gridX = (int)(localMouse.X / gridTileWidth);
        var gridY = (int)(localMouse.Y / gridTileHeight);

        if (gridX >= level.Grid.Width || gridY >= level.Grid.Height) return;

        var gameObj = moveTool.Object;
        var pos = gameObj.Transform.Position;

        var prefabArgs = PrefabManager.PrefabArguments[gameObj];

        var freeTile = gameObj.Free;

        if (freeTile)
        {
            // move around freely

            gameObj.Transform.Center = localMouse;

            level.OriginalPositions[gameObj] = gameObj.Transform.Position;

            // update prefab position
            prefabArgs[0] = gameObj.Transform.Position;

            SaveToFile();
            return;
        }


        // check if the tile is occupied
        var cell = level.Grid[gridX, gridY];
        if (cell.Occupied) return;
        var tilePos = cell.Position;

        // remove from original grid position
        var originalGridX = (int)(pos.X / gridTileWidth);
        var originalGridY = (int)(pos.Y / gridTileHeight);

        var originalCell = level.Grid[originalGridX, originalGridY];
        originalCell.GameObject = null;

        // add to grid
        prefabArgs[0] = tilePos;
        cell.GameObject = gameObj;
        gameObj.Transform.Position = cell.Position;

        moveTool.Object = gameObj;

        // update colliders
        level.Grid.Updated = true;

        SaveToFile();
    }

    public Vector2 WorldToScreen(Vector2 worldPos)
    {
        return new Vector2(worldPos.X - Transform.Position.X,
            worldPos.Y - Transform.Position.Y);
    }

    public Vector2 MouseToGrid(Vector2 mousePos)
    {
        var gridTileWidth = Grid.TileSizeX;
        var gridTileHeight = Grid.TileSizeY;

        var localMouse = new Vector2(mousePos.X + Transform.Position.X,
            mousePos.Y + Transform.Position.Y);

        var gridX = (int)(localMouse.X / gridTileWidth);
        var gridY = (int)(localMouse.Y / gridTileHeight);

        return new Vector2(gridX, gridY);
    }

    public override void Start()
    {
        base.Start();

        State = new EditorState
        {
            Tool = EditorTool.Move,
            ToolProps = new MoveTool()
        };
    }

    private int _lockedCounter = 15;

    public override void Update()
    {
        // lock for a few frames
        if (_lockedCounter > 0)
        {
            _lockedCounter--;
            return;
        }

        HandleKeyboard();
        HandleMouse();
    }

    private void DrawHighlight(Bounds2 bounds)
    {
        _highlightsToDraw.Enqueue(bounds);
    }

    public override void Render()
    {
        // set render target
        SDL.SDL_SetRenderTarget(Renderer, Target.Handle);
        SDL.SDL_SetRenderDrawColor(Renderer, 0, 0, 0, 0xFF);
        SDL.SDL_RenderClear(Renderer);

        Engine.DrawRectSolid(new Bounds2(Vector2.Zero, Transform.Size), Color.LightCoral);

        var mousePos = Engine.MousePosition;

        // render all entities
        var objects = RenderedObjects.OrderByDescending(obj => obj.Id).ToList();
        objects.Add(LevelManager.CurrentLevel);

        // draw grid
        var level = LevelManager.CurrentLevel;
        var grid = level.UniformGrid;

        var gridWidth = level.Grid.Width;
        var gridHeight = level.Grid.Height;
        var tileSizeX = Grid.TileSizeX;
        var tileSizeY = Grid.TileSizeY;
        var g = level.Grid;

        var c = Color.White.WithAlpha(0.5f);
        // draw grid
        for (var i = 0; i < gridWidth; i++)
        {
            for (var j = 0; j < gridHeight; j++)
            {
                var tile = g[i, j];
                if (tile == null) continue;

                var tilePos = new Vector2(i * tileSizeX, j * tileSizeY);
                var localPos = new Vector2(tilePos.X - Transform.Position.X + CameraBorder,
                    tilePos.Y - Transform.Position.Y + CameraBorder);

                var tileBounds = new Bounds2(localPos, new Vector2(tileSizeX, tileSizeY));

                Engine.DrawRectEmpty(tileBounds, c);

                if (State.Tool == EditorTool.Draw && tileBounds.Contains(mousePos) && !ScreenManager.Screens[0].DisabledMouseInput)
                {
                    DrawHighlight(tileBounds);
                }
            }
        }

        gridWidth = grid.GetLength(0);
        gridHeight = grid.GetLength(1);
        tileSizeX = 64;
        tileSizeY = 64;

        // draw uniform grid
        for (var i = 0; i < gridWidth; i++)
        {
            for (var j = 0; j < gridHeight; j++)
            {
                var tile = grid[i, j];
                if (tile == null) continue;

                var tilePos = new Vector2(i * tileSizeX, j * tileSizeY);
                var localPos = new Vector2(tilePos.X - Transform.Position.X + CameraBorder,
                    tilePos.Y - Transform.Position.Y + CameraBorder);

                var tileBounds = new Bounds2(localPos, new Vector2(tileSizeX, tileSizeY));

                Engine.DrawRectEmpty(tileBounds, Color.White);
            }
        }

        foreach (var gameObj in objects)
        {
            if (gameObj.HasComponent<Camera>() || gameObj.HasComponent<EditorCamera>()) continue;

            var oldPos = gameObj.Transform.Position;

            // update entity pos to local pos
            var localPos = new Vector2(oldPos.X - Transform.Position.X + CameraBorder,
                oldPos.Y - Transform.Position.Y + CameraBorder);

            gameObj.SpriteRenderer?.RenderAt(localPos);
            DrawColliderFrame(gameObj);
        }

        // draw an overlay on the hovered object
        if (_hoveringObject != null && State.Tool != EditorTool.Draw)
        {
            var bounds = _hoveringObject.SpriteRenderer.Bounds;
            var pos = _hoveringObject.Transform.Position;

            var localPos = new Vector2(pos.X - Transform.Position.X + CameraBorder,
                pos.Y - Transform.Position.Y + CameraBorder);

            var newBounds = new Bounds2(localPos, bounds.Size);

            DrawHighlight(newBounds);
        }

        // draw an overlay on the edit object
        if (State.Tool == EditorTool.Edit && ((EditTool)State.ToolProps).Object != null)
        {
            var editTool = (EditTool)State.ToolProps;
            var obj = editTool.Object;
            var bounds = obj.SpriteRenderer.Bounds;
            var pos = obj.Transform.Position;

            var localPos = new Vector2(pos.X - Transform.Position.X + CameraBorder,
                pos.Y - Transform.Position.Y + CameraBorder);

            var newBounds = new Bounds2(localPos, bounds.Size);


            if (editTool.Object != null) DrawHighlight(newBounds);
        }

        // draw highlights
        while (_highlightsToDraw.Count > 0)
        {
            var bounds = _highlightsToDraw.Dequeue();
            Engine.DrawRectSolid(bounds, Color.White.WithAlpha(0.15f));
            Engine.DrawRectEmpty(bounds, Color.Black);
        }

        SDL.SDL_SetRenderTarget(Renderer, Engine.RenderTarget.Handle);
        SDL.SDL_SetRenderDrawColor(Renderer, 0, 0, 0, 255);
        SDL.SDL_RenderClear(Renderer);

        // render camera
        var pixelH = Game.Resolution.Y / Transform.Size.Y;
        var corX = (int)Transform.Position.X - Transform.Position.X;
        var corY = (int)Transform.Position.Y - Transform.Position.Y;

        var dest = new SDL.SDL_Rect
        {
            x = (int)(corX * pixelH - pixelH * CameraBorder),
            y = (int)(corY * pixelH - pixelH * CameraBorder),
            w = (int)(TargetWidth * pixelH),
            h = (int)(TargetHeight * pixelH)
        };

        SDL.SDL_RenderCopy(Renderer, Target.Handle, IntPtr.Zero, ref dest);
    }
}

class EditorState
{
    public EditorTool Tool { get; set; }
    public ITool ToolProps { get; set; }
}

enum EditorTool
{
    Move,
    Edit,
    Draw,
    Delete
}

class MoveTool : ITool
{
    public GameObject Object { get; set; }

    public override string ToString()
    {
        return "Move(Object Id: " + (Object == null ? "null" : Object.Id + "") + ")";
    }
}

class DeleteTool : ITool
{
    public GameObject Object { get; set; }

    public override string ToString()
    {
        return "Delete(Object Id: " + (Object == null ? "null" : Object.Id + "") + ")";
    }
}

class EditTool : ITool
{
    public GameObject Object { get; set; }

    public override string ToString()
    {
        return "Edit(Object Id: " + (Object == null ? "null" : Object.Id + "") + ")";
    }
}

class DrawTool : ITool
{
    public string Prefab { get; set; }

    public override string ToString()
    {
        return "Draw(Prefab: " + Prefab + ")";
    }
}

interface ITool
{
    string ToString();
}