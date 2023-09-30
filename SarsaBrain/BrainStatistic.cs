namespace SarsaBrain;

public class BrainStatistic<TAction>
{
    public List<double> Sensors { get; set; }
    public float Reward { get; set; }
    public double Exploration { get; set; }
    public List<double> QValues { get; set; }
    public TAction CurrentAction { get; set; }
}