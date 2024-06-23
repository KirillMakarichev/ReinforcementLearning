using CNTK;

namespace SarsaBrain;

struct Batch
{
    public Value Input { get; set; }
    public Value Output { get; set; }
}

public class GpuNeuralNetwork : ISarsaNeuralNetwork
{
    private readonly NeuralNetworkSettings _neuralNetworkSettings;
    private readonly Function _outputLayer;
    private readonly Variable _inputVar;
    private readonly Variable _outputVar;
    private readonly Trainer _trainer;
    private readonly DeviceDescriptor _device;
    private readonly Function _predictFunc;

    public GpuNeuralNetwork(NeuralNetworkSettings neuralNetworkSettings)
    {
        _device = DeviceDescriptor.GPUDevice(0);
        _neuralNetworkSettings = neuralNetworkSettings;
        _inputVar = Variable.InputVariable(new[] { neuralNetworkSettings.NumInputs }, DataType.Double, "Input");
        _outputVar = Variable.InputVariable(new[] { neuralNetworkSettings.NumOutputs }, DataType.Double, "Output");

        Function hiddenLayer = _inputVar;
        for (int i = 0; i < neuralNetworkSettings.NumHiddenLayers; i++)
        {
            hiddenLayer = FullyConnected(hiddenLayer, neuralNetworkSettings.NumNeuronsInHiddenLayer, CNTKLib.Tanh,
                $"HiddenLayer{i}");
        }

        // Создание выходного слоя
        _outputLayer = FullyConnected(hiddenLayer, neuralNetworkSettings.NumOutputs, null, "OutputLayer");

        var loss = CNTKLib.SquaredError(_outputLayer, _outputVar);
        var error = CNTKLib.SquaredError(_outputLayer, _outputVar);

        var learningRate = _neuralNetworkSettings.LearningRate;
        var learner = Learner.SGDLearner(_outputLayer.Parameters(), new TrainingParameterScheduleDouble(learningRate));

        _trainer = Trainer.CreateTrainer(_outputLayer, loss, error, new List<Learner> { learner });
        
        var outputVariable = _outputLayer.Output;
        var variableVector = new VariableVector();
        variableVector.Add(outputVariable);
        _predictFunc = CNTKLib.Combine(variableVector);
    }

    private Function FullyConnected(Variable input, int outputDim, Func<Variable, string, Function> activation,
        string outputName)
    {
        int inputDim = input.Shape[0];

        var weightParam = new Parameter(new[] { outputDim, inputDim }, DataType.Double,
            CNTKLib.GlorotUniformInitializer(), _device, "Weights");
        var biasParam = new Parameter(new[] { outputDim }, DataType.Double, 0, _device, "Bias");

        var timesFunction = CNTKLib.Times(weightParam, input);
        var plusFunction = CNTKLib.Plus(timesFunction, biasParam);

        if (activation != null)
        {
            return activation(plusFunction, outputName);
        }
        else
        {
            return plusFunction;
        }
    }

    public double[] Predict(double[] input)
    {
        var batch = new Batch()
        {
            Input = Value.CreateBatch(new[] { _neuralNetworkSettings.NumInputs }, input, _device),
        };

        return Predict(batch);
    }

    public double[] Train(double[] input, double[] target)
    {
        var batchForInout = new Batch()
        {
            Input = Value.CreateBatch(new[] { _neuralNetworkSettings.NumInputs }, input, _device),
        };

        var output = Predict(batchForInout);

        var errors = new double[output.Length];

        for (var j = 0; j < output.Length; j++)
        {
            errors[j] = target[j] - output[j];
        }

        var batch = batchForInout with
        {
            Output = Value.CreateBatch(new[] { _neuralNetworkSettings.NumOutputs }, errors, _device)
        };

        var arguments = new Dictionary<Variable, Value>
        {
            { _inputVar, batch.Input },
            { _outputVar, batch.Output }
        };

        _trainer.TrainMinibatch(arguments, false, _device);

        return errors;
    }
    
    private double[] Predict(Batch batch)
    {
        var inputDataMap = new Dictionary<Variable, Value>
        {
            { _inputVar, batch.Input }
        };

        var outputDataMap = new Dictionary<Variable, Value>
        {
            { _outputLayer.Output, null }
        };

        _predictFunc.Evaluate(inputDataMap, outputDataMap, _device);

        var outputData = outputDataMap[_outputLayer.Output];
        var predictedValues = outputData.GetDenseData<double>(_outputLayer.Output)[0];

        return predictedValues.ToArray();
    }

    public async Task SaveAsync(string path)
    {
        await Task.Run(() => _trainer.SaveCheckpoint(path));
    }
}