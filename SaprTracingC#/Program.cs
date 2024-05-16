public class PassInfo
{
    public bool IsPassed = false;
    public int Weight = 0;
    public Direction Direction;
};

public class Component
{
    public int TraceId;
}

public enum CellState
{
    Empty,
    Obstacle,
    ContainsComponet,
    ContainsWire
};

public enum Direction
{
    Down,
    Left,
    Up,
    Right
}

public class Cell
{
    public int Id;
    public Cell RightCell;
    public Cell LeftCell;
    public Cell UpCell;
    public Cell DownCell;

    public PassInfo PassInfo;
    public CellState State;
    public Component Component;

    public bool IsEqual(Cell other)
    {
        return Id == other.Id;
    }
};

public class Program
{
    public static void Main()
    {

    }
}


public class DiscreteField
{
    private Cell[,] cells;
    public int Width { get; private set; }
    public int Height { get; private set; }


    public DiscreteField(int width, int height)
    {
        Width = width;
        Height = height;
        cells = new Cell[height, width];
    }

    public void SetCell(int x, int y, Cell cell)
    {
        cells[y, x] = cell;
    }

    public Cell GetCell(int x, int y)
    {
        return cells[y, x];
    }

    public void ConnectCells()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Cell cell = GetCell(x, y);
                cell.RightCell = GetCellOrNull(x + 1, y);
                cell.LeftCell = GetCellOrNull(x - 1, y);
                cell.UpCell = GetCellOrNull(x, y - 1);
                cell.DownCell = GetCellOrNull(x, y + 1);
            }
        }
    }

    private Cell GetCellOrNull(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null;
        return GetCell(x, y);
    }
}

