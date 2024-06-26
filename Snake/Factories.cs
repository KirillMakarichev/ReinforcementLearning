﻿using SarsaBrain;

namespace Snake;

public static class Factories
{
    private static ConstantsInitializer _constantsInitializer = new()
    {
        ExplorationDefault = 1,
        ExplorationDecay = 0.995,
        ExplorationMin = 0.01,
        LearningRate = 0.001,
        DiscountFactor = 0.999,
        ReplayMemoryCapacity = 20000,
        ReplayMemoryMinSize = 2048,
        MiniBatchSize = 8192,
    };

    private static NeuralNetworkSettings _neuralNetworkSettings = new()
    {
        NumInputs = 446,
        NumOutputs = 4,
        NumHiddenLayers = 10,
        NumNeuronsInHiddenLayer = 10,
        LearningRate = _constantsInitializer.LearningRate
    };
    
    public static Game1 CreateGame(int rows, int columns)
    {
        var snake = new Snake(_constantsInitializer, _neuralNetworkSettings, new SarsaBrain.ArtificialNeuralNetwork(_neuralNetworkSettings));
        return new Game1(rows: rows, columns: columns, snake: snake, brainStatisticsCollector: snake);
    }

    public static async Task<Game1> CreateGame(int rows, int columns, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return CreateGame(rows, columns);
        
        var network = await SarsaBrain.ArtificialNeuralNetwork.LoadFromJson(_neuralNetworkSettings, path);
        var snake = new Snake(_constantsInitializer, _neuralNetworkSettings, network);
        
        return new Game1(rows: rows, columns: columns, snake: snake, brainStatisticsCollector: snake);
    }
}