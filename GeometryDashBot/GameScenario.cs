using InputManager;
using SarsaBrain;

namespace GeometryDashBot;

public class GameScenario : ScenarioBase<ControllerAgent, ControllerActions, ControllerState>
{
    private readonly Size _expectedSize;
    private readonly Size _monitorSize;
    private readonly GameDeathController _deathController;
    private readonly GDApiManager _gdApiManager;
    private readonly BackendSaver _backendSaver;
    private InputManager _inputManager;

    private GameScenario(ControllerAgent agent) : base(agent)
    {
        _gdApiManager = new GDApiManager();

        _deathController = new GameDeathController(_gdApiManager);
        _backendSaver = new BackendSaver(agent,
            $"gd_{DateTime.UtcNow.ToString($"yyyy-MM-dd_{Guid.NewGuid().ToString()}")}.txt");
        _backendSaver.StartSaving(TimeSpan.FromMinutes(2.5));
    }

    public GameScenario(ConstantsInitializer constantsInitializer, NeuralNetworkSettings neuralNetworkSettings,
        Size expectedSize, Size monitorSize, InputManager? inputManager = null) : this(
        new ControllerAgent(constantsInitializer, neuralNetworkSettings, new GpuNeuralNetwork(neuralNetworkSettings)))
    {
        _expectedSize = expectedSize;
        _monitorSize = monitorSize;
        _inputManager = inputManager ?? new InputManager();
    }

    public override void Tick(ModeControl control = ModeControl.LearningPc, bool withFuturePossibleStates = false)
    {
        if (!GDApiManager.GameOnTop()) return;

        var aliveStateChangedOn = _deathController.AliveStateChangedOn();
        if (aliveStateChangedOn is { currentState: true, changedOn: null })
            return;

        base.Tick(control, withFuturePossibleStates);
    }

    protected override double[] GetSensorsInState(ControllerState state)
    {
        var screen = ScreenshotMaker.TakeScreenshot(_monitorSize);
        var resizedScreen = ScreenshotMaker.ResizeBitmap(screen, _expectedSize.Width, _expectedSize.Height);

        var values = new double[_expectedSize.Width * _expectedSize.Height];

        for (int i = 0; i < resizedScreen.Height; i++)
        {
            for (int j = 0; j < resizedScreen.Width; j++)
            {
                values[i * resizedScreen.Width + j] = NormalizeColor(resizedScreen.GetPixel(j, i));
            }
        }

        return values;
    }

    protected override float ReleaseDecision(ControllerActions action, ControllerState currentState)
    {
        var levelPercent = _gdApiManager.GetLevelPercent();
        var isDead = _deathController.AliveStateChangedOn();

        if (isDead.currentState)
        {
            Done = true;
            return -3f * levelPercent; // награда за смерть
        }

        switch (action)
        {
            case ControllerActions.ButtonClick:
                _inputManager.MousePress(Mouse.MouseKeys.Left);
                break;
            case ControllerActions.ButtonDown:
                _inputManager.MouseDown(Mouse.MouseKeys.Left);
                break;
            case ControllerActions.ButtonUp:
                _inputManager.MouseUp(Mouse.MouseKeys.Left);
                break;
        }

        return 1f * levelPercent;
    }

    protected override void BeforeLearn(bool done)
    {
        if (!done) return;

        _inputManager.KeyboardPress(Keys.Escape);
    }

    protected override void AfterLearn(bool done)
    {
        if (!done) return;

        _inputManager.KeyboardPress(Keys.Space);
    }

    private static double NormalizeColor(Color color)
    {
        return (color.A / 255.0 + color.B / 255.0 + color.R / 255.0 + color.G / 255.0) / 4.0;
    }
}