using SarsaBrain;

namespace Snake;

public class Game1 : ScenarioBase<Snake, Direction, SnakeState>
{
    private Direction _nextDirection;
    public readonly Field Field;
    public List<Pos> Walls { get; private set; } = new();
    private int _foodHungry = 200;
    private int _foodCount = 0;
    private readonly int _maxFoodForCounting = 2;
    private double _minDistance = int.MaxValue;
    private double _lastDistance = int.MaxValue;
    private IBrainStatisticsCollector<Direction> _brainStatisticsCollector;

    public IReadOnlyList<Pos> SnakeBody => Agent.SnakeBody;
    public int Score { get; private set; }
    private int MaxScore { get; set; }
    public IReadOnlyList<Pos> FoodLocation { get; private set; }
    public Pos TargetFood { get; private set; }

    public event Action<int> OnGameRestart;

    public IReadOnlyList<float> Sensors { get; private set; }
    public List<(int, Pos)> SensorsAdditionalInfo { get; private set; } = new();
    public List<double> ErrorsP => Errors;

    public Game1(int columns, int rows, Snake snake,
        IBrainStatisticsCollector<Direction> brainStatisticsCollector) : base(snake)
    {
        _brainStatisticsCollector = brainStatisticsCollector;
        Field = new Field(columns, rows);
        Sensors = new List<float>();
        NextEpisode();
    }

    protected override double[] GetSensorsInState(SnakeState state)
    {
        SensorsAdditionalInfo.Clear();
        const int sensorKinds = 4;
        const int wallSensor = 0;
        const int appleSensor = 1;
        const int tailSensor = 2;
        const int obstSensor = 3;
        var dirs = Pos.Dir8.Count;
        var maxRange = Math.Max(Field.Width, Field.Height) + 1;
        var sensors = new double[sensorKinds, dirs];
        
        for (var i = 0; i < dirs; i++)
        {
            var dir = Pos.Dir8[i];
        
            bool foundWall = false, foundApple = false, foundTail = false, foundObst = false;
            for (var distance = 1; distance <= maxRange; distance++)
            {
                var pos = state.Head + distance * dir;
                var sensor = Math.Round(1f / distance, 2);
        
                if (!foundWall && !IsValidPosition(pos))
                {
                    if (distance <= maxRange / 3)
                    {
                        sensors[wallSensor, i] = sensor;
                        SensorsAdditionalInfo.Add((0, pos));
                        break;
                    }
                }
        
                if (!foundApple && FoodLocation.Contains(pos))
                {
                    foundApple = true;
                    sensors[appleSensor, i] = sensor;
                    SensorsAdditionalInfo.Add((1, pos));
                }
        
                if (!foundTail && Agent.SnakeBody.Contains(pos))
                {
                    foundTail = true;
                    sensors[tailSensor, i] = sensor;
                    SensorsAdditionalInfo.Add((2, pos));
                }
        
                if (!foundObst && Walls.Contains(pos))
                {
                    foundObst = true;
                    sensors[obstSensor, i] = sensor;
                    SensorsAdditionalInfo.Add((3, pos));
                }
            }
        }
        
        var gatheredSensors = new List<double>();
        for (var i = 0; i < sensorKinds; i++)
        {
            for (var j = 0; j < dirs; j++)
            {
                gatheredSensors.Add(sensors[i, j]);
            }
        }
        
        gatheredSensors.Add(_nextDirection == Direction.Down ? 1 : 0);
        gatheredSensors.Add(_nextDirection == Direction.Up ? 1 : 0);
        gatheredSensors.Add(_nextDirection == Direction.Left ? 1 : 0);
        gatheredSensors.Add(_nextDirection == Direction.Right ? 1 : 0);
        
        gatheredSensors.Add(Math.Round(TargetFood.X / (float)Field.Width, 2));
        gatheredSensors.Add(Math.Round(TargetFood.Y / (float)Field.Height, 2));
        gatheredSensors.Add(Math.Round(Agent.Head.X / (float)Field.Width, 2));
        gatheredSensors.Add(Math.Round(Agent.Head.Y / (float)Field.Height, 2));
        gatheredSensors.Add(Math.Round(_lastDistance / Pos.Distance(new Pos(0, 0),
            new Pos(Field.Width, Field.Height)), 2));
        gatheredSensors.Add(Math.Round(_minDistance / Pos.Distance(new Pos(0, 0),
            new Pos(Field.Width, Field.Height)), 2));
        
        gatheredSensors.Add(Math.Round((Agent.Head.Y - TargetFood.Y) / (float)Field.Height, 2));
        gatheredSensors.Add(Math.Round((Agent.Head.X - TargetFood.X) / (float)Field.Width, 2));
        gatheredSensors.Add(Math.Round(
            GetDistanceToFood(state.Head) / (float)Pos.Distance(new Pos(0, 0), new Pos(Field.Width, Field.Height)), 2));
        gatheredSensors.Add(Math.Round(_minDistance / Pos.Distance(new Pos(0, 0), new Pos(Field.Width, Field.Height)), 2));
        
        for (int i = 0; i < Field.Width; i++)
        {
            for (int j = 0; j < Field.Height; j++)
            {
                var value = new Pos(i, j);
                if (SnakeBody.Contains(value))
                {
                    gatheredSensors.Add(value == SnakeBody[0] ? 0.5 : -1);
                    continue;
                }

                if (FoodLocation.Contains(value))
                {
                    gatheredSensors.Add(1);
                    continue;
                }

                gatheredSensors.Add(0);
            }
        }

        return gatheredSensors.ToArray();
    }

    protected override List<Experience<double[], Direction>> GetAllPossibleFutureStates()
    {
        var directions = new List<Experience<double[], Direction>>();

        for (var index = 0; index < Pos.Dir4.Count; index++)
        {
            var pos = Pos.Dir4[index];
            var potentialHeadPos = Agent.Head + pos;
            var snakeState = new SnakeState()
            {
                Head = potentialHeadPos
            };

            var calculatedPosition = CalculateRewards(potentialHeadPos);
            var sensors = GetSensorsInState(snakeState);

            directions.Add(new Experience<double[], Direction>()
            {
                Reward = calculatedPosition.reward,
                Done = calculatedPosition.failed,
                NextState = sensors,
            });
        }

        return directions;
    }

    private bool IsFailed(Pos newPosition)
    {
        if (newPosition.X < 0 || newPosition.X >= Field.Width || newPosition.Y < 0 || newPosition.Y >= Field.Height)
        {
            return true;
        }

        if (Walls.Contains(newPosition))
        {
            return true;
        }
        
        var snakeHead = newPosition;
        for (var i = 1; i < Agent.SnakeBody.Count; i++)
        {
            if (Agent.SnakeBody[i] == snakeHead)
            {
                return true;
            }
        }

        if (_foodHungry - 1 == 0)
        {
            return true;
        }

        return false;
    }

    protected override float ReleaseDecision(Direction action, SnakeState state)
    {
        var newHeadPos = GetNewSnakeHeadPos(Agent.Head, action);
        var calculatedPosition = CalculateRewards(newHeadPos);
        Console.WriteLine(calculatedPosition.reward);

        if (calculatedPosition.failed || _foodHungry == 0)
        {
            Done = true;
            return calculatedPosition.reward;
        }

        MoveSnake(newHeadPos, calculatedPosition.ateFood);

        var distanceToFood = calculatedPosition.distanceToFood;
        if (distanceToFood < _minDistance)
        {
            _minDistance = distanceToFood;
        }

        _lastDistance = distanceToFood;

        if (calculatedPosition.ateFood)
        {
            _foodHungry += 100;
            Score++;
            MaxScore = Math.Max(Score, MaxScore);
            SpawnFood();
        }

        _foodHungry--;

        return calculatedPosition.reward;
    }

    private (bool failed, bool ateFood, float reward, double distanceToFood) CalculateRewards(Pos pos)
    {
        var failed = IsFailed(pos);
        if (failed) return (true, false, RatePosition(failed: true), 0);

        var ateFood = FoodLocation.Contains(pos);
        if (ateFood) return (false, true, RatePosition(ateFood: true), 0);

        var distanceToFood = GetDistanceToFood(pos);

        return (false, false, RatePosition(distanceToFood: distanceToFood), distanceToFood);
    }

    private float RatePosition(bool failed = false, bool ateFood = false, double distanceToFood = 0)
    {
        //var rewardForScore = Math.Sqrt(MaxScore + 1) * Math.Max(1f, 1.0f / Score);
        var rewardForScore = 7f;
        if (failed) return (float)(Score <= MaxScore ? -rewardForScore : rewardForScore);

        if (!ateFood) return distanceToFood < _minDistance ? 1f : -0.1f;

        //var ratePosition = (float)Math.Sqrt(Score + 1) * Math.Max(1f, 2.0f / Score);
        var ratePosition = 5f;
        return ratePosition;
    }

    private void MoveSnake(Pos newPosition, bool ateFood)
    {
        Agent.MoveTo(newPosition, ateFood);
    }

    private Pos GetNewSnakeHeadPos(Pos head, Direction direction)
    {
        switch (direction)
        {
            case Direction.Left:
                head.X--;
                break;
            case Direction.Right:
                head.X++;
                break;
            case Direction.Up:
                head.Y--;
                break;
            case Direction.Down:
                head.Y++;
                break;
        }

        _nextDirection = direction;

        return head;
    }

    private bool IsValidPosition(Pos pos)
    {
        return pos.X >= 0 && pos.X < Field.Width && pos.Y >= 0 && pos.Y < Field.Height;
    }

    private double GetDistanceToFood(Pos head)
    {
        return FoodLocation.Select(food => Pos.Distance(head, food)).DefaultIfEmpty(0).Min();
    }

    public BrainStatisticSnake BrainStatistic()
    {
        var statistics = _brainStatisticsCollector.GetStatistics();

        return new BrainStatisticSnake()
        {
            FoodHungry = _foodHungry,
            FoodCollected = _foodCount,
            MaxScore = MaxScore,
            Reward = statistics.Reward,
            QValues = statistics.QValues,
            Sensors = statistics.Sensors,
            Exploration = statistics.Exploration,
            CurrentAction = statistics.CurrentAction
        };
    }

    protected override void NextEpisode()
    {
        base.NextEpisode();
        OnGameRestart?.Invoke(Score);

        SpawnSnake();
        SpawnWalls(0);
        SpawnFood(5);
        Score = 0;
        _foodHungry = 200;
        _minDistance = int.MaxValue;
        _lastDistance = int.MaxValue;
        _nextDirection = Direction.Up;
    }

    private void SpawnWalls(int percent)
    {
        Walls.Clear();
        var amountWithWalls = (Field.Height * Field.Width) * percent / 100.0;

        Walls = Field.GetRandomFreeCells(Field, SnakeBody.ToList(), (int)amountWithWalls);
    }

    private void SpawnSnake()
    {
        Agent.Create(new Pos(Random.Shared.Next(0, Field.Height - 5),
            Random.Shared.Next(0, Field.Width)));

        for (var i = 0; i < 5; i++)
        {
            Agent.AddBodyPart(new Pos() { X = 0, Y = 1 });
        }
    }

    private void SpawnFood(int count = 1)
    {
        var taken = SnakeBody.ToList();
        taken.AddRange(Walls);
        if (TargetFood != null)
        {
            taken.Add(TargetFood);
        }

        var free = Field.GetFreeCells(Field, taken);

        FoodLocation = Enumerable.Range(0, count)
            .Select(x => free[Random.Shared.Next(0, free.Count)])
            .ToList();
        TargetFood = FoodLocation.FirstOrDefault();
        _minDistance = GetDistanceToFood(Agent.Head);
    }
}