namespace rpg;

public interface ISoundReaction
{
    bool IsActive { get; }
    (int, int) NextStep(int x, int y, Dictionary<(int, int), int> gradient);
}

public class AggressiveSound : ISoundReaction
{
    public bool IsActive => true;
    public (int, int) NextStep(int x, int y, Dictionary<(int, int), int> gradient)
    {
        if (!gradient.TryGetValue((x, y), out int myLevel))
            return (x, y);

        var dirs = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
        foreach (var (dx, dy) in dirs)
        {
            var key = (x + dx, y + dy);
            if (gradient.TryGetValue(key, out int nd) && nd == myLevel + 1)
                return key;
        }
        return (x, y);
    }
}

public class CowardlySound : ISoundReaction
{
    public bool IsActive => true;
    public (int, int) NextStep(int x, int y, Dictionary<(int, int), int> gradient)
    {
        if (!gradient.TryGetValue((x, y), out int myLevel))
            return (x, y);

        var dirs = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
        foreach (var (dx, dy) in dirs)
        {
            var key = (x + dx, y + dy);
            if (gradient.TryGetValue(key, out int nd) && nd == myLevel - 1)
                return key;
        }
        return (x, y);
    }
}

public class NeutralSound : ISoundReaction
{
    public bool IsActive => false;
    public (int, int) NextStep(int x, int y, Dictionary<(int, int), int> gradient) => (x, y);
}
