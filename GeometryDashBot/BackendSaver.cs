using SarsaBrain;

namespace GeometryDashBot;

public class BackendSaver : IDisposable
{
    private readonly ISaver _saver;
    private readonly string _path;
    private Thread _thread;

    private struct Parameters
    {
        public string Path;
        public TimeSpan Delay;
    }

    public BackendSaver(ISaver saver, string path)
    {
        _saver = saver;
        _path = path;
        _thread = new Thread(async (param) =>
        {
            var parameters = (Parameters)param;

            while (true)
            {
                await _saver.SaveAsync(parameters.Path);
                await Task.Delay(parameters.Delay);
            }
        });
    }

    public void StartSaving(TimeSpan delay)
    {
        _thread.Start(new Parameters()
        {
            Delay = delay,
            Path = _path
        });
    }

    public void Dispose()
    {
        _thread.Interrupt();
    }
}