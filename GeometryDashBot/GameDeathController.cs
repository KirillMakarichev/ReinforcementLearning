namespace GeometryDashBot;

class GameDeathController
{
    private readonly GDApiManager _process;
    private bool _lastState;
    
    public GameDeathController(GDApiManager process)
    {
        _process = process;
    }

    /// <summary>
    /// if state hadn't changed return value is null, otherwise it's the value the state was changed on
    /// </summary>
    /// <returns>(changed on, currentState)</returns>
    public (bool? changedOn, bool currentState) AliveStateChangedOn()
    {
        var isDead = _process.IsDead();

        if (isDead == _lastState) return (null, isDead);
        _lastState = isDead;

        return (isDead, isDead);
    }

    public bool IsDead() => _process.IsDead();
}