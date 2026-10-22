namespace rpg;

public class SoundChannel
{
    public List<Enemy> _subscribers = new List<Enemy>();
    private Map _map;
    public SoundChannel(Map map)
    {
        _map = map;
    }

    public void Subscribe(Enemy enemy)
    {
        _subscribers.Add(enemy);
    }
    public void Unsubscribe(Enemy enemy)
    {
        _subscribers.Remove(enemy);
    }

    public void MakeDistanceGradient(SoundNotification n, out Dictionary<(int, int), int> gradient)
    {
        gradient = new Dictionary<(int, int), int>();
        var source = (n.source.x, n.source.y);
        gradient[source] = n.NoiseLevel;

        var dirs = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
        var frontier = new Queue<(int x, int y)>();
        frontier.Enqueue(source);

        while (frontier.Count > 0)
        {
            var pos = frontier.Dequeue();
            int level = gradient[pos];
            if (level <= 1) continue;

            foreach (var (dx, dy) in dirs)
            {
                var next = (pos.x + dx, pos.y + dy);
                if (gradient.ContainsKey(next)) continue;
                if (!_map.GetTile(next.Item1, next.Item2).CanEnter()) continue;

                gradient[next] = level - 1;
                frontier.Enqueue(next);
            }
        }
    }

    public void NotifySubscribers(SoundNotification n)
    {
        MakeDistanceGradient(n, out Dictionary<(int, int), int> gradient);
        foreach (var enemy in _subscribers)
        {
            enemy.ReactToNoise(n, gradient);
        }
    }
}
