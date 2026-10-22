namespace rpg;

public class Player
{
    int _x;
    int _y;

    public int Health { get; set; }
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Luck { get; set; }
    public int Rage { get; set; }
    public int Mana { get; set; }
    public int Coins { get; set; }
    public int Gold { get; set; }
    public int Level { get; set; } = 1;
    public int WinsTowardsLevel { get; set; } = 0;
    public const int WinsPerLevel = 3;

    public void RegisterCombatWin(int playerId)
    {
        WinsTowardsLevel++;
        if (WinsTowardsLevel >= WinsPerLevel)
        {
            WinsTowardsLevel = 0;
            Level++;
            Strength += 5;
            Dexterity += 5;
            Luck += 5;
            Rage += 5;
            Mana += 5;
            Health = Math.Min(Health + 20, 100);
            AddMessage($"LEVEL UP! Osiagnieto poziom {Level}.");
            EventJournalManager.Instance.Log($"Gracz {playerId} osiagnal poziom {Level}");
        }
    }
    public CombatSession? ActiveCombat { get; set; }
    public SlotEditorSession? SlotEditor { get; set; }

    public Item?[] Inventory = new Item[5];
    public Item?[] Hands = new Item[2];
    public int _selectedSlot = 0;
    public int _selectedHand = 0;
    public int SelectedHand => _selectedHand;
    public int SelectedSlot => _selectedSlot;
    public List<string> _messages = new List<string>();
    public int GetInventoryCount()
    {
        return Inventory.Count(i => i != null);
    }
    public int GetHandItemCount()
    {
        return Hands.Count(h => h != null);
    }
    public void RemoveOldestMessage()
    {
        if (_messages.Count > 0)
        {
            _messages.RemoveAt(0);
        }
    }
    public void SetPosition(int x, int y, GameModel model)
    {
        if (model.IsTileInCombat(x, y))
        {
            EventJournalManager.Instance.Log("Proba wejscia na pole z walka");
            return;
        }
        if (model.Map.GetTile(x, y).CanEnter())
        {
            _x = x;
            _y = y;
        }
        else
        {
            EventJournalManager.Instance.Log("Proba wejscia w sciane");
        }
    }

    public Player(int health, int strength, int dexterity, int luck, int rage, int mana)
    {
        _x = 1;
        _y = 1;
        Health = health;
        Strength = strength;
        Dexterity = dexterity;
        Luck = luck;
        Rage = rage;
        Mana = mana;
        Coins = 0;
        Gold = 0;
    }
    public void AddMessage(string message)  // dodanie wiadomosci do wyswietlenia w footerze
    {
        _messages.Add(message);
        if (_messages.Count > 3)
            _messages.RemoveAt(0);
    }
    public List<string> GetMessages => _messages;

    public void ChangeSlot(int slot)
    {
        _selectedSlot = slot;
    }
    public void Equip(Map map)
    {
        Item? itemInInventory = Inventory[_selectedSlot];
        Item? itemInHand = Hands[_selectedHand];

        if (itemInInventory == null)  // przypadek gdzie odkladasz item  z lapki do pustego miejsca w inventory
        {
            if (itemInHand != null)
            {
                if (Hands[0] != null && Hands[0] == Hands[1])  // case ze trzymasz oburacz bron
                {
                    Inventory[_selectedSlot] = Hands[0];
                    Hands[0] = null;
                    Hands[1] = null;
                }
                else
                {
                    Inventory[_selectedSlot] = itemInHand;
                    Hands[_selectedHand] = null;
                }
                AddMessage($"Odłożono {itemInHand.Name} do plecaka.");
                itemInHand.OnUnequip(this);
            }
            else
            {
                AddMessage("Ten slot jest pusty i ręka też.");
            }
            return;
        }

        itemInInventory.OnEquip(this, map);
    }

    public void DropItemFromHand(Map map)
    {
        Item? itemToDrop = Inventory[_selectedSlot];
        if (itemToDrop != null)
        {
            Inventory[_selectedSlot] = null;
            map.AddItem(_x, _y, itemToDrop);
        }
        else
        {
            AddMessage("Nie masz czego wyrzucić!");
        }
    }


    public void EquipSingleHand(Item item, Map map)
    {
        Item? currentInHand = Hands[_selectedHand];

        if (Hands[0] != null && Hands[0] == Hands[1])
        {
            Item? oldTwoHanded = Hands[0];
            Hands[0] = null;
            Hands[1] = null;

            Inventory[_selectedSlot] = oldTwoHanded;
            Hands[_selectedHand] = item;
        }
        else
        {
            Hands[_selectedHand] = item;
            Inventory[_selectedSlot] = currentInHand;
        }
        currentInHand?.OnUnequip(this);
        EventJournalManager.Instance.Log($"Wzieto do reki: {item.Name}");
    }
    public void EquipDoubleHand(Item item, Map map)
    {
        Item? left = Hands[0];
        Item? right = Hands[1];

        Inventory[_selectedSlot] = null;

        if (left != null && left == right)
        {
            if (!AddToInventory(left))  // teoretycznie zawsze przejdzie
                map.AddItem(_x, _y, left);
            left.OnUnequip(this);
        }
        else
        {
            if (left != null)
                if (!AddToInventory(left))
                    map.AddItem(_x, _y, left);
            if (right != null)
                if (!AddToInventory(right))
                    map.AddItem(_x, _y, right);
            right?.OnUnequip(this);
            left?.OnUnequip(this);
        }

        Hands[0] = Hands[1] = item;
        EventJournalManager.Instance.Log($"Wzieto do reki: {item.Name}");
    }

    public void TryPickUp(Map map)  // podnoszenie itema z mapy
    {
        var items = map.GetItemsAt(_x, _y);
        if (items.Count > 0)
        {
            Item priority = items[0];

            if (priority.OnPickUp(this))
            {
                map.RemoveItem(_x, _y, priority);
                if (priority.NoiseRange > 0)
                    map.channel.NotifySubscribers(new SoundNotification(priority.NoiseRange, _x, _y));
            }
        }
    }
    public bool AddToInventory(Item item)
    {
        for (int i = 0; i < Inventory.Length; i++)
        {
            if (Inventory[i] == null)
            {
                Inventory[i] = item;
                return true;
            }
        }
        AddMessage("Zrób miejsce w ekwipunku!");
        return false;
    }

    public void SwapSelectedHand()
    {
        _selectedHand = _selectedHand == 0 ? 1 : 0;
    }

    public bool IsLeftHandEmpty() => Hands[0] == null;
    public bool IsRightHandEmpty() => Hands[1] == null;

    public int GetPositionX() => _x;
    public int GetPositionY() => _y;

    public void ReceiveDamage(int dmg)
    {
        Health = Health - dmg;
    }
}
