namespace rpg;

public class DungeonBuilder
{
    private Map _map;
    private Random _rng;
    private List<string> _introLines = new List<string>();

    public DungeonBuilder(Map map)
    {
        _map = map;
        _rng = new Random();
    }

    public List<string> GetIntroLines() => _introLines;

    public DungeonBuilder EmptyDungeon()
    {
        for (int y = 1; y < _map.Height; y++)
            for (int x = 1; x < _map.Width; x++)
                _map.SetTileAsFloor(x, y);
        _introLines.Add("Rozległa pustka rozpościera się przed tobą...");
        return this;
    }

    public DungeonBuilder FilledDungeon()
    {
        for (int y = 1; y < _map.Height; y++)
            for (int x = 1; x < _map.Width; x++)
                _map.SetTileAsWall(x, y);
        _introLines.Add("Ciemność wypełnia podziemia...");
        return this;
    }

    public DungeonBuilder AddCorridors(int width = 1)
    {
        int step = width + 1;
        int cols = (_map.Width - 1 - width) / step + 1;
        int rows = (_map.Height - 1 - width) / step + 1;

        if (cols < 1 || rows < 1)
            return this;

        bool[,] visited = new bool[cols, rows];
        var stack = new Stack<(int cx, int cy)>();

        int startCx = _rng.Next(cols);
        int startCy = _rng.Next(rows);
        visited[startCx, startCy] = true;
        CarveCell(startCx, startCy, width, step);
        stack.Push((startCx, startCy));

        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { -1, 1, 0, 0 };

        while (stack.Count > 0)
        {
            var (cx, cy) = stack.Peek();
            var neighbors = new List<int>();

            for (int d = 0; d < 4; d++)
            {
                int nx = cx + dx[d];
                int ny = cy + dy[d];
                if (nx >= 0 && nx < cols && ny >= 0 && ny < rows && !visited[nx, ny])
                    neighbors.Add(d);
            }

            if (neighbors.Count == 0)
            {
                stack.Pop();
                continue;
            }

            int dir = neighbors[_rng.Next(neighbors.Count)];
            int ncx = cx + dx[dir];
            int ncy = cy + dy[dir];
            visited[ncx, ncy] = true;

            CarvePath(cx, cy, ncx, ncy, width, step);
            CarveCell(ncx, ncy, width, step);
            stack.Push((ncx, ncy));
        }

        _introLines.Add("Kręte korytarze prowadzą w nieznane...");
        return this;
    }

    private void CarveCell(int cx, int cy, int width, int step)
    {
        int baseX = 1 + cx * step;
        int baseY = 1 + cy * step;
        for (int dy = 0; dy < width; dy++)
            for (int dx = 0; dx < width; dx++)
                _map.SetTileAsFloor(baseX + dx, baseY + dy);
    }

    private void CarvePath(int fromCx, int fromCy, int toCx, int toCy, int width, int step)
    {
        int fx = 1 + fromCx * step;
        int fy = 1 + fromCy * step;
        int tx = 1 + toCx * step;
        int ty = 1 + toCy * step;

        int dirX = Math.Sign(tx - fx);
        int dirY = Math.Sign(ty - fy);

        if (dirX != 0)
        {
            int startX = Math.Min(fx, tx);
            int endX = Math.Max(fx, tx) + width - 1;
            for (int x = startX; x <= endX; x++)
                for (int w = 0; w < width; w++)
                    _map.SetTileAsFloor(x, fy + w);
        }
        else
        {
            int startY = Math.Min(fy, ty);
            int endY = Math.Max(fy, ty) + width - 1;
            for (int y = startY; y <= endY; y++)
                for (int w = 0; w < width; w++)
                    _map.SetTileAsFloor(fx + w, y);
        }
    }

    public DungeonBuilder AddRooms(int count, int minSize, int maxSize)
    {
        for (int i = 0; i < count; i++)
        {
            int size = _rng.Next(minSize, maxSize + 1);
            int x = _rng.Next(1, _map.Width - size);
            int y = _rng.Next(1, _map.Height - size);

            for (int dy = 0; dy < size; dy++)
                for (int dx = 0; dx < size; dx++)
                    _map.SetTileAsFloor(x + dx, y + dy);
        }
        _introLines.Add("Odkryto tajemnicze komnaty...");
        return this;
    }

    public DungeonBuilder AddCentralHall(int width, int height)
    {
        int startX = (_map.Width - width) / 2;
        int startY = (_map.Height - height) / 2;

        for (int dy = 0; dy < height; dy++)
            for (int dx = 0; dx < width; dx++)
                _map.SetTileAsFloor(startX + dx, startY + dy);

        _introLines.Add("W sercu lochu kryje się wielka sala...");
        return this;
    }

    public DungeonBuilder AddItems(int count)
    {
        string[] names = { "Sukuna's finger", "Basement key", "Map to Shiganshina", "Rotten potato",
                           "Quick Ball",      "Leaf Stone",   "Rare candy",         "Bottomless hat" };
        char[] symbols = { '!', '?', '*', '+', '~', '^', '"', ':' };

        _map.AddInstruction("pickup");
        _map.AddInstruction("drop");

        PlaceRandomly(count, () =>
                             {
                                 int idx = _rng.Next(names.Length);
                                 return new MiscItem(names[idx], symbols[idx]);
                             });

        _introLines.Add("Na podłodze leżą dziwne przedmioty...");
        return this;
    }
    private Item ApplyDecorator(Item weapon)
    {
        var dmgDecorator = new List<Func<Item, Item>> {
            item => new DamageItemDecorator(item, "Silny", 6),
            item => new DamageItemDecorator(item, "Słaby", -7),
        };

        var statDecorator = new List<Func<Item, Item>> {
            item => new StatItemDecorator(item, "Nieporęczny", -5, (p, val) => p.Dexterity += val),
            item => new StatItemDecorator(item, "Fuksiarski", 10, (p, val) => p.Luck += val)
        };

        if (_rng.Next(2) == 0)
        {
            weapon = dmgDecorator[_rng.Next(dmgDecorator.Count)](weapon);
        }
        if (_rng.Next(2) == 0)
        {
            weapon = statDecorator[_rng.Next(statDecorator.Count)](weapon);
        }
        if (_rng.Next(2) == 0)
        {
            weapon = new SlottedDecorator(weapon, _rng.Next(1, 4));
        }

        return weapon;
    }

    public DungeonBuilder AddWeapons(int count)
    {
        var oneHanded = new(string name, char symbol, Func<Item, Item> weaponType)[] {
            ("Livid Dagger", '/', item => new LightWeapon(item)),
            ("Hyperion", '†', item => new MagicWeapon(item)),
            ("Inverted Spear of Heaven", '╒', item => new MagicWeapon(item)),
            ("Rika's Ring", 'o', item => new MagicWeapon(item)),
            ("Dooku's Lighsaber", '|', item => new LightWeapon(item)),
        };

        var twoHanded = new(string name, char symbol, Func<Item, Item> weaponType)[] {
            ("Soul Reaper", '&', item => new HeavyWeapon(item)),
            ("Ragnarok", 'Φ', item => new HeavyWeapon(item)),
            ("Playfull Cloud", '¥', item => new LightWeapon(item)),
            ("Anakin's LightSaber", 'Ȉ', item => new HeavyWeapon(item)),
        };

        _map.AddInstruction("pickup");
        _map.AddInstruction("drop");
        _map.AddInstruction("equip");
        _map.AddInstruction("swap_hand");

        PlaceRandomly(count, () =>
                             {
                                 if (_rng.Next(2) == 0)
                                 {
                                     var t = oneHanded[_rng.Next(oneHanded.Length)];
                                     int dmg = _rng.Next(15, 30);
                                     Item weapon = new OneHandedWeapon(t.name, dmg, t.symbol);
                                     weapon = ApplyDecorator(weapon);
                                     return t.weaponType(weapon);
                                 }
                                 else
                                 {
                                     var t = twoHanded[_rng.Next(twoHanded.Length)];
                                     int dmg = _rng.Next(30, 60);
                                     Item weapon = new DoubleHandedWeapon(t.name, dmg, t.symbol);
                                     weapon = ApplyDecorator(weapon);
                                     return t.weaponType(weapon);
                                 }
                             });

        _introLines.Add("Broń czeka na godnego wojownika...");
        return this;
    }

    public DungeonBuilder AddSlotItems(int count)
    {
        _map.AddInstruction("pickup");
        _map.AddInstruction("drop");

        var pool = new List<Func<Item>> {
            () => new StatItemDecorator(new MiscItem("kamien sily", 's'),       "+STR", 2, (p, v) => p.Strength  += v),
            () => new StatItemDecorator(new MiscItem("kamien szczescia", 'o'),  "+LCK", 2, (p, v) => p.Luck      += v),
            () => new StatItemDecorator(new MiscItem("kamien madrosci", 'm'),   "+MNA", 2, (p, v) => p.Mana      += v),
            () => new StatItemDecorator(new MiscItem("kamien zrecznosci", 'z'), "+DEX", 2, (p, v) => p.Dexterity += v),
            () => new Socket("uchwyt prosty",  '◇', 2),
            () => new Socket("uchwyt zlozony", '◆', 3),
        };

        PlaceRandomly(count, () => pool[_rng.Next(pool.Count)]());
        _introLines.Add("Kamienie i uchwyty walaja sie po lochu...");
        return this;
    }

    private List<(int x, int y)> GetWalkableFloors()
    {
        var floors = new List<(int x, int y)>();
        for (int y = 1; y < _map.Height; y++)
            for (int x = 1; x < _map.Width; x++)
                if (_map.GetTile(x, y).CanEnter() && !_map.HasItems(x, y) && !_map.HasEnemy(x, y))
                    floors.Add((x, y));
        return floors;
    }

    public DungeonBuilder AddItems(Func<Item>[] pool, int count)
    {
        _map.AddInstruction("pickup");
        _map.AddInstruction("drop");
        PlaceRandomly(count, () =>
                             { return pool[_rng.Next(pool.Length)](); });
        return this;
    }

    public DungeonBuilder AddWeapons((string name, char symbol, int hands, Func<Item, Item> weaponType)[] templates,
                                     int count)
    {
        _map.AddInstruction("pickup");
        _map.AddInstruction("drop");
        _map.AddInstruction("equip");
        _map.AddInstruction("swap_hand");

        PlaceRandomly(count, () =>
                             {
                                 var t = templates[_rng.Next(templates.Length)];
                                 int dmg = t.hands == 1 ? _rng.Next(15, 30) : _rng.Next(30, 60);
                                 Item weapon = t.hands == 1 ? new OneHandedWeapon(t.name, dmg, t.symbol)
                                                            : new DoubleHandedWeapon(t.name, dmg, t.symbol);
                                 weapon = ApplyDecorator(weapon);
                                 return t.weaponType(weapon);
                             });
        return this;
    }

    public DungeonBuilder AddEnemies((string name, int hp, int dmg, int armor, Species species, int count)[] templates)
    {
        var floorTiles = GetWalkableFloors();
        foreach (var t in templates)
        {
            for (int i = 0; i < t.count && floorTiles.Count > 0; i++)
            {
                var (x, y) = floorTiles[_rng.Next(floorTiles.Count)];
                _map.AddEnemy(x, y, new Enemy(t.name, t.hp, t.dmg, t.armor, t.species));
                floorTiles.Remove((x, y));
            }
        }
        return this;
    }

    public DungeonBuilder PlaceArtifact(Item artifact)
    {
        _map.AddInstruction("pickup");
        _map.AddInstruction("drop");
        _map.AddInstruction("equip");
        _map.AddInstruction("swap_hand");

        var floorTiles = GetWalkableFloors();
        if (floorTiles.Count > 0)
        {
            var (x, y) = floorTiles[_rng.Next(floorTiles.Count)];
            _map.AddItem(x, y, artifact);
        }
        return this;
    }

    private void PlaceRandomly(int count, Func<Item> createItem)
    {
        var floorTiles = GetWalkableFloors();

        for (int i = 0; i < count && floorTiles.Count > 0; i++)
        {
            var (x, y) = floorTiles[_rng.Next(floorTiles.Count)];
            _map.AddItem(x, y, createItem());
            floorTiles.Remove((x, y));
        }
    }
}

public abstract class DungeonStrategy
{
    public abstract void Build(DungeonBuilder builder);
}

public class DungeonTerrainStrategy : DungeonStrategy
{
    public override void Build(DungeonBuilder builder)
    {
        builder.FilledDungeon()
            .AddCorridors()
            .AddCentralHall(10, 8)
            .AddRooms(2, 3, 6)
            .AddItems(5)
            .AddWeapons(7)
            .AddSlotItems(6);
    }
}
