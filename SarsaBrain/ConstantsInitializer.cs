namespace SarsaBrain;

public class ConstantsInitializer
{
    public double LearningRate { get; set; }
    public double ExplorationDefault { get; set; }
    public double ExplorationDecay { get; set; }
    public double ExplorationMin { get; set; }
    public double DiscountFactor { get; set; }
    public int ReplayMemoryCapacity { get; set; }
    public int ReplayMemoryMinSize { get; set; }
    public int MiniBatchSize { get; set; }
}