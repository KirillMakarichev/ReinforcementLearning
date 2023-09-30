using SarsaBrain;

namespace Snake
{
    public class Game
    {
        private Direction _nextDirection;
        public readonly Field Field;
        private readonly Snake _snake;
        private int _foodHungry = 200;
        private int _foodCount = 0;
        private readonly int _maxFoodForCounting = 2;
        private double _minDistance = int.MaxValue;
        private double _lastDistance = int.MaxValue;
        private IBrainStatisticsCollector<Direction> _brainStatisticsCollector;

        public IReadOnlyList<Pos> SnakeBody => _snake.SnakeBody;
        public int Score { get; private set; }
        private int MaxScore { get; set; }
        public IReadOnlyList<Pos> FoodLocation { get; private set; }
        public Pos TargetFood { get; private set; }

        public event Action<int> OnGameRestart;

        public IReadOnlyList<float> Sensors { get; private set; }
        public List<(int, Pos)> SensorsAdditionalInfo { get; private set; } = new List<(int, Pos)>();
        public List<double> Errors;

        public Game(int columns, int rows)
        {
            Field = new Field(columns, rows);
            _snake = new Snake(new ConstantsInitializer()
            {
                ExplorationDefault = 1,
                ExplorationDecay = 0.995,
                ExplorationMin = 0.01,
                LearningRate = 0.001,
                DiscountFactor = 0.999,
                ReplayMemoryCapacity = 10000,
                ReplayMemoryMinSize = 200,
                MiniBatchSize = 256,
            }, new SarsaBrain.NeuralNetworkSettings()
            {
                NumInputs = 33,
                NumOutputs = 4,
                NumHiddenLayers = 4,
                NumNeuronsInHiddenLayer = 25,
                LearningRate = 0.001
            });
            
            Sensors = new List<float>();
            _brainStatisticsCollector = _snake;
            RestartGame();
        }

        public void SetNextDirection(Direction direction)
        {
            if (IsOppositeDirection(_nextDirection, direction))
            {
                return;
            }

            _nextDirection = direction;
        }

        private List<(float reward, bool isFailed, List<double> sensors)> GetAllPossibleDirections(Pos headPos)
        {
            var directions = new List<(float reward, bool isFailed, List<double> sensors)>();

            foreach (var pos in Pos.Dir4)
            {
                var potentialHeadPos = headPos + pos;

                var ratedDirection = RatePossibleDirections(potentialHeadPos);
                var sensors = GatherSensors(ratedDirection.Item1, potentialHeadPos);

                directions.Add((ratedDirection.Item2, ratedDirection.Item1, sensors));
            }

            return directions;
        }

        public void Tick(bool isPc)
        {
            var sensorsBefore = GatherSensors(true, _snake.Head).ToArray();
            var potentialDirections = GetAllPossibleDirections(_snake.Head)
                .Select(x => new Experience<double[], Direction>()
                {
                    Reward = x.reward,
                    NextState = x.sensors.ToArray(),
                    Done = x.isFailed
                }).ToList();
            var chosenDirection = _snake.DecideAction(potentialDirections, sensorsBefore);
            if (isPc) SetNextDirection(chosenDirection);

            var newPosition = GetNewSnakeHeadPos();
            var (isFailed, reward) = MoveSnake(newPosition);

            var sensorsAfter = GatherSensors(!isFailed, _snake.Head).ToArray();
            _snake.DowngradeExploration();

            Errors = _snake.Learn(new Experience<double[], Direction>()
            {
                State = sensorsBefore,
                Action = chosenDirection, 
                Reward = reward, 
                NextState = sensorsAfter, 
                Done = isFailed
            });
            _foodHungry--;
            if (!isFailed && _foodHungry > 0) return;
            if (Score > MaxScore) MaxScore = Score;

            RestartGame();
            _snake.NextEpisode();
        }

        private (bool isFailed, float reward) MoveSnake(Pos newPosition)
        {
            var isFailed = IsFailed(newPosition);

            var reward = 0f;

            if (!isFailed)
            {
                if (FoodLocation.Contains(newPosition))
                {
                    reward = HandleFoodCollision(newPosition, 0);
                    _snake.MoveTo(newPosition, true);
                    SpawnFood();
                    _foodCount++;
                    Score++;
                    _foodHungry += 100;
                }
                else
                {
                    reward = MoveSnakeToNewPosition(newPosition, 0);
                }
            }

            if (isFailed) reward += Score < MaxScore ? -50f : -10f;

            return (isFailed, reward);
        }

        private (bool, float) RatePossibleDirections(Pos pos)
        {
            var isFailed = IsFailed(pos);

            var reward = 0f;

            if (!isFailed)
            {
                if (FoodLocation.Contains(pos))
                {
                    reward = HandleFoodCollision(pos, 0);
                }
                else
                {
                    var distanceToFood = GetDistanceToFood(pos);
                    reward += distanceToFood < _lastDistance ? 1f : -1f;
                    //reward += -0.25f;
                    if (_foodHungry - 1 == 0)
                    {
                        reward += Score < MaxScore ? -50f : -10f;
                    }
                }
            }

            if (isFailed) reward += Score < MaxScore ? -50f : -10f;
            return (isFailed, reward);
        }

        private bool IsOppositeDirection(Direction current, Direction next)
        {
            return (current == Direction.Left && next == Direction.Right) ||
                   (current == Direction.Right && next == Direction.Left) ||
                   (current == Direction.Up && next == Direction.Down) ||
                   (current == Direction.Down && next == Direction.Up);
        }

        private bool IsFailed(Pos newPosition)
        {
            if (newPosition.X < 0 || newPosition.X >= Field.Width || newPosition.Y < 0 || newPosition.Y >= Field.Height)
            {
                return true;
            }

            var snakeHead = newPosition;
            for (var i = 1; i < _snake.SnakeBody.Count; i++)
            {
                if (_snake.SnakeBody[i] == snakeHead)
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

        private float HandleFoodCollision(Pos newPosition, float reward)
        {
            reward += (float)Math.Sqrt(Score) * 3.5f;

            return reward;
        }

        private float MoveSnakeToNewPosition(Pos newPosition, float reward)
        {
            _snake.MoveTo(newPosition, false);
            var distanceToFood = GetDistanceToFood(_snake.Head);
            if (distanceToFood < _minDistance)
            {
                _minDistance = distanceToFood;
            }

            reward += distanceToFood < _lastDistance ? 1f : -1f;
            //reward += -0.25f;

            _lastDistance = distanceToFood;
            
            return reward;
        }

        private Pos GetNewSnakeHeadPos()
        {
            var newHead = new Pos(_snake.Head.Y, _snake.Head.X);

            switch (_nextDirection)
            {
                case Direction.Left:
                    newHead.X--;
                    break;
                case Direction.Right:
                    newHead.X++;
                    break;
                case Direction.Up:
                    newHead.Y--;
                    break;
                case Direction.Down:
                    newHead.Y++;
                    break;
            }

            return newHead;
        }

        private List<double> GatherSensors(bool isAlive, Pos head)
        {
            SensorsAdditionalInfo.Clear();
            const int sensorKinds = 3;
            const int wallSensor = 0;
            const int appleSensor = 1;
            const int tailSensor = 2;
            var dirs = Pos.Dir8.Count;
            var maxRange = Math.Max(Field.Width, Field.Height) + 1;
            var sensors = new double[sensorKinds, dirs];

            for (var i = 0; i < dirs; i++)
            {
                var dir = Pos.Dir8[i];

                bool foundWall = false, foundApple = false, foundTail = false;
                for (var distance = 1; distance <= maxRange; distance++)
                {
                    var pos = head + distance * dir;
                    var sensor = Math.Round(1f / distance, 2);
                    if (!foundWall && !IsValidPosition(pos))
                    {
                        if (distance <= maxRange / 3)
                        {
                            foundWall = true;
                            sensors[wallSensor, i] = sensor;
                            SensorsAdditionalInfo.Add((0, pos));
                            break;
                        }
                    }

                    if (!foundApple && FoodLocation.Contains(pos))
                    {
                        foundApple = true;
                        sensors[appleSensor, i] = 1;
                        SensorsAdditionalInfo.Add((1, pos));
                    }

                    if (!foundTail && _snake.SnakeBody.Contains(pos))
                    {
                        foundTail = true;
                        sensors[tailSensor, i] = sensor;
                        SensorsAdditionalInfo.Add((2, pos));
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

            gatheredSensors.Add(isAlive ? 1 : -1);
            // gatheredSensors.Add(Math.Round(TargetFood.X / (float)Field.Width, 2));
            // gatheredSensors.Add(Math.Round(TargetFood.Y / (float)Field.Height, 2));
            gatheredSensors.Add(Math.Round(_snake.Head.X / (float)Field.Width, 2));
            gatheredSensors.Add(Math.Round(_snake.Head.Y / (float)Field.Height, 2));
            gatheredSensors.Add(Math.Round((_snake.Head.Y - TargetFood.Y) / (float)Field.Height, 2));
            gatheredSensors.Add(Math.Round((_snake.Head.X - TargetFood.X) / (float)Field.Width, 2));
            // gatheredSensors.Add(Math.Round(
            //     GetDistanceToFood() / (float)Pos.Distance(new Pos(0, 0), new Pos(Field.Width, Field.Height)), 2));
            // gatheredSensors.Add(_minDistance / Pos.Distance(new Pos(0, 0), new Pos(Field.Width, Field.Height)));


            return gatheredSensors.ToList();
        }


        private bool IsValidPosition(Pos pos)
        {
            return pos.X >= 0 && pos.X < Field.Width && pos.Y >= 0 && pos.Y < Field.Height;
        }

        private void RestartGame()
        {
            OnGameRestart?.Invoke(Score);

            _snake.Create(new Pos(Random.Shared.Next(0, Field.Height - 5),
                Random.Shared.Next(0, Field.Width)));

            for (var i = 0; i < 5; i++)
            {
                _snake.AddBodyPart(new Pos() { X = 0, Y = 1 });
            }

            //_snake.Brain.Reset();
            //_foodCount = 0;
            Score = 1;
            _foodHungry = 200;
            _minDistance = int.MaxValue;
            _lastDistance = int.MaxValue;
            _nextDirection = Direction.Up;

            SpawnFood();
        }

        private void SpawnFood(int count = 1)
        {
            var taken = SnakeBody.ToList();
            if (TargetFood != null)
            {
                taken.Add(TargetFood);
            }

            var free = Field.GetFreeCells(Field, taken);

            FoodLocation = Enumerable.Range(0, count)
                .Select(x => free[Random.Shared.Next(0, free.Count)])
                .ToList();
            TargetFood = FoodLocation.FirstOrDefault();
        }

        private double GetDistanceToFood(Pos pos)
        {
            return FoodLocation.Select(food => Pos.Distance(pos, food)).DefaultIfEmpty(0).Min();
        }
    }
}