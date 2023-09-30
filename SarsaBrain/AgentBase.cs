namespace SarsaBrain;

public abstract class AgentBase<TAction, TState> : IBrain<double[], TAction>
{
    public TState State { get; protected set; }

    protected readonly ConstantsInitializer ConstantsInitializer;
    protected readonly int NumOutputs;
    protected double Exploration;
    protected double[] CurrentState;
    protected double[] CurrentQValues;
    protected int CurrentAction;
    protected float CurrentReward;

    protected readonly ISarsaNeuralNetwork NeuralNetwork;
    protected readonly ReplayMemory<double[], TAction> ReplayMemory;

    private AgentBase(ConstantsInitializer constantsInitializer, ISarsaNeuralNetwork sarsaNeuralNetwork)
    {
        ConstantsInitializer = constantsInitializer;
        Exploration = ConstantsInitializer.ExplorationDefault;

        NeuralNetwork = sarsaNeuralNetwork;

        ReplayMemory = new ReplayMemory<double[], TAction>(ConstantsInitializer.ReplayMemoryCapacity);
    }

    protected AgentBase(ConstantsInitializer constantsInitializer,
        NeuralNetworkSettings neuralNetworkSettings,
        ISarsaNeuralNetwork? sarsaNeuralNetwork = null) :
        this(constantsInitializer, sarsaNeuralNetwork ?? new ArtificialNeuralNetwork(neuralNetworkSettings))
    {
        NumOutputs = neuralNetworkSettings.NumOutputs;
    }
    
    public virtual void NextEpisode()
    {
        CurrentQValues = default;
        CurrentReward = 0;
        CurrentState = default;
    }

    public virtual TAction DecideAction(double[] state)
    {
        CurrentQValues = default;
        if (Random.Shared.NextDouble() < Exploration)
        {
            // Случайное действие для исследования
            return ConvertDoubleToAction(Random.Shared.Next(0, NumOutputs));
        }

        var prediction = NeuralNetwork.Predict(state);
        CurrentAction = Array.IndexOf(prediction, prediction.Max());
        CurrentQValues = prediction;

        return ConvertDoubleToAction(CurrentAction);
    }

    public virtual TAction DecideAction(List<Experience<double[], TAction>> experiences, double[] currentSensors)
    {
        if (Random.Shared.NextDouble() < Exploration)
        {
            // Случайное действие для исследования
            return ConvertDoubleToAction(Random.Shared.Next(0, NumOutputs));
        }

        var maxValue = double.MinValue;
        var indexOfMax = 0;
        var qValues = new double[experiences.Count];
        var predictions = new double[experiences.Count][];

        var currentPrediction = NeuralNetwork.Predict(currentSensors);

        for (var i = 0; i < experiences.Count; i++)
        {
            var state = experiences[i];

            double value = 0;

            var nextPrediction = NeuralNetwork.Predict(state.NextState);
            predictions[i] = nextPrediction;
            var nextAction = Array.IndexOf(nextPrediction, nextPrediction.Max());
            value = currentPrediction[CurrentAction] +
                    ConstantsInitializer.LearningRate *
                    (state.Reward + ConstantsInitializer.DiscountFactor * nextPrediction[nextAction] -
                     currentPrediction[CurrentAction]);

            qValues[i] = value;
            if (!(value > maxValue)) continue;

            maxValue = value;
            indexOfMax = i;
        }

        CurrentAction = indexOfMax;
        CurrentQValues = predictions[indexOfMax];
        return ConvertDoubleToAction(indexOfMax);
    }

    public virtual List<double> Learn(Experience<double[], TAction> experience)
    {
        CurrentReward = experience.Reward;

        AddExperiencePolicy(new Experience<double[], TAction>()
        {
            Reward = CurrentReward,
            Action = experience.Action,
            State = experience.State,
            NextState = experience.NextState,
            Done = experience.Done
        });

        CurrentState = experience.NextState;

        if (!experience.Done)
        {
            return new List<double>();
        }

        var miniBatch = ReplayMemory.MiniButchExperience(ConstantsInitializer.MiniBatchSize);

        var errors = new List<double[]>();
        foreach (var e in miniBatch)
        {
            // var nextQValues = NeuralNetwork.Predict(e.NextState.ToArray());
            //
            // var updatedQValues = UpdateQValues(qValues, nextQValues, e);
            //
            // var errorsExp = NeuralNetwork.Train(e.State.ToArray(), updatedQValues);
            // errors.Add(errorsExp);
            var qValues = NeuralNetwork.Predict(e.State);
            var nextState = e.NextState;
            var nextStateQ = NeuralNetwork.Predict(nextState);
            
            var action = e.Action;
            var reward = e.Reward;

            var actionIndex = ConvertActionToInt(action);
            qValues[actionIndex] = reward + ConstantsInitializer.DiscountFactor * nextStateQ.Max();
            var errorsExp = NeuralNetwork.Train(e.State, qValues);
            errors.Add(errorsExp);
        }

        var listAverageErrors = new List<double>();
        for (var i = 0; i < NumOutputs; i++)
        {
            var sum = errors.Select(x => x[i]).Average();
            listAverageErrors.Add(sum);
        }

        return listAverageErrors;
    }

    protected virtual void AddExperiencePolicy(Experience<double[], TAction> experience)
    {
        ReplayMemory.AddExperience(experience);
    }
    
    protected virtual double[] UpdateQValues(double[] qValues, double[] nextQValues, Experience<List<double>, TAction> e)
    {
        var action = ConvertActionToInt(e.Action);
        var target = e.Reward + ConstantsInitializer.DiscountFactor * nextQValues.Max();

        qValues[action] = target;
        return qValues;
    }

    public virtual void DowngradeExploration()
    {
        var newExploration = Exploration * ConstantsInitializer.ExplorationDecay;
        if (newExploration >= ConstantsInitializer.ExplorationMin)
            Exploration = newExploration;
    }

    protected virtual TAction ConvertDoubleToAction(params double[] values)
    {
        return default;
    }

    protected virtual int ConvertActionToInt(TAction action)
    {
        return default;
    }

    public Task SaveAsync(string path) => NeuralNetwork.SaveAsync(path);
}