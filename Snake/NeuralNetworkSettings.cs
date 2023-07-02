namespace Snake;

public class NeuralNetworkSettings
{
    public int NumInputs { get; set; }
    public int NumOutputs { get; set; }
    public int NumHiddenLayers { get; set; }
    public int NumNeuronsInHiddenLayer { get; set; }
    public double LearningRate { get; set; }
    public double Momentum { get; set; }
}