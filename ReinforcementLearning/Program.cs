

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.ActivationFunctions;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.WeightInitializer;
using NeuralNetwork.Backpropagation;
using NeuralNetwork.Backpropagation.ActivationFunctions;
using SarsaBrain;



return;
var learningRate = 0.01;

var numInputs = 3600;
var numOutputs = 1;
var numHiddenLayers = 3;
var numNeuronsInHiddenLayer = 30;

//var network = await NeuralNetworkExtensions.LoadFromJsonAsync("dump.txt");

var somaFactory = SomaFactory.GetInstance(new SimpleSummation());

INeuralNetworkFactory factory =
    new BackpropagationNetworkFactoryBuilder().BuildBackpropagationNetworkFactory(
        new RandomWeightInitializer(Random.Shared), somaFactory,
        new TanhActivationFunctionWithDerivative(), new TanhActivationFunction(),
        new NeuronFactory());
var network = factory.Create(numInputs, numOutputs, numHiddenLayers, numNeuronsInHiddenLayer);

var backpropagater = new Backpropagater(network, learningRate, 0.0001, 1, false);

// var network = NeuralNetworkFactory.GetInstance()
// .Create(numInputs, numOutputs, numHiddenLayers, numNeuronsInHiddenLayer);



var epoches = 70;
var test = new List<string>();
test.AddRange(Directory.GetFiles(@"D:\Downloads\archive\PetImages\animalsTest")
    .Where(x => x.ToLowerInvariant().Contains("cat")).Take(15).ToList());
test.AddRange(Directory.GetFiles(@"D:\Downloads\archive\PetImages\animalsTest")
    .Where(x => x.ToLowerInvariant().Contains("dog")).Take(15).ToList());

for (var epoch = 0; epoch < epoches; epoch++)
{
    var fileId = 0;
    //var fileNames = Directory.GetFiles(@"D:\Downloads\archive\PetImages\animalsTest");
    var mixed = test.OrderBy(v => Random.Shared.Next()).ToList();
    foreach (var fileName in mixed)
    {
        Console.WriteLine(epoch);
        Console.WriteLine(new FileInfo(fileName).Name);
        var dest = ResizeImage(Bitmap.FromFile(fileName), 60, 60);
        var inputs = ConvertBitMapIntoInputs(dest);
        NormalizeData(inputs);

        //cat - 1, dog - -1
        var expectedResult = new FileInfo(fileName).Name.ToLowerInvariant().Contains("cat") ? 1 : -1;

        //if (expectedResult == -1) continue;

        network.SetInputs(inputs);
        network.Process();

        var output = network.GetOutputs()[0];
        var isTestPassed = (expectedResult == 1 && output > 0) || (expectedResult == -1 && output < 0);
        
        Console.ForegroundColor = isTestPassed ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(output);
        Console.ResetColor();
        
        backpropagater.Backpropagate(new double []{ expectedResult });

        Console.WriteLine();
        
        fileId++;

        if (fileId == test.Count - 1)
        {
            await network.SaveAsync("dump.txt");
        }
    }

}

{
    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine();

    foreach (var fileName in Directory.GetFiles(@"D:\Downloads\archive\PetImages\animalsTest")
                 .OrderBy(x => Random.Shared.Next()).ToList()
                 )
    {
        Console.WriteLine(new FileInfo(fileName).Name);
        var dest = ResizeImage(Bitmap.FromFile(fileName), 60, 60);
        var inputs = ConvertBitMapIntoInputs(dest);
        NormalizeData(inputs);

        //cat - 1, dog - -1
        var expectedResult = new FileInfo(fileName).Name.ToLowerInvariant().Contains("cat") ? 1 : -1;
        network.SetInputs(inputs);
        network.Process();
        var output = network.GetOutputs()[0];
        var isTestPassed = (expectedResult == 1 && output > 0) || (expectedResult == -1 && output < 0);

        Console.ForegroundColor = isTestPassed ? ConsoleColor.Green : ConsoleColor.Red;
        
        Console.WriteLine($"expected = {expectedResult}; actual result = {output}");
        Console.ResetColor();
    }

    await network.SaveAsync("dump.txt");
}


// 0    1 2 3 4 5    6
//            0 1    2


static void NormalizeData(double[] array)
{
    var min = array.Min();
    var max = array.Max();
    
    var range = max - min;
    for (var i = 0; i < array.Length; i++)
    {
        array[i] = Math.Round((array[i] - min) / range, 2);
    }
    for (var i = 0; i < array.Length; i++)
    {
        array[i] = array[i];
    }
}

static Bitmap ResizeImage(Image image, int width, int height)
{
    var destRect = new Rectangle(0, 0, width, height);
    var destImage = new Bitmap(width, height);

    destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

    using var graphics = Graphics.FromImage(destImage);
    graphics.CompositingMode = CompositingMode.SourceCopy;
    graphics.CompositingQuality = CompositingQuality.HighQuality;
    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
    graphics.SmoothingMode = SmoothingMode.HighQuality;
    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

    using var wrapMode = new ImageAttributes();
    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);

    return destImage;
}

static int Brightness(Color pixel)
{
    var result = 0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B;
    return result < 128 ? 0 : 1;
}

static double[] ConvertBitMapIntoInputs(Bitmap bitmap)
{
    var destBytes = new List<double>();
    for (var i = 0; i < 60; i++)
    {
        for (var j = 0; j < 60; j++)
        {
            var pixel = bitmap.GetPixel(i, j);
     //       var brightness = Brightness(pixel);
            destBytes.Add(pixel.ToArgb());
        }
    }

    return destBytes.ToArray();
}