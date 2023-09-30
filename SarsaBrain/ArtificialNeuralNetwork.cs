using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.ActivationFunctions;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.WeightInitializer;
using NeuralNetwork.Backpropagation;
using NeuralNetwork.Backpropagation.ActivationFunctions;

namespace SarsaBrain;

public class ArtificialNeuralNetwork : ISarsaNeuralNetwork
{
    private readonly INeuralNetwork _neuralNetwork;
    private readonly Backpropagater _backpropagater;

    public ArtificialNeuralNetwork(NeuralNetworkSettings neuralNetworkSettings)
    {
        var somaFactory = SomaFactory.GetInstance(new SimpleSummation());

        var factory = new BackpropagationNetworkFactoryBuilder().BuildBackpropagationNetworkFactory(
            new RandomWeightInitializer(Random.Shared), somaFactory,
            new TanhActivationFunctionWithDerivative(), new TanhActivationFunction(),
            new NeuronFactory());
        _neuralNetwork = factory.Create(neuralNetworkSettings.NumInputs,
            neuralNetworkSettings.NumOutputs,
            neuralNetworkSettings.NumHiddenLayers,
            neuralNetworkSettings.NumNeuronsInHiddenLayer);

        _backpropagater =
            new Backpropagater(_neuralNetwork, neuralNetworkSettings.LearningRate,
                momentum: neuralNetworkSettings.Momentum, 1, false);
    }

    private ArtificialNeuralNetwork(NeuralNetworkSettings neuralNetworkSettings, INeuralNetwork neuralNetwork)
    {
        _neuralNetwork = neuralNetwork;
        _backpropagater =
            new Backpropagater(_neuralNetwork, neuralNetworkSettings.LearningRate,
                momentum: neuralNetworkSettings.Momentum, 1, false);
    }

    public double[] Predict(double[] input)
    {
        _neuralNetwork.SetInputs(input);
        _neuralNetwork.Process();

        return _neuralNetwork.GetOutputs();
    }

    public double[] Train(double[] input, double[] target)
    {
        var output = Predict(input);
        
        var errors = new double[output.Length];

        for (var j = 0; j < output.Length; j++)
        {
            errors[j] = target[j] - output[j];
        }
        
        _backpropagater.Backpropagate(errors);

        return errors;
    }

    public async Task SaveAsync(string path)
    {
        await _neuralNetwork.SaveAsync(path);
    }

    public static async Task<ArtificialNeuralNetwork> LoadFromJson(NeuralNetworkSettings networkSettings, string path)
    {
        var network = await NeuralNetworkExtensions.LoadFromJsonAsync(path);

        return new ArtificialNeuralNetwork(networkSettings, network);
    }
}