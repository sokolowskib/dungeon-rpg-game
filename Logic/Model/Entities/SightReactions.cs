namespace rpg;

public interface ISightReaction
{
    bool IsActive { get; }
    bool ShouldFreeze();
    (int, int) NextStep(int x, int y, int targetX, int targetY, Map map);
}

public class AggressiveSight : ISightReaction
{
    public bool IsActive => true;
    public bool ShouldFreeze() => false;
    public (int, int) NextStep(int x, int y, int targetX, int targetY, Map map)
    {
        return SightOps.PickBestNeighbor(x, y, targetX, targetY, map, preferCloser: true);
    }
}

public class CowardlySight : ISightReaction
{
    private static readonly Random _rng = new Random();
    public bool IsActive => true;
    public bool ShouldFreeze() => _rng.Next(2) == 0;
    public (int, int) NextStep(int x, int y, int targetX, int targetY, Map map)
    {
        return SightOps.PickBestNeighbor(x, y, targetX, targetY, map, preferCloser: false);
    }
}

public class NeutralSight : ISightReaction
{
    public bool IsActive => false;
    public bool ShouldFreeze() => false;
    public (int, int) NextStep(int x, int y, int targetX, int targetY, Map map) => (x, y);
}

public static class SightOps
{
    public static (int, int) PickBestNeighbor(int x, int y, int tx, int ty, Map map, bool preferCloser)
    {
        var dirs = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
        int bestDist = Math.Abs(x - tx) + Math.Abs(y - ty);
        (int, int) best = (x, y);

        foreach (var (dx, dy) in dirs)
        {
            int nx = x + dx, ny = y + dy;
            if (!map.GetTile(nx, ny).CanEnter()) continue;
            if (map.HasEnemy(nx, ny)) continue;

            int d = Math.Abs(nx - tx) + Math.Abs(ny - ty);
            bool better = preferCloser ? d < bestDist : d > bestDist;
            if (better)
            {
                bestDist = d;
                best = (nx, ny);
            }
        }
        return best;
    }
}
