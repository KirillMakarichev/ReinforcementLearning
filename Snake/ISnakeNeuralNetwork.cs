namespace Snake;

public interface ISnakeNeuralNetwork
{
    double[] Predict(double[] input);
    double[] Train(double[] input, double[] target);
}

// Класс, представляющий память для хранения опыта
public class ReplayMemory<TState, TAction>
{
    private List<Experience<TState, TAction>> memory;
    private int capacity;

    public ReplayMemory(int capacity)
    {
        this.capacity = capacity;
        memory = new List<Experience<TState, TAction>>();
    }

    public void AddExperience(Experience<TState, TAction> experience)
    {
        memory.Add(experience);
        if (memory.Count > capacity)
            memory.RemoveAt(0);
    }

    public Experience<TState, TAction> SampleExperience()
    {
        int index = Random.Shared.Next(memory.Count);
        return memory[index];
    }
    
    public List<Experience<TState, TAction>> MiniButchExperience(int count)
    {
        return memory.OrderBy(x => Random.Shared.Next()).Take(count).ToList();
    }
}

// Класс, представляющий опыт
public class Experience<TState, TAction>
{
    public TState State { get; set; }
    public TAction Action { get; set; }
    public double Reward { get; set; }
    public TState NextState { get; set; }
    public bool Done { get; set; }
}