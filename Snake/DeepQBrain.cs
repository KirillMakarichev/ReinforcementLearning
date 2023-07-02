namespace Snake
{
    public class DeepQBrain : IBrain<List<double>, Direction>, IBrainStatisticsCollector
    {
        private const double ExplorationDefault = 1;
        private const double ExplorationDecay = 0.995;
        private const double Momentum = 0.8;
        private const double ExplorationMin = 0.01;
        private const double LearningRate = 0.001;
        private const double DiscountFactor = 0.999;
        private const int ReplayMemoryCapacity = 10000;
        private const int ReplayMemoryMinSize = 200;
        private const int MiniBatchSize = 256;

        private const int NumOutputs = 4;

        private double _exploration = ExplorationDefault;
        private List<double> _currentState;
        private List<double> _currentQValues;
        private int _currentAction;
        private float _currentReward;
        private readonly ISnakeNeuralNetwork _neuralNetwork;
        private readonly ReplayMemory<List<double>, Direction> _replayMemory;

        public DeepQBrain()
        {
            var numInputs = 33;
            var numOutputs = NumOutputs;
            var numHiddenLayers = 4;
            var numNeuronsInHiddenLayer = 25;

            var settings = new NeuralNetworkSettings()
            {
                LearningRate = LearningRate,
                Momentum = Momentum,
                NumInputs = numInputs,
                NumHiddenLayers = numHiddenLayers,
                NumOutputs = numOutputs,
                NumNeuronsInHiddenLayer = numNeuronsInHiddenLayer
            };

            _neuralNetwork = new ArtificialNeuralNetwork(settings);

            _replayMemory = new ReplayMemory<List<double>, Direction>(ReplayMemoryCapacity);
        }

        public void NextEpisode()
        {
            _currentQValues = null;
            _currentReward = 0;
            _currentState = null;
        }

        public Direction DecideAction(List<double> state)
        {
            _currentQValues = null;
            if (Random.Shared.NextDouble() < _exploration)
            {
                // Случайное действие для исследования
                return (Direction)Random.Shared.Next(0, NumOutputs);
            }

            var prediction = _neuralNetwork.Predict(state.ToArray());
            _currentAction = Array.IndexOf(prediction, prediction.Max());
            _currentQValues = prediction.ToList();

            return (Direction)_currentAction;
        }

        public Direction DecideAction(List<(double reward, bool isFailed, List<double> nextSensors)> states, List<double> currentSensors)
        {
            if (Random.Shared.NextDouble() < _exploration)
            {
                // Случайное действие для исследования
                return (Direction)Random.Shared.Next(0, NumOutputs);
            }

            var maxValue = double.MinValue;
            var indexOfMax = 0;
            double[] qValues = new double[states.Count];
            double[][] predictions = new double[states.Count][];

            var currentPrediction = _neuralNetwork.Predict(currentSensors.ToArray());

            for (var i = 0; i < states.Count; i++)
            {
                var state = states[i];

                double value = 0;

                    var nextPrediction = _neuralNetwork.Predict(state.nextSensors.ToArray());
                    predictions[i] = nextPrediction;
                    var nextAction = nextPrediction.ToList().IndexOf(nextPrediction.Max());
                    value = currentPrediction[_currentAction] +
                            LearningRate *
                            (state.reward + DiscountFactor * nextPrediction[nextAction] - currentPrediction[_currentAction]);
                    // if (state.isFailed)
                    // {
                    //     value = state.reward;
                    // }
                    // else
                    // {
                    // }

                qValues[i] = value;
                if (value > maxValue)
                {
                    maxValue = value;
                    indexOfMax = i;
                }
            }

            _currentAction = indexOfMax;
            _currentQValues = predictions[indexOfMax]?.ToList();
            return (Direction)indexOfMax;
        }

        public List<double> Learn(List<double> state, Direction action, float reward, List<double> nextState,
            bool done)
        {
            _currentReward = reward;

            _replayMemory.AddExperience(new Experience<List<double>, Direction>()
            {
                Reward = _currentReward,
                Action = action,
                State = state,
                NextState = nextState,
                Done = done
            });
            _currentState = nextState;

            if (!done)
            {
                return new List<double>();
            }

            var miniBatch = _replayMemory.MiniButchExperience(MiniBatchSize);

            var errors = new List<double[]>();
            foreach (var e in miniBatch)
            {
                var qValues = _neuralNetwork.Predict(e.State.ToArray());
                var nextQValues = _neuralNetwork.Predict(e.NextState.ToArray());
                var target = e.Reward + DiscountFactor * nextQValues[(int)e.Action];

                qValues[(int)e.Action] = target;
                var errorsExp = _neuralNetwork.Train(e.State.ToArray(), qValues);
                errors.Add(errorsExp);
            }

            var listAverageErrors = new List<double>();
            for (int i = 0; i < NumOutputs; i++)
            {
                var sum = errors.Select(x => x[i]).Average();
                listAverageErrors.Add(sum);
            }

            return listAverageErrors;
        }

        public void DowngradeExploration()
        {
            var newExploration = _exploration * ExplorationDecay;
            if (newExploration >= ExplorationMin)
                _exploration = newExploration;
        }

        public BrainStatistic GetStatistics()
        {
            return new BrainStatistic()
            {
                Exploration = _exploration,
                Reward = _currentReward,
                Sensors = _currentState,
                QValues = _currentQValues,
                CurrentAction = (Direction)_currentAction
            };
        }
    }
}