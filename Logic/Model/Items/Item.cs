namespace rpg;

public abstract class Item
{
    public virtual string Name { get; }
    public virtual int Damage { get; }
    public virtual char Symbol { get; }
    public virtual int HandsRequired { get; }

    protected Item(string name, int damage, char symbol, int handsRequired)
    {
        Name = name;
        Damage = damage;
        Symbol = symbol;
        HandsRequired = handsRequired;
    }

    public virtual bool OnPickUp(Player player) => OnPickUpAs(this, player);

    public virtual bool OnPickUpAs(Item self, Player player)
    {
        if (player.AddToInventory(self))
        {
            player.AddMessage($"Podniesiono {self.Name}");
            EventJournalManager.Instance.Log($"Podniesiono przedmiot: {self.Name}");
            return true;
        }
        return false;
    }
    public virtual void OnEquip(Player player, Map map) => OnEquipAs(this, player, map);
    public virtual void OnEquipAs(Item self, Player player, Map map) {}
    public virtual void OnUnequip(Player player) {}

    public virtual string GetDescription()
    {
        if (Damage > 0 && HandsRequired == 1)
            return $"{Name} (DMG: {Damage})";
        else if (Damage > 0 && HandsRequired == 2)
        {
            return $"{Name} (DMG:{Damage}) broń dwuręczna";
        }
        return Name;
    }

    public virtual int Accept(IAttackVisitor v) => 0;
    public virtual int Accept(IDefenseVisitor v) => 0;
    public virtual int NoiseRange => 0;

    public virtual void ApplyPassiveEffects(Player player) {}
    public virtual void RemovePassiveEffects(Player player) {}
    public virtual bool HasSlots => false;
    public virtual IReadOnlyList<Item?> GetSlots() => Array.Empty<Item?>();
    public virtual bool CanAccept(Item child) => false;
    public virtual void SetSlot(int index, Item? child) {}

    public int CountDisplayLines()
    {
        if (!HasSlots) return 0;
        int total = 0;
        foreach (var c in GetSlots())
        {
            total += 1;
            if (c != null)
                total += c.CountDisplayLines();
        }
        return total;
    }
}

public class MiscItem : Item
{
    public MiscItem(string name, char symbol) : base(name, 0, symbol, 0) {}

    public override string GetDescription()
    {
        return $"{Name} (brak zastosowania bojowego)";
    }
    public override void OnEquipAs(Item self, Player player, Map map)
    {
        player.AddMessage($"Przedmiotu {self.Name} nie można użyć jako broni");
    }
}
