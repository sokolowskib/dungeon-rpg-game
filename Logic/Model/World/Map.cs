namespace rpg;

public class Map
{
    private Tile[,] _tiles;
    private Dictionary<(int, int), List<Item>> _itemsOnGround;
    private Dictionary<(int, int), Enemy> _enemiesOnMap;
    private HashSet<string> _instructions = new HashSet<string>();
    public int Width { get; set; }
    public int Height { get; set; }
    public SoundChannel channel;
    public Random _rng;

    public void AddInstruction(string instruction) => _instructions.Add(instruction);
    public bool HasInstruction(string instruction) => _instructions.Contains(instruction);

    public IEnumerable<KeyValuePair<(int, int), Enemy>> GetAllEnemies() => _enemiesOnMap;
    public IEnumerable<KeyValuePair<(int, int), List<Item>>> GetAllItems() => _itemsOnGround;
    public IEnumerable<string> GetInstructions() => _instructions;

    public Map(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new Tile[width + 1, height + 1];
        _itemsOnGround = new Dictionary<(int, int), List<Item>>();
        _enemiesOnMap = new Dictionary<(int, int), Enemy>();
        _rng = new Random();
        channel = new SoundChannel(this);

        for (int y = 0; y <= Height; y++)
        {
            for (int x = 0; x <= Width; x++)
            {
                if (x == 0 || x == Width || y == 0 || y == Height)
                {
                    _tiles[x, y] = new WallTile();
                }
                else
                {
                    _tiles[x, y] = new FloorTile();
                }
            }
        }
    }
    public (int, int) PickRandomNeighbor((int x, int y)pos)
    {
        var dirs = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
        var shuffled = dirs.OrderBy(_p => _rng.Next());

        foreach (var (dx, dy) in shuffled)
        {
            int nx = pos.x + dx, ny = pos.y + dy;
            if (GetTile(nx, ny).CanEnter() && !HasEnemy(nx, ny))
            {
                return (nx, ny);
            }
        }
        return pos;
    }

    public void MoveAllEnemies(IEnumerable<Player> players, HashSet<(int, int)>? frozenTiles = null)
    {
        var playerList = players.ToList();
        foreach (var (pos, enemy) in _enemiesOnMap.ToList())
        {
            // Wrog na polu aktywnej walki nie rusza sie - siedzi z graczem walczacym.
            if (frozenTiles != null && frozenTiles.Contains(pos))
                continue;

            if (enemy.EffectiveSight.IsActive)
                enemy.CheckSight(playerList, this);

            (int x, int y) next;
            if (enemy.EffectiveSight.IsActive && enemy.IsSeeingPlayer)
            {
                if (enemy.EffectiveSight.ShouldFreeze())
                    continue;
                next = enemy.NextStepBySight(this);
            }
            else if (enemy.EffectiveSound.IsActive && enemy.IsHearingNoise)
                next = enemy.NextStepBySound();
            else
                next = PickRandomNeighbor(pos);

            var (x, y) = next;

            if (pos.Item1 == x && pos.Item2 == y)
                continue;
            if (HasEnemy(x, y))
                continue;

            _enemiesOnMap.Remove(pos);
            _enemiesOnMap[(x, y)] = enemy;
            enemy.X = x;
            enemy.Y = y;
        }
    }

    public bool HasLineOfSight(int x1, int y1, int x2, int y2)
    {
        int dx = Math.Abs(x2 - x1), dy = Math.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1, sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;
        int x = x1, y = y1;

        while (true)
        {
            if (x == x2 && y == y2) return true;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x += sx; }
            if (e2 < dx)  { err += dx; y += sy; }
            if (x == x2 && y == y2) return true;
            if (!_tiles[x, y].CanEnter()) return false;
        }
    }

    public Tile GetTile(int x, int y) => _tiles[x, y];
    public void SetTileAsWall(int x, int y)
    {
        if (x > 0 && x < Width && y > 0 && y < Height)
        {
            _tiles[x, y] = new WallTile();
        }
    }
    public void SetTileAsFloor(int x, int y)
    {
        if (x > 0 && x < Width && y > 0 && y < Height)
        {
            _tiles[x, y] = new FloorTile();
        }
    }
    public void AddEnemy(int x, int y, Enemy enemy)
    {
        var key = (x, y);
        if (!_enemiesOnMap.ContainsKey(key))
        {
            _enemiesOnMap[key] = enemy;
            enemy.X = x;
            enemy.Y = y;
            enemy.SetMap(this);
            enemy.SetAndSubscribeToChannel(channel);
        }
    }
    public void MoveEnemy(int x, int y, Enemy enemy)
    {
        if (_enemiesOnMap.ContainsKey((x, y)))
            return;
        _enemiesOnMap.Remove((enemy.X, enemy.Y));
        _enemiesOnMap[(x, y)] = enemy;
        enemy.X = x;
        enemy.Y = y;
    }
    public void RemoveEnemy(int x, int y)
    {
        var key = (x, y);
        if (_enemiesOnMap.ContainsKey(key))
        {
            _enemiesOnMap.Remove(key);
        }
    }
    public Enemy? GetEnemyAt(int x, int y)
    {
        var key = (x, y);
        if (_enemiesOnMap.ContainsKey(key))
        {
            return _enemiesOnMap[key];
        }
        return null;
    }
    public bool HasEnemy(int x, int y)
    {
        var key = (x, y);
        return _enemiesOnMap.ContainsKey(key);
    }
    public char GetSymbolFromEnemy(int x, int y)
    {
        var key = (x, y);
        if (_enemiesOnMap.ContainsKey(key))
        {
            return _enemiesOnMap[key].Symbol;
        }
        return ' ';
    }

    public void AddItem(int x, int y, Item item)
    {
        var key = (x, y);
        if (!_itemsOnGround.ContainsKey(key))
            _itemsOnGround[key] = new List<Item>();
        _itemsOnGround[key].Add(item);
    }

    public void RemoveItem(int x, int y, Item item)
    {
        var key = (x, y);
        if (_itemsOnGround.ContainsKey(key))
        {
            _itemsOnGround[key].Remove(item);
            if (_itemsOnGround[key].Count == 0)
                _itemsOnGround.Remove(key);
        }
    }

    public List<Item> GetItemsAt(int x, int y)
    {
        var key = (x, y);
        if (_itemsOnGround.ContainsKey(key))
            return _itemsOnGround[key];
        return new List<Item>();
    }

    public bool HasItems(int x, int y) => _itemsOnGround.ContainsKey((x, y));

    public char GetItemSymbol(int x, int y)
    {
        var items = GetItemsAt(x, y);
        if (items.Count > 0)
            return items[0].Symbol;
        return ' ';
    }
}
