namespace Snake;

public interface IBrain<TState, TAction>
{
    void NextEpisode ();
    TAction DecideAction(List<double> state);
    TAction DecideAction(List<(double reward, bool isFailed, List<double> nextSensors)> states,
        List<double> currentSensors);
    List<double> Learn(List<double> state, Direction action, float reward, List<double> nextState, bool done);
    void DowngradeExploration();
}