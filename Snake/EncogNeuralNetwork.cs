using Encog.Engine.Network.Activation;
using Encog.ML.Data;
using Encog.ML.Data.Basic;
using Encog.ML.Train;
using Encog.Neural.Data.Basic;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Layers;
using Encog.Neural.Networks.Training.Propagation.Back;
using Encog.Neural.Networks.Training.Propagation.Resilient;

namespace Snake;

public class EncogNeuralNetwork : ISnakeNeuralNetwork
{
    private readonly NeuralNetworkSettings _neuralNetworkSettings;
    private readonly BasicNetwork _network;

    public EncogNeuralNetwork(NeuralNetworkSettings neuralNetworkSettings)
    {
        _neuralNetworkSettings = neuralNetworkSettings;

        var network = new BasicNetwork();
        network.AddLayer(new BasicLayer(new ActivationTANH(), true, neuralNetworkSettings.NumInputs));
        for (int i = 0; i < neuralNetworkSettings.NumHiddenLayers; i++)
        {
            network.AddLayer(new BasicLayer(new ActivationTANH(), true, neuralNetworkSettings.NumNeuronsInHiddenLayer));
        }

        network.AddLayer(new BasicLayer(new ActivationSoftMax(), false, neuralNetworkSettings.NumOutputs));
        network.Structure.FinalizeStructure();
        network.Reset();

        _network = network;
    }

    public double[] Predict(double[] input)
    {
        var prediction = new double[_network.OutputCount];
        _network.Compute(input, prediction);
        return prediction;
    }

    public double[] Train(double[] input, double[] target)
    {
        var pairs = new List<IMLDataPair>
        {
            new BasicNeuralDataPair(new BasicNeuralData(input),
                new BasicNeuralData(target))
        };

        // create training data
        IMLDataSet trainingSet = new BasicMLDataSet(pairs);

        // train the neural network
        IMLTrain train = new Backpropagation(_network, 
            trainingSet, 
            _neuralNetworkSettings.LearningRate
            , _neuralNetworkSettings.Momentum
            );
        
        var output = Predict(input);
        
        var errors = new double[output.Length];

        for (var j = 0; j < output.Length; j++)
        {
            errors[j] = target[j] - output[j];
        }
        
        train.Iteration();

        return errors;
    }
}