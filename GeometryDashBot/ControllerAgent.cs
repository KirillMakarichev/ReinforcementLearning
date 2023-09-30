using SarsaBrain;

namespace GeometryDashBot;

public class ControllerAgent : AgentBase<ControllerActions, ControllerState>, ISaver
{
    public ControllerAgent(ConstantsInitializer constantsInitializer, NeuralNetworkSettings neuralNetworkSettings,
        ISarsaNeuralNetwork? sarsaNeuralNetwork = null) : base(constantsInitializer, neuralNetworkSettings,
        sarsaNeuralNetwork)
    {
    }

    protected override ControllerActions ConvertDoubleToAction(params double[] values)
    {
        return (ControllerActions)values[0];
    }

    protected override int ConvertActionToInt(ControllerActions action)
    {
        return (int)action;
    }
}