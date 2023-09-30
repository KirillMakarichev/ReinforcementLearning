namespace SarsaBrain;

public interface ISarsaNeuralNetwork
{
    double[] Predict(double[] input);
    double[] Train(double[] input, double[] target);
    Task SaveAsync(string path);
}

// Класс, представляющий память для хранения опыта
public class ReplayMemory<TState, TAction>
{
    private List<Experience<TState, TAction>> _memory;
    private int _capacity;

    public ReplayMemory(int capacity)
    {
        this._capacity = capacity;
        _memory = new List<Experience<TState, TAction>>();
    }

    public void AddExperience(Experience<TState, TAction> experience)
    {
        _memory.Add(experience);
        if (_memory.Count > _capacity)
            _memory.RemoveAt(0);
    }

    public Experience<TState, TAction> SampleExperience()
    {
        var index = Random.Shared.Next(_memory.Count);
        return _memory[index];
    }
    
    public List<Experience<TState, TAction>> MiniButchExperience(int count)
    {
        return _memory.OrderBy(x => Random.Shared.Next()).Take(count).ToList();
    }
}

// Класс, представляющий опыт
public class Experience<TState, TAction>
{
    public TState State { get; set; }
    public TAction Action { get; set; }
    public float Reward { get; set; }
    public TState NextState { get; set; }
    public bool Done { get; set; }
}