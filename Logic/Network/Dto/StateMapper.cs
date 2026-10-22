namespace rpg;

/// <summary>
///  klasa ma na celu zmienic obiekty modelu gry do obiektow DTO. ulatwia serializacje do json, co pozwala na lepszy
///  przesyl przez siec. w pelni statyczna
/// </summary>

public static class StateMapper
{
    public static EnemyDto ToDto(Enemy e)
    {
        return new EnemyDto {
            Name = e.Name, X = e.X, Y = e.Y, Health = e.Health, Damage = e.DMG, Armor = e.Armor, Symbol = e.Symbol,
        };
    }

    public static ItemDto ToDto(Item i)
    {
        var dto = new ItemDto {
            Name = i.Name,
            Description = i.GetDescription(),
            Symbol = i.Symbol,
            HasSlots = i.HasSlots,
        };
        if (i.HasSlots)
        {
            var slotsList = new List<ItemDto?>();
            foreach (Item? c in i.GetSlots())
            {
                if (c == null)
                    slotsList.Add(null);
                else
                    slotsList.Add(ToDto(c));
            }
            dto.Slots = slotsList;
        }
        return dto;
    }
    public static PlayerDto ToDto(Player p, int id)
    {
        return new PlayerDto { HP = p.Health,
                               X = p.GetPositionX(),
                               Y = p.GetPositionY(),
                               Mana = p.Mana,
                               Strength = p.Strength,
                               Dexterity = p.Dexterity,
                               Luck = p.Luck,
                               Rage = p.Rage,
                               Gold = p.Gold,
                               Coins = p.Coins,
                               Hands = p.Hands.Select(i => i?.GetDescription()).ToArray(),
                               Inventory = p.Inventory.Select(i => i == null ? null : ToDto(i)).ToArray(),
                               SelectedHand = p.SelectedHand,
                               SelectedSlot = p.SelectedSlot,
                               ID = id,
                               Level = p.Level,
                               WinsTowardsLevel = p.WinsTowardsLevel,
                               Messages = p.GetMessages,
                               Combat = ToDto(p.ActiveCombat),
                               SlotEditor = ToDto(p.SlotEditor) };
    }

    public static SlotEditorDto? ToDto(SlotEditorSession? s)
    {
        if (s == null)
            return null;
        return new SlotEditorDto {
            InventoryCursor = s.InventoryCursor,
            FocusedInventoryIndex = s.FocusedInventoryIndex,
            MaxRenderedHeight = s.MaxRenderedHeight,
            SlotPath = s.SlotPath == null ? null : new List<int>(s.SlotPath),
        };
    }

    public static CombatDto? ToDto(CombatSession? session)
    {
        if (session == null)
            return null;
        var e = session.Combat._enemy;
        return new CombatDto {
            EnemyName = e.Name,
            EnemyHp = e.Health,
            EnemyDamage = e.DMG,
            EnemyArmor = e.Armor,
            FightLog = session.Combat.FightLog.ToList(),
        };
    }

    public static MapDto ToDto(Map m)
    {
        var tiles = new char [m.Height + 1][];
        for (int y = 0; y <= m.Height; y++)
        {
            tiles[y] = new char[m.Width + 1];
            for (int x = 0; x <= m.Width; x++)
                tiles[y][x] = m.GetTile(x, y).Symbol;
        }

        var enemies = m.GetAllEnemies().Select(kv => ToDto(kv.Value)).ToList();

        var piles = m.GetAllItems()
                        .Select(kv => new ItemPileDto { X = kv.Key.Item1, Y = kv.Key.Item2,
                                                        Items = kv.Value.Select(ToDto).ToList() })
                        .ToList();

        var instructions = m.GetInstructions().ToList();

        return new MapDto {
            Width = m.Width,   Height = m.Height,     Tiles = tiles,
            Enemies = enemies, ItemsOnGround = piles, Instructions = instructions,
        };
    }
    public static GameStateDto ToDto(GameModel gm)
    {
        var players = gm.Players.Select(kv => ToDto(kv.Value, kv.Key)).ToList();
        var journal = EventJournalManager.Instance.GetRecent(50);
        return new GameStateDto { Map = ToDto(gm.Map), Players = players, Journal = journal };
    }
}
