using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.Genes;
using Newtonsoft.Json;

namespace SarsaBrain;

public static class NeuralNetworkExtensions
{
    public static async Task SaveAsync(this INeuralNetwork network, string file)
    {
        var genes = network.GetGenes();

        await File.WriteAllTextAsync(file, JsonConvert.SerializeObject(genes));
    }

    public static async Task<INeuralNetwork> LoadFromJsonAsync(string file)
    {
        var text = await File.ReadAllTextAsync(file);
        var genes = JsonConvert.DeserializeObject<NeuralNetworkGene>(text);

        return NeuralNetworkFactory.GetInstance().Create(genes);
    }

    private static double LearnNeuron(this INeuron neuron, double error, double learningRate)
    {
        var delta = error * TanhDx(neuron.Axon.Value);

        foreach (var dendrite in neuron.Soma.Dendrites.ToList())
        {
            var weight = dendrite.Weight;
            var input = dendrite.Axon.Value;

            var newWeight = weight - input * delta * learningRate;
            dendrite.Weight = newWeight;
        }

        return delta;
    }
    
    public static void BackPropagation(this INeuralNetwork network, double[] expectedResults, double learningRate)
    {
        for (var index = 0; index < network.OutputLayer.NeuronsInLayer.Count; index++)
        {
            var outputNeuron = network.OutputLayer.NeuronsInLayer[index];
            var output = network.GetOutputs()[index];
            var difference = expectedResults[index] - output;
    
            var layers = new List<ILayer>();
            layers.AddRange(network.HiddenLayers);
            layers.Add(network.OutputLayer);
    
            var deltas = new List<List<double>>();
    
            var delta = outputNeuron.LearnNeuron(difference, learningRate);
            deltas.Add(new List<double>() { delta });
    
            for (var j = layers.Count - 2; j >= 0; j--)
            {
                var layer = layers[j];
                var previousLayer = layers[j + 1];
    
                //нахожу все нейроны в текущем слое
                deltas.Insert(0, new List<double>());
                for (var i = 0; i < layer.NeuronsInLayer.Count; i++)
                {
                    var neuron = layer.NeuronsInLayer[i];
    
                    //для каждого нейрона в текущем слое нахожу все нейроны в правом(предыдущем) слое
                    for (var k = 0; k < previousLayer.NeuronsInLayer.Count; k++)
                    {
                        var previousNeuron = previousLayer.NeuronsInLayer[k];
                        var error = previousNeuron.Soma.Dendrites[i].Weight * deltas[1][k];
                        var deltaCur = neuron.LearnNeuron(error, learningRate);
                        deltas[0].Add(deltaCur);
                    }
                }
            }
        }
    }
    
    static double TanhDx(double value) => 1 - Math.Pow(Math.Tanh(value), 2);
}