namespace rpg;
public abstract class ItemDecorator : Item
{
    public Item _inner;

    public ItemDecorator(Item inner) : base(inner.Name, inner.Damage, inner.Symbol, inner.HandsRequired)
    {
        _inner = inner;
    }

    public override string Name => _inner.Name;
    public override char Symbol => _inner.Symbol;
    public override int HandsRequired => _inner.HandsRequired;
    public override int Damage => _inner.Damage;
    public override int NoiseRange => _inner.NoiseRange;
    public override bool OnPickUp(Player player) => _inner.OnPickUpAs(this, player);
    public override bool OnPickUpAs(Item self, Player player) => _inner.OnPickUpAs(self, player);
    public override void OnEquip(Player player, Map map)
    {
        _inner.OnEquipAs(this, player, map);
    }
    public override void OnEquipAs(Item self, Player player, Map map)
    {
        _inner.OnEquipAs(self, player, map);
    }
    public override void OnUnequip(Player player)
    {
        _inner.OnUnequip(player);
    }
    public override void ApplyPassiveEffects(Player player)  => _inner.ApplyPassiveEffects(player);
    public override void RemovePassiveEffects(Player player) => _inner.RemovePassiveEffects(player);
    public override bool HasSlots => _inner.HasSlots;
    public override IReadOnlyList<Item?> GetSlots() => _inner.GetSlots();
    public override bool CanAccept(Item child) => _inner.CanAccept(child);
    public override void SetSlot(int index, Item? child) => _inner.SetSlot(index, child);
}

public class HeavyWeapon : ItemDecorator
{
    public HeavyWeapon(Item inner) : base(inner) {}

    public override int Accept(IAttackVisitor v) => v.Visit(this);
    public override int Accept(IDefenseVisitor v) => v.Visit(this);
    public override int NoiseRange => 14;
}
public class LightWeapon : ItemDecorator
{
    public LightWeapon(Item inner) : base(inner) {}

    public override int Accept(IAttackVisitor v) => v.Visit(this);
    public override int Accept(IDefenseVisitor v) => v.Visit(this);
    public override int NoiseRange => 7;
}
public class MagicWeapon : ItemDecorator
{
    public MagicWeapon(Item inner) : base(inner) {}

    public override int Accept(IAttackVisitor v) => v.Visit(this);

    public override int Accept(IDefenseVisitor v) => v.Visit(this);
    public override int NoiseRange => 10;
}

public class DamageItemDecorator : ItemDecorator
{
    public string _nameMod;
    public int _dmgMod;

    public DamageItemDecorator(Item inner, string nameMod, int DamageMod) : base(inner)
    {
        _nameMod = nameMod;
        _dmgMod = DamageMod;
    }

    public override string Name => $"{_inner.Name} ({_nameMod})";
    public override int Damage => _dmgMod + _inner.Damage;
    public override int Accept(IAttackVisitor v) => _inner.Accept(v);
    public override int Accept(IDefenseVisitor v) => _inner.Accept(v);
}

public class StatItemDecorator : ItemDecorator
{
    public string _nameMod;
    public Action<Player, int> _applyStat;
    public int _bonus;

    public StatItemDecorator(Item inner, string nameMod, int bonus, Action<Player, int> applyStat) : base(inner)
    {
        _nameMod = nameMod;
        _applyStat = applyStat;
        _bonus = bonus;
    }
    public override string Name => $"{_inner.Name} ({_nameMod})";

    public override void OnEquip(Player player, Map map)
    {
        _applyStat(player, _bonus);
        base.OnEquip(player, map);
    }
    public override void OnEquipAs(Item self, Player player, Map map)
    {
        _applyStat(player, _bonus);
        base.OnEquipAs(self, player, map);
    }
    public override void OnUnequip(Player player)
    {
        _applyStat(player, -_bonus);
        base.OnUnequip(player);
    }
    public override int Accept(IAttackVisitor v) => _inner.Accept(v);
    public override int Accept(IDefenseVisitor v) => _inner.Accept(v);

    public override void ApplyPassiveEffects(Player player)  => _applyStat(player, _bonus);
    public override void RemovePassiveEffects(Player player) => _applyStat(player, -_bonus);
}

public class SlottedDecorator : ItemDecorator
{
    public Item?[] Slots;

    public SlottedDecorator(Item inner, int slotCount) : base(inner)
    {
        Slots = new Item?[slotCount];
    }

    public override bool HasSlots => true;

    public override void OnEquipAs(Item self, Player player, Map map)
    {
        _inner.OnEquipAs(self, player, map);
        foreach (var child in Slots) child?.ApplyPassiveEffects(player);
    }
    public override void OnEquip(Player player, Map map) => OnEquipAs(this, player, map);

    public override void OnUnequip(Player player)
    {
        foreach (var child in Slots) child?.RemovePassiveEffects(player);
        _inner.OnUnequip(player);
    }

    public override int Accept(IAttackVisitor v)  => _inner.Accept(v);
    public override int Accept(IDefenseVisitor v) => _inner.Accept(v);

    public override bool CanAccept(Item child) => true;
    public override IReadOnlyList<Item?> GetSlots() => Slots;
    public override void SetSlot(int index, Item? child) => Slots[index] = child;

    public override string GetDescription()
    {
        int filled = Slots.Count(s => s != null);
        return $"{_inner.GetDescription()} [sloty: {filled}/{Slots.Length}]";
    }
}
