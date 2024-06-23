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
}