namespace SarsaBrain;

public interface ISaver
{
    Task SaveAsync(string path);
}