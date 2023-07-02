namespace Snake;

public class BrainStatistic
{
    public List<double> Sensors { get; set; }
    public float Reward { get; set; }
    public double Exploration { get; set; }
    public List<double> QValues { get; set; }
    public Direction CurrentAction { get; set; }
    public int FoodHungry { get; set; }
    public int MaxScore { get; set; }
    public int FoodCollected { get; set; }
}