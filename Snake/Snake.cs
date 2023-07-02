namespace Snake;

public class Snake
{
    public readonly IBrain<List<double>, Direction> Brain;
    private readonly List<Pos> _snakeBody;

    public IReadOnlyList<Pos> SnakeBody => _snakeBody.AsReadOnly();
    public Pos Head => _snakeBody[0];

    public Snake(IBrain<List<double>, Direction> brain)
    {
        _snakeBody = new List<Pos>();
        Brain = brain;
    }

    public Direction ChooseDirection(List<double> sensors)
    {
        return Brain.DecideAction(sensors);
    }

    public void Create(Pos pos)
    {
        _snakeBody.Clear();
        _snakeBody.Add(pos);
    }

    public void AddBodyPart(Pos posDir4RelativelyToLastPart)
    {
        _snakeBody.Add(_snakeBody.Last() + posDir4RelativelyToLastPart);
    }

    public void MoveTo(Pos pos, bool ateFood)
    {
        _snakeBody.Insert(0, pos);
        
        if (!ateFood)
            _snakeBody.RemoveAt(_snakeBody.Count - 1);
    }
}