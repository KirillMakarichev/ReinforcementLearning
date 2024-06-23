using SarsaBrain;

namespace Snake;

public class BrainStatisticSnake : BrainStatistic<Direction>
{
    public int FoodHungry { get; set; }
    public int MaxScore { get; set; }
    public int FoodCollected { get; set; }
}