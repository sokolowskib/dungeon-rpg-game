namespace rpg;

public class Socket : Item
{
    public Item?[] Slots;

    public Socket(string name, char symbol, int slotCount) : base(name, 0, symbol, 0)
    {
        Slots = new Item?[slotCount];
    }

    public override bool HasSlots => true;

    public override void ApplyPassiveEffects(Player player)
    {
        foreach (var child in Slots) child?.ApplyPassiveEffects(player);
    }
    public override void RemovePassiveEffects(Player player)
    {
        foreach (var child in Slots) child?.RemovePassiveEffects(player);
    }

    public override string GetDescription() => $"{Name} (uchwyt, {Slots.Length} slotów)";

    public override bool CanAccept(Item child) => true;
    public override IReadOnlyList<Item?> GetSlots() => Slots;
    public override void SetSlot(int index, Item? child) => Slots[index] = child;
}
