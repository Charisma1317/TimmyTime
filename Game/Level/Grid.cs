using System.Collections;
using System.Collections.Generic;
using System.Linq;

internal class Grid : IEnumerable<Grid.GridCell>
{
    public static readonly int TileSizeX = 32;
    public static readonly int TileSizeY = 32;

    public readonly GridCell[,] GridCells;
    public readonly int Height;

    public readonly int Width;

    public Grid(int w, int h)
    {
        Width = w;
        Height = h;
        GridCells = new GridCell[w, h];

        // populate grid with empty cells
        for (var x = 0; x < w; x++)
        for (var y = 0; y < h; y++)
            GridCells[x, y] = new GridCell(new Vector2(x * TileSizeX, y * TileSizeY));
    }

    public bool Updated { get; set; }

    public GridCell this[int x, int y]
    {
        get {
            if (x < 0 || x >= GridCells.GetLength(0)) return null;
            if (y < 0 || y >= GridCells.GetLength(1)) return null;
            return GridCells[x, y];
        }
        set
        {
            GridCells[x, y] = value;
            Updated = true;
        }
    }

    public IEnumerator<GridCell> GetEnumerator()
    {
        return GridCells.Cast<GridCell>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public class GridCell
    {
        public GameObject GameObject;

        public Vector2 Position;

        public Vector2 Center => Position + new Vector2(TileSizeX, TileSizeY)/2;

        public GridCell(Vector2 pos)
        {
            Position = pos;
            GameObject = null;
        }

        public Vector2 GridPosition => new Vector2((int)Position.X / TileSizeX, (int)Position.Y / TileSizeY);
        public bool Occupied => GameObject != null;
    }
}