namespace SarsaBrain;

public interface IBrain<TState, TAction>
{
    void NextEpisode ();
    TAction DecideAction(TState state);
    TAction DecideAction(List<Experience<TState, TAction>> experiences, TState currentSensors);
    List<double> Learn(Experience<TState, TAction> experience);
    void DowngradeExploration();
}