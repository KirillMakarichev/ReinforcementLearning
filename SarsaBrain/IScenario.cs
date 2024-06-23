namespace SarsaBrain;

public interface IScenario
{
    void Tick(ModeControl control = ModeControl.LearningPc, bool withFuturePossibleStates = false);
}