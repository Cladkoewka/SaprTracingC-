public class PassInfo
{
    public bool IsPassed = false;
    public int Weight = 0;
    public Direction Direction;
};

public class Component
{
    public List<Cell> Contacts;
    public int Id;
    public int TraceId;

}

public enum CellState
{
    Empty,
    Obstacle,
    ContainsComponent,
    ContainsComponentContact,
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
    public int X;
    public int Y;
    public Cell RightCell;
    public Cell LeftCell;
    public Cell UpCell;
    public Cell DownCell;

    public int TraceId;

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
        DiscreteField field = new DiscreteField(16, 16);
        field.InitializeFieldFromFile("C:/Users/semen/Desktop/_/УчебаСем4/SAPR_Trasing/SAPR_Trasing/board.txt");
        field.PrintField();
       
        
        Cell firstPoint = field.Components[0].Contacts[0];
        Cell secondPoint = field.Components[1].Contacts[2];

        field.ClearPassInfo();
        field.WaveAlgorithm(firstPoint, secondPoint);

        Console.WriteLine();
        field.PrintPassWeights();

        field.ClearPassInfo();
        field.LimitedWaveAlgorithm(firstPoint, secondPoint);

        Console.WriteLine();
        field.PrintPassWeights();

        var path = field.ReconstructPath(firstPoint, secondPoint);
        foreach (var c in path)
            c.State = CellState.ContainsWire;

        Console.WriteLine();
        field.PrintField();


    }
}


public static class Solution
{
    public static void TraceElements()
    {

    }
}


public class DiscreteField
{
    private Cell[,] cells;
    public int Width { get; private set; }
    public int Height { get; private set; }

    public List<Component> Components;

    private int componentIdCounter = 1;

    public DiscreteField(int width, int height)
    {
        Width = width;
        Height = height;
        cells = new Cell[height, width];
        Components = new List<Component> { };
    }
    public void InitializeFieldFromFile(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        Height = lines.Length;
        Width = lines[0].Length;

        cells = new Cell[Height, Width];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                cells[y, x] = CreateCellFromChar(x, y, lines[y][x]);
            }
        }

        ConnectCells();
        CreateComponents();
    }

    private void CreateComponents()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Cell cell = GetCell(x, y);
                if (cell.State == CellState.ContainsComponent && cell.Component == null)
                {
                    int componentId = componentIdCounter++;

                    Component component = new Component
                    {
                        Id = componentId,
                        TraceId = cell.TraceId,
                        Contacts = new List<Cell> {},
                    };

                    Components.Add(component);

                    FindFullComponent(cell, component);

                }
            }
        }
    }

    private void FindFullComponent(Cell startCell, Component component)
    {
        Queue<Cell> queue = new Queue<Cell>();
        queue.Enqueue(startCell);
        startCell.Component = component;

        while (queue.Count > 0)
        {
            Cell currentCell = queue.Dequeue();

            AddNeighborsToCellQueue(currentCell, queue, component);
        }
    }

    private void AddNeighborsToCellQueue(Cell cell, Queue<Cell> queue, Component component)
    {
        if (cell.RightCell != null && cell.RightCell.State == CellState.ContainsComponent && cell.RightCell.Component == null)
        {
            cell.RightCell.Component = component;
            queue.Enqueue(cell.RightCell);
        }
        if (cell.RightCell != null && cell.RightCell.State == CellState.ContainsComponentContact)
        {
            cell.RightCell.TraceId = component.TraceId;
            component.Contacts.Add(cell.RightCell);
        }

        if (cell.LeftCell != null && cell.LeftCell.State == CellState.ContainsComponent && cell.LeftCell.Component == null)
        {
            cell.LeftCell.Component = component;
            queue.Enqueue(cell.LeftCell);
        }
        if (cell.LeftCell != null && cell.LeftCell.State == CellState.ContainsComponentContact)
        {
            cell.LeftCell.TraceId = component.TraceId;
            component.Contacts.Add(cell.LeftCell);
        }

        if (cell.UpCell != null && cell.UpCell.State == CellState.ContainsComponent && cell.UpCell.Component == null)
        {
            cell.UpCell.Component = component;
            queue.Enqueue(cell.UpCell);
        }
        if (cell.UpCell != null && cell.UpCell.State == CellState.ContainsComponentContact)
        {
            cell.UpCell.TraceId = component.TraceId;
            component.Contacts.Add(cell.UpCell);
        }

        if (cell.DownCell != null && cell.DownCell.State == CellState.ContainsComponent && cell.DownCell.Component == null)
        {
            cell.DownCell.Component = component;
            queue.Enqueue(cell.DownCell);
        }
        if (cell.DownCell != null && cell.DownCell.State == CellState.ContainsComponentContact)
        {
            cell.DownCell.TraceId = component.TraceId;
            component.Contacts.Add(cell.DownCell);
        }
    }



    private Cell CreateCellFromChar(int x, int y, char symbol)
    {
        Cell cell = new Cell
        {
            Id = y * Width + x,
            State = GetCellStateFromChar(symbol),
            X = x,
            Y = y,
           
        };

        if (cell.State == CellState.ContainsComponent)
            cell.TraceId = symbol - '0';

        cell.PassInfo = new PassInfo();

        return cell;
    }

   

    public CellState GetCellStateFromChar(char symbol)
    {
        switch (symbol)
        {
            case 'O': return CellState.Obstacle;
            case 'E': return CellState.Empty;
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': return CellState.ContainsComponent;
            case '.': return CellState.ContainsComponentContact;
            default: throw new ArgumentException($"Неизвестный символ ячейки: {symbol}");
        }
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

    public void PrintField()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Cell cell = GetCell(x, y);
                switch (cell.State)
                {
                    case CellState.Empty:
                        Console.Write("E ");
                        break;
                    case CellState.Obstacle:
                        Console.Write("O ");
                        break;
                    case CellState.ContainsComponent:
                        Console.Write($"{cell.Component.TraceId} ");
                        break;
                    case CellState.ContainsComponentContact:
                        Console.Write(". ");
                        break;
                    case CellState.ContainsWire:
                        Console.Write("W ");
                        break;
                    default:
                        throw new ArgumentException($"Unknown cell state: {cell.State}");
                }
            }
            Console.WriteLine();
        }
    }

    //Волновой алгоритм
    public void WaveAlgorithm(Cell startCell, Cell endCell)
    {
        // Очищаем информацию о пройденных ячейках
        //ClearPassInfo();

        // Инициализируем начальную ячейку
        startCell.PassInfo.IsPassed = true;
        startCell.PassInfo.Weight = 0;

        // Создаем очередь и помещаем в нее начальную ячейку
        Queue<Cell> queue = new Queue<Cell>();
        queue.Enqueue(startCell);

        while (queue.Count > 0)
        {
            Cell currentCell = queue.Dequeue();


            if (IsCellAvaliable(currentCell.DownCell, endCell))
            {
                int weight = currentCell.PassInfo.Weight + 1;
                currentCell.DownCell.PassInfo.Weight = weight;
                currentCell.DownCell.PassInfo.IsPassed = true;
                queue.Enqueue(currentCell.DownCell);
            }

            if (IsCellAvaliable(currentCell.UpCell, endCell))
            {
                int weight = currentCell.PassInfo.Weight + 1;
                currentCell.UpCell.PassInfo.Weight = weight;
                currentCell.UpCell.PassInfo.IsPassed = true;
                queue.Enqueue(currentCell.UpCell);
            }

            if (IsCellAvaliable(currentCell.LeftCell, endCell))
            {
                int weight = currentCell.PassInfo.Weight + 1;
                currentCell.LeftCell.PassInfo.Weight = weight;
                currentCell.LeftCell.PassInfo.IsPassed = true;
                queue.Enqueue(currentCell.LeftCell);
            }

            if (IsCellAvaliable(currentCell.RightCell, endCell))
            {
                int weight = currentCell.PassInfo.Weight + 1;
                currentCell.RightCell.PassInfo.Weight = weight;
                currentCell.RightCell.PassInfo.IsPassed = true;
                queue.Enqueue(currentCell.RightCell);
            }


        }

    }

    public void LimitedWaveAlgorithm(Cell startCell, Cell endCell)
    {
        // Очищаем информацию о пройденных ячейках
        //ClearPassInfo();

        // Инициализируем начальную ячейку
        startCell.PassInfo.IsPassed = true;
        startCell.PassInfo.Weight = 0;

        // Создаем очередь и помещаем в нее начальную ячейку
        Queue<Cell> queue = new Queue<Cell>();
        queue.Enqueue(startCell);

        while (queue.Count > 0)
        {
            Cell currentCell = queue.Dequeue();


            if (IsCellAvaliable(currentCell.DownCell, endCell) && IsInLimit(currentCell.DownCell, endCell, startCell))
            {
                int weight = currentCell.PassInfo.Weight + 1;
                currentCell.DownCell.PassInfo.Weight = weight;
                currentCell.DownCell.PassInfo.IsPassed = true;
                queue.Enqueue(currentCell.DownCell);
            }

            if (IsCellAvaliable(currentCell.UpCell, endCell) && IsInLimit(currentCell.UpCell, endCell, startCell))
            {
                int weight = currentCell.PassInfo.Weight + 1;
                currentCell.UpCell.PassInfo.Weight = weight;
                currentCell.UpCell.PassInfo.IsPassed = true;
                queue.Enqueue(currentCell.UpCell);
            }

            if (IsCellAvaliable(currentCell.LeftCell, endCell) && IsInLimit(currentCell.LeftCell, endCell, startCell))
            {
                int weight = currentCell.PassInfo.Weight + 1;
                currentCell.LeftCell.PassInfo.Weight = weight;
                currentCell.LeftCell.PassInfo.IsPassed = true;
                queue.Enqueue(currentCell.LeftCell);
            }

            if (IsCellAvaliable(currentCell.RightCell, endCell) && IsInLimit(currentCell.RightCell, endCell, startCell))
            {
                int weight = currentCell.PassInfo.Weight + 1;
                currentCell.RightCell.PassInfo.Weight = weight;
                currentCell.RightCell.PassInfo.IsPassed = true;
                queue.Enqueue(currentCell.RightCell);
            }


        }

    }

    private bool IsCellAvaliable(Cell nextCell, Cell endCell)
    {
        return nextCell != null 
            && !nextCell.PassInfo.IsPassed 
            && (nextCell.State == CellState.Empty || nextCell == endCell);
    }

    private bool IsInLimit(Cell nextCell, Cell endCell, Cell startCell)
    {
        return nextCell.X >= Math.Min(startCell.X, endCell.X)
           && nextCell.X <= Math.Max(startCell.X, endCell.X)
           && nextCell.Y >= Math.Min(startCell.Y, endCell.Y)
           && nextCell.Y <= Math.Max(startCell.Y, endCell.Y);
    }

    public List<Cell> ReconstructPath(Cell startCell, Cell endCell)
    {
        List<Cell> path = new List<Cell>();
        Cell currentCell = endCell;

        while (currentCell != startCell)
        {
            path.Insert(0, currentCell);

            // Найдем соседа с наименьшим весом
            Cell nextCell = null;
            int minWeight = int.MaxValue;

            if (currentCell.DownCell != null && currentCell.DownCell.PassInfo.Weight == currentCell.PassInfo.Weight - 1 )
            {
                if (currentCell.DownCell.PassInfo.Weight < minWeight)
                {
                    nextCell = currentCell.DownCell;
                    minWeight = nextCell.PassInfo.Weight;
                }
            }

            if (currentCell.UpCell != null && currentCell.UpCell.PassInfo.Weight == currentCell.PassInfo.Weight - 1)
            {
                if (currentCell.UpCell.PassInfo.Weight < minWeight)
                {
                    nextCell = currentCell.UpCell;
                    minWeight = nextCell.PassInfo.Weight;
                }
            }

            if (currentCell.LeftCell != null && currentCell.LeftCell.PassInfo.Weight == currentCell.PassInfo.Weight - 1)
            {
                if (currentCell.LeftCell.PassInfo.Weight < minWeight)
                {
                    nextCell = currentCell.LeftCell;
                    minWeight = nextCell.PassInfo.Weight;
                }
            }

            if (currentCell.RightCell != null && currentCell.RightCell.PassInfo.Weight == currentCell.PassInfo.Weight - 1)
            {
                if (currentCell.RightCell.PassInfo.Weight < minWeight)
                {
                    nextCell = currentCell.RightCell;
                    minWeight = nextCell.PassInfo.Weight;
                }
            }

            if (nextCell != null)
            {
                currentCell = nextCell;
            }
            else
            {
                // Если не найден следующий ход, значит что-то пошло не так
                break;
            }
        }

        path.Insert(0, startCell);
        return path;
    }


    public void ClearPassInfo()
    {
        foreach (var cell in cells)
        {
            cell.PassInfo.IsPassed = false;
            cell.PassInfo.Weight = 0;
            cell.PassInfo.Direction = Direction.Down;
        }
    }

    //Отладочные методы
    public void PrintFieldID()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Cell cell = GetCell(x, y);
                Console.Write($"{cell.Id} ");
            }
            Console.WriteLine();
        }
    }

    public void PrintRightNeighborsId()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Cell cell = GetCell(x, y);
                int rightNeighborId = cell.RightCell != null ? cell.RightCell.Id : -1;
                Console.Write($"{rightNeighborId} ");
            }
            Console.WriteLine();
        }
    }

    public void PrintFieldComponentsID()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Cell cell = GetCell(x, y);
                if (cell.State == CellState.ContainsComponent)
                    Console.Write($"{cell.Component.Id} ");
                else
                    Console.Write('_');
            }
            Console.WriteLine();
        }
    }

    public void PrintFieldComponentsTraceID()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Cell cell = GetCell(x, y);
                if (cell.State == CellState.ContainsComponent)
                    Console.Write($"{cell.Component.TraceId} ");
                else
                    Console.Write('_');
            }
            Console.WriteLine();
        }
    }

    public void PrintFieldComponentsContactsTraceID()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Cell cell = GetCell(x, y);
                if (cell.State == CellState.ContainsComponentContact)
                    Console.Write($"{cell.TraceId} ");
                else
                    Console.Write('_');
            }
            Console.WriteLine();
        }
    }

    public void PrintComponentsContacts()
    {
        foreach (var component in Components)
        {
            foreach (var contact in component.Contacts)
            {
                Console.Write($"{contact.Id} ");
            }
            Console.WriteLine();
            
        }
    }

    public void PrintPassWeights()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Cell cell = GetCell(x, y);
                Console.Write("{0,2} ", cell.PassInfo.Weight);
            }
            Console.WriteLine();
        }
    }
}

