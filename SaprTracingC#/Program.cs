﻿/// <summary>
///  Что сделано:
///     - Считывание компонентов разного размера, с разным количеством входов
///     - Разделение на трассы
///     - Волновой алгоритм с ограничением
///     
///     
/// По проблемам:
///     - Нет критерия по изгибам, путевые координаты ломали все, убрал
///     - Нет встречного алгоритма
///     - Есть баги с проложением, может показать Wire на месте элемента
///     - Явно не оптимальное проложение путей, можно короче
///     
/// </summary>

public class Program
{
    public static void Main()
    {
        // Создание дискретного поля, инициализация из файла и вывод на консоль
        DiscreteField field = new DiscreteField(16, 16);
        field.InitializeFieldFromFile("C:/Users/semen/Desktop/_/УчебаСем4/SAPR_Trasing/SAPR_Trasing/board.txt");
        field.PrintField();


        // Проложение проводов для цепи 1
        Solution.TraceElements(field, 1);
        Console.WriteLine();
        field.PrintField();

        // Проложение проводов для цепи 2
        Solution.TraceElements(field, 2);
        Console.WriteLine();
        field.PrintField();

        // Проложение проводов для цепи 3
        Solution.TraceElements(field, 3);
        Console.WriteLine();
        field.PrintField();
    }
}

public static class Solution
{
    public static void TraceElements(DiscreteField discreteField, int traceId)
    {
        // Сгруппируем компоненты по TraceId
        Dictionary<int, List<Component>> componentsByTraceId = new Dictionary<int, List<Component>>();
        foreach (var component in discreteField.Components)
        {
            if (!componentsByTraceId.ContainsKey(component.TraceId))
                componentsByTraceId[component.TraceId] = new List<Component>();

            componentsByTraceId[component.TraceId].Add(component);
        }

        List<Component> componentsWithSameTraceId = componentsByTraceId[traceId];

        // Сортируем компоненты по расстоянию между ними
        componentsWithSameTraceId.Sort((c1, c2) => discreteField.GetDistanceBetweenComponents(c1, c2));

        // Соединяем компоненты в порядке возрастания расстояния
        for (int i = 0; i < componentsWithSameTraceId.Count - 1; i++)
        {
            Component currentComponent = componentsWithSameTraceId[i];
            Component nextComponent = componentsWithSameTraceId[i + 1];

            // Находим ближайшие контакты между текущим и следующим компонентами
            Cell currentContact = currentComponent.Contacts.OrderBy(c => discreteField.GetDistanceBetweenCells(currentComponent.Contacts[0], c)).First();
            Cell nextContact = nextComponent.Contacts.OrderBy(c => discreteField.GetDistanceBetweenCells(nextComponent.Contacts[0], c)).First();

            // discreteField.WaveAlgorithm(currentContact, nextContact);
            // discreteField.LimitedWaveAlgorithm(currentContact, nextContact);
             discreteField.BidirectionalWaveAlgorithm(currentContact,nextContact);

            currentComponent.IsConnected = true;
            nextComponent.IsConnected = true;
        }
    }
}



public class DiscreteField
{
    private Cell[,] cells; // Двумерный массив ячеек, представляющих дискретное поле
    public int Width { get; private set; } // Ширина поля
    public int Height { get; private set; } // Высота поля

    public List<Component> Components; // Список всех компонентов на поле

    private int componentIdCounter = 1; // Счетчик для присвоения уникальных идентификаторов компонентам

    // Конструктор
    public DiscreteField(int width, int height)
    {
        Width = width;
        Height = height;
        cells = new Cell[height, width];
        Components = new List<Component> { };
    }

    //Инициализация из файла
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

    // Обход поля и поиск компонентов
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

    
    //Поиск всех ячеек, принадлежащих компоненту
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

    //Рекурсивное добавление соседей в очередь
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


    // Конвертация символа в ячейку 
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

   
    // Выбор состояния ячейки на основе символа
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


    //Установка ячейки
    public void SetCell(int x, int y, Cell cell)
    {
        cells[y, x] = cell;
    }

    // Получение ячейки
    public Cell GetCell(int x, int y)
    {
        return cells[y, x];
    }


    // Указание соседей для всех ячеек
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

    // Получение ячейки или null, если координаты выходят за пределы поля
    private Cell GetCellOrNull(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null;
        return GetCell(x, y);
    }

    //Вывод дискретного поля в консоль
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
                        SetConsoleColor(ConsoleColor.Gray, ConsoleColor.Black);
                        Console.Write("E ");
                        break;
                    case CellState.Obstacle:
                        SetConsoleColor(ConsoleColor.Red, ConsoleColor.Black);
                        Console.Write("O ");
                        break;
                    case CellState.ContainsComponent:
                        SetConsoleColor(ComponentColors[cell.Component.TraceId % ComponentColors.Length], ConsoleColor.Black);
                        Console.Write($"{cell.Component.TraceId} ");
                        break;
                    case CellState.ContainsComponentContact:
                        SetConsoleColor(ComponentColors[cell.TraceId % ComponentColors.Length], ConsoleColor.Black);
                        Console.Write(". ");
                        break;
                    case CellState.ContainsWire:
                        SetConsoleColor(ComponentColors[cell.TraceId % ComponentColors.Length ], ConsoleColor.Black);
                        Console.Write("W ");
                        break;
                    default:
                        throw new ArgumentException($"Unknown cell state: {cell.State}");
                }
            }
            Console.WriteLine();
        }
        ResetConsoleColor();
    }

    //Волновой алгоритм
    public void WaveAlgorithm(Cell startCell, Cell endCell)
    {
        //Очищаем информацию о прохождении ячеек
        ClearPassInfo();

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

        //Строим путь

        List<Cell> path = ReconstructPath(startCell, endCell);
        foreach (var cell in path)
            cell.State = CellState.ContainsWire;

    }

    // Волновой алгоритм с ограничением распостранения волны
    public void LimitedWaveAlgorithm(Cell startCell, Cell endCell)
    {

        //Очищаем информацию о прохождении ячеек
        ClearPassInfo();

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

        //Строим путь

        List<Cell> path = ReconstructPath(startCell, endCell);
        foreach (var cell in path)
            cell.State = CellState.ContainsWire;


    }

    //Встречный волновой алгоритм
    public void BidirectionalWaveAlgorithm(Cell startCell, Cell endCell)
    {
        //Очищаем информацию о прохождении ячеек
        ClearPassInfo();

        // Инициализация начальной и конечной ячеек
        startCell.PassInfo.IsPassed = true;
        startCell.PassInfo.Weight = 0;
        endCell.PassInfo.IsPassed = true;
        endCell.PassInfo.Weight = 0;

        // Создание очередей для начальной и конечной ячеек
        Queue<Cell> startQueue = new Queue<Cell>();
        startQueue.Enqueue(startCell);
        Queue<Cell> endQueue = new Queue<Cell>();
        endQueue.Enqueue(endCell);

        bool meetInTheMiddle = false;
        Cell meetingCell = null;

        while (startQueue.Count > 0 && endQueue.Count > 0)
        {
            // Расширение фронта волны, начинающейся от startCell
            Cell currentStartCell = startQueue.Dequeue();
            ExpandFrontier(currentStartCell, endQueue, ref meetInTheMiddle, ref meetingCell);

            // Расширение фронта волны, начинающейся от endCell
            Cell currentEndCell = endQueue.Dequeue();
            ExpandFrontier(currentEndCell, startQueue, ref meetInTheMiddle, ref meetingCell);

            if (meetInTheMiddle)
                break;
        }

        PrintPassWeights();

        if (meetingCell != null)
        {
            // Восстановление пути
            List<Cell> path = ReconstructPath(startCell, meetingCell);
            path.AddRange(ReconstructPath(meetingCell, endCell));

            // Установка ячеек, содержащих провод
            foreach (var cell in path)
                cell.State = CellState.ContainsWire;
        }
    }

    // Дополнительный Метод встречного волнового алгоритма
    private void ExpandFrontier(Cell currentCell, Queue<Cell> oppositeQueue, ref bool meetInTheMiddle, ref Cell meetingCell)
    {
        if (IsCellAvaliable(currentCell, currentCell.DownCell, oppositeQueue))
        {
            int weight = currentCell.PassInfo.Weight + 1;
            currentCell.DownCell.PassInfo.Weight = weight;
            currentCell.DownCell.PassInfo.IsPassed = true;
            CheckMeetingPoint(currentCell.DownCell, oppositeQueue, ref meetInTheMiddle, ref meetingCell);
            currentCell.DownCell.TraceId = currentCell.TraceId;
            oppositeQueue.Enqueue(currentCell.DownCell);
        }

        if (IsCellAvaliable(currentCell, currentCell.UpCell, oppositeQueue))
        {
            int weight = currentCell.PassInfo.Weight + 1;
            currentCell.UpCell.PassInfo.Weight = weight;
            currentCell.UpCell.PassInfo.IsPassed = true;
            CheckMeetingPoint(currentCell.UpCell, oppositeQueue, ref meetInTheMiddle, ref meetingCell);
            currentCell.UpCell.TraceId = currentCell.TraceId;
            oppositeQueue.Enqueue(currentCell.UpCell);
        }

        if (IsCellAvaliable(currentCell, currentCell.LeftCell, oppositeQueue))
        {
            int weight = currentCell.PassInfo.Weight + 1;
            currentCell.LeftCell.PassInfo.Weight = weight;
            currentCell.LeftCell.PassInfo.IsPassed = true;
            CheckMeetingPoint(currentCell.LeftCell, oppositeQueue, ref meetInTheMiddle, ref meetingCell);
            currentCell.LeftCell.TraceId = currentCell.TraceId;
            oppositeQueue.Enqueue(currentCell.LeftCell);
        }

        if (IsCellAvaliable(currentCell, currentCell.RightCell, oppositeQueue))
        {
            int weight = currentCell.PassInfo.Weight + 1;
            currentCell.RightCell.PassInfo.Weight = weight;
            currentCell.RightCell.PassInfo.IsPassed = true;
            CheckMeetingPoint(currentCell.RightCell, oppositeQueue, ref meetInTheMiddle, ref meetingCell);
            currentCell.RightCell.TraceId = currentCell.TraceId;
            oppositeQueue.Enqueue(currentCell.RightCell);
        }
    }

    // Дополнительный Метод встречного волнового алгоритма
    private bool IsCellAvaliable(Cell currentCell,Cell nextCell, Queue<Cell> oppositeQueue)
    {
        return nextCell != null
        && (!nextCell.PassInfo.IsPassed)
        && (nextCell.State == CellState.Empty || (nextCell.State == CellState.ContainsWire && nextCell.TraceId == currentCell.TraceId));
    }

    // Дополнительный Метод встречного волнового алгоритма
    private void CheckMeetingPoint(Cell currentCell, Queue<Cell> oppositeQueue, ref bool meetInTheMiddle, ref Cell meetingCell)
    {
        foreach (var cell in oppositeQueue)
        {
            if (cell.X == currentCell.X && cell.Y == currentCell.Y)
            {
                meetInTheMiddle = true;
                meetingCell = currentCell;
                break;
            }
        }
    }



    // Проверка ячейки на доступность в волновом алгоритме
    private bool IsCellAvaliable(Cell nextCell, Cell endCell)
    {
        return nextCell != null
        && (!nextCell.PassInfo.IsPassed)
        && (nextCell.State == CellState.Empty || nextCell == endCell || (nextCell.State == CellState.ContainsWire && nextCell.TraceId == endCell.TraceId));
    }

    // Доп проверка для волнового алгоритма с ограничением
    private bool IsInLimit(Cell nextCell, Cell endCell, Cell startCell)
    {
        return nextCell.X >= Math.Min(startCell.X, endCell.X)
           && nextCell.X <= Math.Max(startCell.X, endCell.X)
           && nextCell.Y >= Math.Min(startCell.Y, endCell.Y)
           && nextCell.Y <= Math.Max(startCell.Y, endCell.Y);
    }


    // Поиск кратчайшего пути по фронту
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
                nextCell.TraceId = endCell.TraceId;
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

    // Очистка фронта
    public void ClearPassInfo()
    {
        foreach (var cell in cells)
        {
            cell.PassInfo.IsPassed = false;
            cell.PassInfo.Weight = 0;
            cell.PassInfo.Direction = Direction.Down;
        }
    }

    // Поиск расстояния между двумя компонентами
    public int GetDistanceBetweenComponents(Component component1, Component component2)
    {
        // Проверяем, что компоненты имеют одинаковый TraceId
        if (component1.TraceId != component2.TraceId)
            return -1; // Если TraceId разные, возвращаем -1 для ошибки

        // Находим ячейки, содержащие контакты компонентов
        List<Cell> component1Contacts = component1.Contacts;
        List<Cell> component2Contacts = component2.Contacts;

        // Находим минимальное расстояние между ячейками контактов
        int minDistance = int.MaxValue;
        foreach (var contact1 in component1Contacts)
        {
            foreach (var contact2 in component2Contacts)
            {
                int distance = Math.Abs(contact1.X - contact2.X) + Math.Abs(contact1.Y - contact2.Y);
                if (distance < minDistance)
                    minDistance = distance;
            }
        }

        return minDistance;
    }

    //Отладочный метод вывода
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
    //Отладочный метод вывода
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
    //Отладочный метод вывода
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
    //Отладочный метод вывода
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
    //Отладочный метод вывода
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
    //Отладочный метод вывода
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
    //Отладочный метод вывода
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

    
    // Цвета для вывода в консоль
    private static readonly ConsoleColor[] ComponentColors = 
        {
    ConsoleColor.Green, ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.Yellow,
    ConsoleColor.DarkGreen, ConsoleColor.DarkCyan, ConsoleColor.DarkMagenta, ConsoleColor.DarkYellow
    };

    // Метод для работы с цветом
    private static void SetConsoleColor(ConsoleColor foreground, ConsoleColor background)
    {
        Console.ForegroundColor = foreground;
        Console.BackgroundColor = background;
    }

    // Метод для работы с цветом
    private static void ResetConsoleColor()
    {
        SetConsoleColor(ConsoleColor.White, ConsoleColor.Black);
    }

    // Расстояние между двумя ячейками
    public int GetDistanceBetweenCells(Cell cell1, Cell cell2)
    {
        return Math.Abs(cell1.X - cell2.X) + Math.Abs(cell1.Y - cell2.Y);
    }
}

// Информация для фронта волны
public class PassInfo
{
    public bool IsPassed = false; // Флаг, указывающий, что ячейка была пройдена
    public int Weight = 0; // Вес ячейки (расстояние от начальной ячейки)
    public Direction Direction; // Направление движения при прохождении ячейки
};

//Компонент
public class Component
{
    public List<Cell> Contacts; // Список контактов компонента
    public bool IsConnected = false; // Флаг, указывающий, что компонент подключен
    public int Id; // Уникальный идентификатор компонента
    public int TraceId; // Идентификатор цепи, к которой относится компонент
}

// Состояние ячейки
public enum CellState
{
    Empty, // Пустая ячейка
    Obstacle, // Ячейка-препятствие
    ContainsComponent, // Ячейка, содержащая компонент
    ContainsComponentContact, // Ячейка, содержащая контакт компонента
    ContainsWire // Ячейка, содержащая провод
};


// Путевые координаты, которых нет)))
public enum Direction
{
    Down,
    Left,
    Up,
    Right
}

//Ячейка дискретного поля
public class Cell
{
    public int Id; // Уникальный идентификатор ячейки
    public int X; // Координата X ячейки
    public int Y; // Координата Y ячейки
    public Cell RightCell; // Правая соседняя ячейка
    public Cell LeftCell; // Левая соседняя ячейка
    public Cell UpCell; // Верхняя соседняя ячейка
    public Cell DownCell; // Нижняя соседняя ячейка

    public int TraceId = -1; // Идентификатор цепи, к которой относится ячейка

    public PassInfo PassInfo; // Информация о прохождении ячейки
    public CellState State; // Состояние ячейки
    public Component Component; // Компонент, содержащийся в ячейке

    public bool IsEqual(Cell other)
    {
        return Id == other.Id;
    }
};



