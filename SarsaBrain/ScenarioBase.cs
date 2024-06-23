namespace SarsaBrain;

public abstract class ScenarioBase<TAgent, TAction, TState> : IScenario
    where TAgent : AgentBase<TAction, TState>
    where TState : struct
{
    protected readonly TAgent Agent;
    protected bool Done { get; set; }
    private float _reward;
    protected List<double> Errors = new List<double>();
    protected ScenarioBase(TAgent agent)
    {
        Agent = agent;
    }

    public virtual void Tick(ModeControl control = ModeControl.LearningPc, bool withFuturePossibleStates = false)
    {
        SetUpEpisode();
        var currentState = GetSensorsInState(Agent.State);
        
        var action = withFuturePossibleStates ? Agent.DecideAction( GetAllPossibleFutureStates(), currentState) 
            : Agent.DecideAction(currentState);
        SetReward(ReleaseDecision(action, Agent.State));
        var afterState = GetSensorsInState(Agent.State);

        if (control == ModeControl.LearningPc)
        {
            Errors.Clear();
            
            BeforeLearn(Done);
            
            var errors = Agent.Learn(new Experience<double[], TAction>()
            {
                State = currentState,
                Action = action,
                Reward = _reward,
                NextState = afterState,
                Done = Done
            });
            
            AfterLearn(Done);
            
            Errors.AddRange(errors);
        }
        
        Agent.DowngradeExploration();
        
        if (!Done) return;
        
        NextEpisode();
    }

    protected abstract double[] GetSensorsInState(TState state);

    protected virtual List<Experience<double[], TAction>> GetAllPossibleFutureStates()
    {
        return new List<Experience<double[], TAction>>();
    }

    protected abstract float ReleaseDecision(TAction action, TState currentState);
    
    protected virtual float SetReward(float reward)
    {
        _reward = reward;
        return _reward;
    }

    protected virtual void NextEpisode()
    {
        _reward = 0;
        Done = false;
        
        Agent.NextEpisode();
    }

    protected virtual void SetUpEpisode()
    {
    }

    protected virtual void BeforeLearn(bool done)
    {
    }

    protected virtual void AfterLearn(bool done)
    {
    }
    
    public Task SaveAsync(string path) => Agent.SaveAsync(path);
    
}