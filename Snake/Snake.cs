using SarsaBrain;

namespace Snake;

public class Snake : AgentBase<Direction, SnakeState>, IBrainStatisticsCollector<Direction>
{
    private readonly List<Pos> _snakeBody;

    public IReadOnlyList<Pos> SnakeBody => _snakeBody.AsReadOnly();
    public Pos Head => _snakeBody[0];

    public Snake(ConstantsInitializer constantsInitializer, NeuralNetworkSettings neuralNetworkSettings) : this
    (constantsInitializer, neuralNetworkSettings, null)
    {
    }

    public Snake(ConstantsInitializer constantsInitializer, NeuralNetworkSettings neuralNetworkSettings, ISarsaNeuralNetwork? neuralNetwork = null) : base(
        constantsInitializer, neuralNetworkSettings, neuralNetwork)
    {
        _snakeBody = new List<Pos>();
    }

    public void Create(Pos pos)
    {
        _snakeBody.Clear();
        _snakeBody.Add(pos);
        State = new SnakeState()
        {
            Head = pos
        };
    }

    public void AddBodyPart(Pos posDir4RelativelyToLastPart)
    {
        _snakeBody.Add(_snakeBody.Last() + posDir4RelativelyToLastPart);
    }

    public void MoveTo(Pos pos, bool ateFood)
    {
        _snakeBody.Insert(0, pos);

        State = new SnakeState()
        {
            Head = pos
        };
        
        if (!ateFood)
            _snakeBody.RemoveAt(_snakeBody.Count - 1);
    }
    
    protected override Direction ConvertDoubleToAction(params double[] values)
    {
        return (Direction)values[0];
    }

    protected override int ConvertActionToInt(Direction action)
    {
        return (int)action;
    }
    
    public BrainStatistic<Direction> GetStatistics()
    {
        return new BrainStatistic<Direction>()
        {
            Exploration = Exploration,
            Reward = CurrentReward,
            Sensors = CurrentState?.ToList(),
            QValues = CurrentQValues?.ToList(),
            CurrentAction = (Direction)CurrentAction
        };
    }
}