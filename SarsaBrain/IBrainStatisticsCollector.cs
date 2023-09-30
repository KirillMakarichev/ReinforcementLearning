namespace SarsaBrain;

public interface IBrainStatisticsCollector<TAction>
{
    BrainStatistic<TAction> GetStatistics();
}