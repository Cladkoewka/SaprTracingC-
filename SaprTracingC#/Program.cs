/// <summary>
/// По проблемам:
///     - Нет критерия по изгибам, путевые координаты ломали все, убрал
///     - Рассмотрен идеальный случай
///     - Элемент принадлежит одной трассе
///     - Порядок цепей выбирается руками
///     - Не соблюдены принципы ООП, Абсолютное отсутствие инкапсуляции
/// </summary>

public class Program
{
    // Точка входа
    public static void Main()
    {
        // Создание дискретного поля, инициализация из файла
        DiscreteField field = new DiscreteField(16, 16);
        field.InitializeFieldFromFile("C:/Users/semen/Desktop/_/УчебаСем4/SAPR_Trasing/SAPR_Trasing/board.txt");

        // Вывод дискретного поля в консоль
        field.PrintField();

        // Пользовательский интерфейс
        ConsoleUI(field);
    }

    // Консольный пользовательский интерфейс
    private static void ConsoleUI(DiscreteField field)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Выберите трассу для прокладки (1-3) или 0 для выхода:");
            int traceId = int.Parse(Console.ReadLine());

            if (traceId == 0)
                break;

            Console.WriteLine("Выберите волновой алгоритм (1-Простой, 2-Ограниченный, 3-Встречный):");
            int waveAlgorithm = int.Parse(Console.ReadLine());

            Console.WriteLine();

            // Проложение проводов
            Solution.TraceElements(field, traceId, waveAlgorithm);
            field.PrintField();
        }
    }
}


// Класс решения задачи
public static class Solution
{
    // Трассировка всех элементов i-ой Трассы
    public static void TraceElements(DiscreteField discreteField, int traceId, int waveAlgorithm)
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
            Contact currentContact = currentComponent.Contacts.OrderBy(c => discreteField.GetDistanceBetweenCells(currentComponent.Contacts[0].Cell, c.Cell)).Where(c => !c.IsOccupied).First();
            Contact nextContact = nextComponent.Contacts.OrderBy(c => discreteField.GetDistanceBetweenCells(nextComponent.Contacts[0].Cell, c.Cell)).Where(c => !c.IsOccupied).First();

            // Делаем контакты занятыми
            currentContact.IsOccupied = true;
            nextContact.IsOccupied = true; 


            // Выбор одного из трех волновых алгоритмов

            if (waveAlgorithm == 1)
                discreteField.WaveAlgorithm(currentContact.Cell, nextContact.Cell);
            else if (waveAlgorithm == 2)
            discreteField.LimitedWaveAlgorithm(currentContact.Cell, nextContact.Cell);
            else if (waveAlgorithm == 3)
                discreteField.BidirectionalWaveAlgorithm(currentContact.Cell, nextContact.Cell);

            currentComponent.IsConnected = true;
            nextComponent.IsConnected = true;
        }
    }
}


// Дискретное рабочее поле, с методами волновых алгоритмов, чтения и вывода
// Писали совместно
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
    //Семён
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

    // Обход поля и поиск компонентов (Для инициализации рабочего поля и компонентов)
    //Семён
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
                        TraceId = cell.TraceId
                    };

                    Components.Add(component);

                    FindFullComponent(cell, component);

                    // Создаем контакты для компонента
                    CreateContactsForComponent(component, cell);
                }
            }
        }
    }

    // Поиск контактов компонента (Для инициализации компонентов)
    //Семён
    private void CreateContactsForComponent(Component component, Cell startCell)
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

    //Поиск всех ячеек, принадлежащих компоненту (Для инициализации компонентов)
    ////Семён
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

    //Рекурсивное добавление соседей в очередь (Для инициализации рабочего поля и компонентов)
    //Семён
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
            Contact newContact = new Contact(cell.RightCell);
            if (!component.Contacts.Contains(newContact))
                component.Contacts.Add(newContact);
        }

        if (cell.LeftCell != null && cell.LeftCell.State == CellState.ContainsComponent && cell.LeftCell.Component == null)
        {
            cell.LeftCell.Component = component;
            queue.Enqueue(cell.LeftCell);
        }
        if (cell.LeftCell != null && cell.LeftCell.State == CellState.ContainsComponentContact)
        {
            cell.LeftCell.TraceId = component.TraceId;
            Contact newContact = new Contact(cell.LeftCell);
            if (!component.Contacts.Contains(newContact))
                component.Contacts.Add(newContact);
        }

        if (cell.UpCell != null && cell.UpCell.State == CellState.ContainsComponent && cell.UpCell.Component == null)
        {
            cell.UpCell.Component = component;
            queue.Enqueue(cell.UpCell);
        }
        if (cell.UpCell != null && cell.UpCell.State == CellState.ContainsComponentContact)
        {
            cell.UpCell.TraceId = component.TraceId;
            Contact newContact = new Contact(cell.UpCell);
            if (!component.Contacts.Contains(newContact))
                component.Contacts.Add(newContact);
        }

        if (cell.DownCell != null && cell.DownCell.State == CellState.ContainsComponent && cell.DownCell.Component == null)
        {
            cell.DownCell.Component = component;
            queue.Enqueue(cell.DownCell);
        }
        if (cell.DownCell != null && cell.DownCell.State == CellState.ContainsComponentContact)
        {
            cell.DownCell.TraceId = component.TraceId;
            Contact newContact = new Contact(cell.DownCell);
            if (!component.Contacts.Contains(newContact))
                component.Contacts.Add(newContact);
        }
    }


    // Конвертация символа в ячейку (Для инициализации рабочего поля)
    // Ника
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


    // Выбор состояния ячейки на основе символа (Для инициализации рабочего поля)
    // Настя
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

    // Указание соседей для всех ячеек (Для инициализации рабочего поля)
    // Ника
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
    // Ника
    private Cell GetCellOrNull(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null;
        return GetCell(x, y);
    }

    //Вывод дискретного поля в консоль
    // Настя
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
                        SetConsoleColor(ComponentColors[cell.TraceId % ComponentColors.Length], ConsoleColor.Black);
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

    //Волновой алгоритм (Простой)
    //Семён
    /// <summary>
    /// Базовый волновой алгоритм
    /// </summary>
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

        // Пока не пройдем все ячейки итеративно проходим по всем соседям
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
        {
            cell.TraceId = startCell.TraceId;
            cell.State = CellState.ContainsWire;
        }

    }

    // Волновой алгоритм (с ограничением распостранения волны)
    // Ника
    /// <summary>
    /// Ограниченный волновой алгоритм, где фронт
    /// распостранения волны не выходит за X,Y координаты 
    /// startCell и endCell.
    /// Если в пределах ограниченной области нельзя проложить путь
    /// он не прокладывается.
    /// </summary>
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

        // Пока не пройдем все ячейки итеративно проходим по всем соседям
        while (queue.Count > 0)
        {
            Cell currentCell = queue.Dequeue();

            //Добавлена проверка на выход за прямоугольную область
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
        {
            cell.TraceId = startCell.TraceId;
            cell.State = CellState.ContainsWire;
        }


    }


    // Волновой алгоритм (встречная волна)
    // Настя
    /// <summary>
    /// Алгоритм, где фронт распостраняется от начальной
    /// и конечной точки одновременно.
    /// При встрече фронтов в точке meetCell
    /// Строим путь от startCell до meetCell и от meetCell.
    /// </summary>
    public void BidirectionalWaveAlgorithm(Cell startCell, Cell endCell)
    {
        // Очищаем информацию о прохождении ячеек
        ClearPassInfo();

        // Инициализируем начальные ячейки
        startCell.PassInfo.IsPassed = true;
        startCell.PassInfo.Weight = 0;
        endCell.PassInfo.IsPassed = true;
        endCell.PassInfo.Weight = 0;

        // Создаем очереди для прямого и обратного фронта
        Queue<Cell> forwardQueue = new Queue<Cell>();
        Queue<Cell> backwardQueue = new Queue<Cell>();

        forwardQueue.Enqueue(startCell);
        backwardQueue.Enqueue(endCell);

        // Флаги 
        bool pathFound = false;
        bool meetForward = false;
        Cell meetingCellOther = null;
        Cell meetingCell = null;


        while (forwardQueue.Count > 0 || backwardQueue.Count > 0)
        {
            // Обработка ячеек переднего фронта
            if (forwardQueue.Count > 0)
            {
                Cell currentCell = forwardQueue.Dequeue();

                // Проверка всех соседей на наличие в задней очереди
                if (backwardQueue.Contains(currentCell.DownCell))
                {
                    pathFound = true;
                    meetingCellOther = currentCell.DownCell;
                    meetingCell = currentCell;
                }
                else if (backwardQueue.Contains(currentCell.UpCell))
                {
                    pathFound = true;
                    meetingCellOther = currentCell.UpCell;
                    meetingCell = currentCell;
                }
                else if (backwardQueue.Contains(currentCell.LeftCell))
                {
                    pathFound = true;
                    meetingCellOther = currentCell.LeftCell;
                    meetingCell = currentCell;
                }
                else if (backwardQueue.Contains(currentCell.RightCell))
                {
                    pathFound = true;
                    meetingCellOther = currentCell.RightCell;
                    meetingCell = currentCell;
                }

                // Проход по соседям
                if (IsCellAvaliableBidirect(currentCell.DownCell, startCell))
                {
                    int weight = currentCell.PassInfo.Weight + 1;
                    currentCell.DownCell.PassInfo.Weight = weight;
                    currentCell.DownCell.PassInfo.IsPassed = true;
                    forwardQueue.Enqueue(currentCell.DownCell);
                }

                if (IsCellAvaliableBidirect(currentCell.UpCell, startCell))
                {
                    int weight = currentCell.PassInfo.Weight + 1;
                    currentCell.UpCell.PassInfo.Weight = weight;
                    currentCell.UpCell.PassInfo.IsPassed = true;
                    forwardQueue.Enqueue(currentCell.UpCell);
                }

                if (IsCellAvaliableBidirect(currentCell.LeftCell, startCell))
                {
                    int weight = currentCell.PassInfo.Weight + 1;
                    currentCell.LeftCell.PassInfo.Weight = weight;
                    currentCell.LeftCell.PassInfo.IsPassed = true;
                    forwardQueue.Enqueue(currentCell.LeftCell);
                }

                if (IsCellAvaliableBidirect(currentCell.RightCell, startCell))
                {
                    int weight = currentCell.PassInfo.Weight + 1;
                    currentCell.RightCell.PassInfo.Weight = weight;
                    currentCell.RightCell.PassInfo.IsPassed = true;
                    forwardQueue.Enqueue(currentCell.RightCell);
                }
            }

            // Обработка ячеек заднего фронта
            if (backwardQueue.Count > 0)
            {
                Cell currentCell = backwardQueue.Dequeue();

                // Проверка всех соседей на наличие в передней очереди
                if (forwardQueue.Contains(currentCell.DownCell))
                {
                    pathFound = true;
                    meetForward = true;
                    meetingCellOther = currentCell.DownCell;
                    meetingCell = currentCell;
                }
                else if (forwardQueue.Contains(currentCell.UpCell))
                {
                    pathFound = true;
                    meetForward = true;
                    meetingCellOther = currentCell.UpCell;
                    meetingCell = currentCell;
                }
                else if (forwardQueue.Contains(currentCell.LeftCell))
                {
                    pathFound = true;
                    meetForward = true;
                    meetingCellOther = currentCell.LeftCell;
                    meetingCell = currentCell;
                }
                else if (forwardQueue.Contains(currentCell.RightCell))
                {
                    pathFound = true;
                    meetForward = true;
                    meetingCellOther = currentCell.RightCell;
                    meetingCell = currentCell;
                }

                // Проход по соседям
                if (IsCellAvaliableBidirect(currentCell.DownCell, endCell))
                {
                    int weight = currentCell.PassInfo.Weight + 1;
                    currentCell.DownCell.PassInfo.Weight = weight;
                    currentCell.DownCell.PassInfo.IsPassed = true;
                    backwardQueue.Enqueue(currentCell.DownCell);
                }

                if (IsCellAvaliableBidirect(currentCell.UpCell, endCell))
                {
                    int weight = currentCell.PassInfo.Weight + 1;
                    currentCell.UpCell.PassInfo.Weight = weight;
                    currentCell.UpCell.PassInfo.IsPassed = true;
                    backwardQueue.Enqueue(currentCell.UpCell);
                }

                if (IsCellAvaliableBidirect(currentCell.LeftCell, endCell))
                {
                    int weight = currentCell.PassInfo.Weight + 1;
                    currentCell.LeftCell.PassInfo.Weight = weight;
                    currentCell.LeftCell.PassInfo.IsPassed = true;
                    backwardQueue.Enqueue(currentCell.LeftCell);
                }

                if (IsCellAvaliableBidirect(currentCell.RightCell, endCell))
                {
                    int weight = currentCell.PassInfo.Weight + 1;
                    currentCell.RightCell.PassInfo.Weight = weight;
                    currentCell.RightCell.PassInfo.IsPassed = true;
                    backwardQueue.Enqueue(currentCell.RightCell);
                }
            }


            if (pathFound)
            {
                List<Cell> forwardPath = new List<Cell>();
                List<Cell> backwardPath = new List<Cell>();

                // Строим путь от startCell до встречной ячейки и от endCell до встречной ячейки

                if (meetForward)
                {
                    forwardPath = ReconstructPath(startCell, meetingCell);
                    backwardPath = ReconstructPath(endCell, meetingCellOther);
                }
                else
                {
                    forwardPath = ReconstructPath(startCell, meetingCellOther);
                    backwardPath = ReconstructPath(endCell, meetingCell);
                }
                

                // Полный путь
                List<Cell> fullPath = new List<Cell>();

                // Отмечаем ячейки, содержащие провод
                foreach (var cell in forwardPath)
                {
                    if (!fullPath.Contains(cell))
                        fullPath.Add(cell);
                }
                foreach (var cell in backwardPath)
                {
                    if (!fullPath.Contains(cell))
                        fullPath.Add(cell);
                }

                // Прокладываем путь
                foreach (var cell in fullPath)
                {
                    cell.TraceId = startCell.TraceId;
                    cell.State = CellState.ContainsWire;
                }


                return;
            }
        }
    }

    // Проверка ячейки на проходимость в встречном волновом алгоритме
    // Настя
    private bool IsCellAvaliableBidirect(Cell cell, Cell targetCell)
    {
        return cell != null
            && !cell.PassInfo.IsPassed
            && cell.State == CellState.Empty || (cell.State == CellState.ContainsWire && cell.TraceId == targetCell.TraceId);
    }

    // Проверка ячейки на проходимость волновом алгоритме
    // Семён
    private bool IsCellAvaliable(Cell nextCell, Cell endCell)
    {
        return nextCell != null
        && (!nextCell.PassInfo.IsPassed)
        && (nextCell.State == CellState.Empty || nextCell == endCell || (nextCell.State == CellState.ContainsWire && nextCell.TraceId == endCell.TraceId));
    }

    // Доп проверка для волнового алгоритма с ограничением
    // Ника
    private bool IsInLimit(Cell nextCell, Cell endCell, Cell startCell)
    {
        return nextCell.X >= Math.Min(startCell.X, endCell.X)
           && nextCell.X <= Math.Max(startCell.X, endCell.X)
           && nextCell.Y >= Math.Min(startCell.Y, endCell.Y)
           && nextCell.Y <= Math.Max(startCell.Y, endCell.Y);
    }

    // Поиск кратчайшего пути по фронту
    // Семён
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

            if (currentCell.DownCell != null && currentCell.DownCell.PassInfo.Weight == currentCell.PassInfo.Weight - 1)
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
                //nextCell.TraceId = endCell.TraceId;
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
    // Семён
    public void ClearPassInfo()
    {
        foreach (var cell in cells)
        {
            cell.PassInfo.IsPassed = false;
            cell.PassInfo.Weight = 0;
        }
    }

    // Поиск расстояния между двумя компонентами
    // Настя
    public int GetDistanceBetweenComponents(Component component1, Component component2)
    {
        // Проверяем, что компоненты имеют одинаковый TraceId
        if (component1.TraceId != component2.TraceId)
            return -1; // Если TraceId разные, возвращаем -1 для ошибки

        // Находим ячейки, содержащие контакты компонентов
        List<Contact> component1Contacts = component1.Contacts;
        List<Contact> component2Contacts = component2.Contacts;

        // Находим минимальное расстояние между ячейками контактов
        int minDistance = int.MaxValue;
        foreach (var contact1 in component1Contacts)
        {
            foreach (var contact2 in component2Contacts)
            {
                int distance = Math.Abs(contact1.Cell.X - contact2.Cell.X) + Math.Abs(contact1.Cell.Y - contact2.Cell.Y);
                if (distance < minDistance)
                    minDistance = distance;
            }
        }

        return minDistance;
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
    // Ника
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
};

// Контакт компонента
public class Contact
{
    public Cell Cell { get; set; }
    public bool IsOccupied { get; set; }

    public Contact(Cell cell)
    {
        Cell = cell;
        IsOccupied = false;
    }

    public override bool Equals(object obj)
    {
        if (obj is Contact other)
        {
            return this.Cell.Equals(other.Cell);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return this.Cell.GetHashCode();
    }

    public static bool operator ==(Contact contact1, Contact contact2)
    {
        if (ReferenceEquals(contact1, contact2))
            return true;

        if (ReferenceEquals(contact1, null) || ReferenceEquals(contact2, null))
            return false;

        return contact1.Equals(contact2);
    }

    public static bool operator !=(Contact contact1, Contact contact2)
    {
        return !(contact1 == contact2);
    }
}
//Компонент
public class Component
{
    public List<Contact> Contacts { get; set; }
    public bool IsConnected = false;
    public int Id;
    public int TraceId;

    public Component()
    {
        Contacts = new List<Contact>();
    }
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