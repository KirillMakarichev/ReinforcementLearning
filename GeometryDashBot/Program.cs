// using System.Drawing.Imaging;
// using System.Runtime.InteropServices;
// using System.Text;
// using CNTK;
// using GeometryDashBot;
// using SarsaBrain;
//
// ConstantsInitializer _constantsInitializer = new()
// {
//     ExplorationDefault = 1,
//     ExplorationDecay = 0.995,
//     ExplorationMin = 0.05,
//     LearningRate = 0.001, 
//     DiscountFactor = 0.999,
//     ReplayMemoryCapacity = 5000,
//     ReplayMemoryMinSize = 300,
//     MiniBatchSize = 256,
// };
// var expectedWidth = 100;
// var monitorSize = ScreenshotMaker.GetMonitorSize();
// var monitorSizeHeight = (int)(expectedWidth / ((double)monitorSize.Width / monitorSize.Height));
//
// NeuralNetworkSettings _neuralNetworkSettings = new()
// {
//     NumInputs = expectedWidth * monitorSizeHeight,
//     NumOutputs = 4,
//     NumHiddenLayers = 15,
//     NumNeuronsInHiddenLayer = 30,
//     LearningRate = _constantsInitializer.LearningRate
// };
//
// var game = new GameScenario(_constantsInitializer, _neuralNetworkSettings, new Size(expectedWidth, monitorSizeHeight),
//     monitorSize);
//
// while (true)
// {
//     game.Tick();
// }
//
using CNTK;

namespace SimpleNeuralNetwork
{
    class Program
    {
        static void Main(string[] args)
        {
            // Определение параметров сети
            int inputDim = 2; // Размер входных данных
            int hiddenDim = 50; // Количество нейронов в скрытом слое
            int outputDim = 1; // Количество выходных нейронов
            int numSamples = 1000; // Количество обучающих примеров
            int numEpochs = 120; // Количество эпох обучения

            // Создание символьных переменных для входных и выходных данных
            var inputVar = Variable.InputVariable(new int[] { inputDim }, DataType.Float, "Input");
            var outputVar = Variable.InputVariable(new int[] { outputDim }, DataType.Float, "Output");

            // Создание скрытого слоя
            var hiddenLayer = FullyConnected(inputVar, hiddenDim, CNTKLib.Sigmoid, "HiddenLayer");

            // Создание выходного слоя
            var outputLayer = FullyConnected(hiddenLayer, outputDim, null, "OutputLayer");

            // Определение функции потерь и метрики (в данном случае используется среднеквадратичная ошибка)
            var loss = CNTKLib.SquaredError(outputLayer, outputVar);
            var error = CNTKLib.SquaredError(outputLayer, outputVar);

            // Определение оптимизатора (например, стохастического градиентного спуска)
            var learningRate = 0.001;
            var learner = Learner.SGDLearner(outputLayer.Parameters(),
                new TrainingParameterScheduleDouble(learningRate));

            // Создание обучающей выборки (случайные значения)
            var rand = new Random();
            List<(float[], float[])> trainingData = GenerateRandomData2(numSamples, inputDim, outputDim, rand);

            // Создание объекта для обучения
            var trainer = Trainer.CreateTrainer(outputLayer, loss, error, new List<Learner> { learner });

            var inputs = trainingData.Select(x => x.Item1).ToList();
            var outpus = trainingData.Select(x => x.Item2).ToList();
            
            // Обучение сети
            for (int epoch = 0; epoch < numEpochs; epoch++)
            {
                double totalLoss = 0.0;

                // var inputs = new List<Value>();
                // var outputs = new List<Value>();
                //
                // foreach (var batch in trainingData)
                // {
                //     inputs.Add(batch.Input);
                //     outputs.Add(batch.Output);
                // }

                // var inputBatch = Value.CreateBatch(inputDim, inputs, DeviceDescriptor.CPUDevice);
                // var outputBatch = Value.CreateBatch<float>(new int[] { outputDim }, outputs, DeviceDescriptor.CPUDevice);
                
                Value inputValues = Value.CreateSequence(inputVar.Shape, inputs, DeviceDescriptor.GPUDevice(0));
                Value outputLabels = Value.CreateSequence(outputVar.Shape, outpus, DeviceDescriptor.GPUDevice(0));

                var arguments = new Dictionary<Variable, Value>
                {
                    { inputVar, inputValues },
                    { outputVar, outputLabels }
                };

                trainer.TrainMinibatch(arguments, false, DeviceDescriptor.GPUDevice(0));
                totalLoss += trainer.PreviousMinibatchLossAverage();

                Console.WriteLine($"Epoch {epoch + 1}, Loss: {totalLoss / numSamples}");
            }

            Console.WriteLine("Обучение завершено.");

            // Теперь вы можете использовать обученную модель для предсказаний.

            // Создание тестовых данных
            var testData = GenerateRandomData(10, inputDim, outputDim, new Random());

            // Загрузка обученных параметров сети (если они были сохранены)
            // Это предполагает, что у вас есть сохраненные параметры после обучения сети
            // Например, trainer.SaveCheckpoint(modelPath);

            // Загрузка обученных параметров (если они были сохранены)
            // Это предполагает, что у вас есть сохраненные параметры после обучения сети
            // Например, trainer.RestoreFromCheckpoint(modelPath);

            // Создание объекта для прямого прохода (forward pass)
            var outputVariable = outputLayer.Output;
            var variableVector = new VariableVector();
            variableVector.Add(outputVariable);
            var predictFunc = CNTKLib.Combine(variableVector);

            // Предсказание результатов на тестовых данных
            foreach (var batch in testData)
            {
                var inputDataMap = new Dictionary<Variable, Value>
                {
                    { inputVar, batch.Input }
                };

                var outputDataMap = new Dictionary<Variable, Value>
                {
                    { outputLayer.Output, null }
                };

                predictFunc.Evaluate(inputDataMap, outputDataMap, DeviceDescriptor.GPUDevice(0));

                var outputData = outputDataMap[outputLayer.Output];
                var predictedValues = outputData.GetDenseData<float>(outputLayer.Output);

                // Вывод предсказанных значений
                var valuesInput = batch.Input.GetDenseData<float>(inputVar)[0];
                Console.WriteLine("Input Data: " + string.Join(", ", valuesInput));
                Console.WriteLine("Fact Output: " + $"{valuesInput.Sum() - 0.5f}");
                Console.WriteLine("Predicted Output: " + string.Join(", ", predictedValues[0]));
                Console.WriteLine("-----------------------");
            }
        }

        private static Function FullyConnected(Variable input, int outputDim,
            Func<Variable, string, Function> activation, string outputName)
        {
            int inputDim = input.Shape[0];

            var weightParam = new Parameter(new int[] { outputDim, inputDim }, DataType.Float,
                CNTKLib.GlorotUniformInitializer(), DeviceDescriptor.GPUDevice(0), "Weights");
            var biasParam = new Parameter(new int[] { outputDim }, DataType.Float, 0, DeviceDescriptor.GPUDevice(0),
                "Bias");

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


        // Генерация случайных данных для обучения
        private static List<Batch> GenerateRandomData(int numSamples, int inputDim, int outputDim, Random rand)
        {
            var data = new List<Batch>();
            for (int i = 0; i < numSamples; i++)
            {
                var input = Enumerable.Range(0, inputDim).Select(_ => (float)rand.NextDouble()).ToArray();
                var output = new float[outputDim];
                for (int j = 0; j < outputDim; j++)
                {
                    output[j] = input.Sum() - 0.5f;
                }

                data.Add(new Batch()
                {
                    Input = Value.CreateBatch<float>(new int[] { inputDim }, input, DeviceDescriptor.GPUDevice(0)),
                    Output = Value.CreateBatch<float>(new int[] { outputDim }, output, DeviceDescriptor.GPUDevice(0))
                });
            }

            return data;
        }


        private static List<(float[], float[])> GenerateRandomData2(int numSamples, int inputDim, int outputDim,
            Random rand)
        {
            var inputs = Enumerable.Range(0, numSamples * inputDim).Select(x => (float)rand.NextDouble()).Chunk(2).ToList();

            var outputs = inputs.Select(x => new float[] { (float)(x.Sum() - 0.5) }).ToList();

            return Enumerable.Range(0, numSamples).Select(x => (inputs[x], outputs[x])).ToList();
        }
    }

    // Структура для хранения обучающих данных
    struct Batch
    {
        public Value Input { get; set; }
        public Value Output { get; set; }
    }
}