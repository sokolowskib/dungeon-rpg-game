using System.Text.Json.Serialization;

namespace rpg;

/// <summary>
/// Interfejs sluzacy do zmiany stanu modelu. Komenda jest zawsze wykonywana
/// w imieniu konkretnego gracza (playerId).
/// Polimorfizm w JSON-ie zalatwiaja atrybuty, dorzuca pole "type"
/// przy serializacji i sam wybiera klase przy deserializacji
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MoveCommand), "move")]
[JsonDerivedType(typeof(AttackCommand), "attack")]
[JsonDerivedType(typeof(CombatTurnCommand), "combat_turn")]
[JsonDerivedType(typeof(PickUpCommand), "pickup")]
[JsonDerivedType(typeof(DropCommand), "drop")]
[JsonDerivedType(typeof(EquipCommand), "equip")]
[JsonDerivedType(typeof(SwapHandCommand), "swap_hand")]
[JsonDerivedType(typeof(SelectSlotCommand), "select_slot")]
[JsonDerivedType(typeof(DismissMessageCommand), "dismiss_message")]
[JsonDerivedType(typeof(UnknownKeyCommand), "unknown_key")]
[JsonDerivedType(typeof(ToggleSlotEditorCommand), "toggle_slot_editor")]
[JsonDerivedType(typeof(MoveSlotEditorCursorCommand), "move_slot_editor_cursor")]
[JsonDerivedType(typeof(EnterFocusedItemCommand), "enter_focused_item")]
[JsonDerivedType(typeof(LeaveFocusedItemCommand), "leave_focused_item")]
[JsonDerivedType(typeof(RemoveFromSlotCommand), "remove_from_slot")]
[JsonDerivedType(typeof(InsertToSlotCommand), "insert_to_slot")]
[JsonDerivedType(typeof(FleeCommand), "flee")]
public interface ICommand
{
    void Execute(GameModel model, int playerId);
}

public sealed class MoveCommand : ICommand
{
    public string Direction { get; set; } = "";
    public MoveCommand() {}
    public MoveCommand(string direction)
    {
        Direction = direction;
    }

    public void Execute(GameModel model, int playerId)
    {
        var p = model.GetPlayer(playerId);
        int x = p.GetPositionX(), y = p.GetPositionY();
        switch (Direction)
        {
            case "up":
                y--;
                break;
            case "down":
                y++;
                break;
            case "left":
                x--;
                break;
            case "right":
                x++;
                break;
        }
        p.SetPosition(x, y, model);
    }
}

public sealed class AttackCommand : ICommand
{
    public void Execute(GameModel model, int playerId)
    {
        var p = model.GetPlayer(playerId);
        int x = p.GetPositionX(), y = p.GetPositionY();
        var enemy = model.Map.GetEnemyAt(x, y);
        if (enemy == null)
            return;
        enemy.combat.BeginCombat(p, model.Map);
        p.ActiveCombat = new CombatSession(enemy.combat, x, y);
    }
}

public sealed class CombatTurnCommand : ICommand
{
    public int Choice { get; set; }  // 1 | 2 | 3
    public CombatTurnCommand() {}
    public CombatTurnCommand(int choice)
    {
        Choice = choice;
    }

    public void Execute(GameModel model, int playerId)
    {
        var p = model.GetPlayer(playerId);
        var session = p.ActiveCombat;
        if (session == null)
            return;
        var combat = session.Combat;
        combat.ExecuteTurn(Choice);
        if (combat._enemy.Health <= 0)
        {
            p.AddMessage($"Pokonałeś {combat._enemy.Name}!");
            model.Map.RemoveEnemy(session.X, session.Y);
            p.ActiveCombat = null;
            p.RegisterCombatWin(playerId);
            return;
        }
        if (p.Health <= 0)
        {
            EventJournalManager.Instance.Log($"Gracz {playerId} zginal w walce z {combat._enemy.Name}");
            combat._enemy.Species.ApplyAttackTransformation(combat._enemy);
            p.ActiveCombat = null;
            // Usuniecie z modelu robi serwer po wyslaniu GameOverMessage.
        }
    }
}

public sealed class FleeCommand : ICommand
{
    public void Execute(GameModel model, int playerId)
    {
        var p = model.GetPlayer(playerId);
        var session = p.ActiveCombat;
        if (session == null) return;
        var enemy = session.Combat._enemy;
        enemy.Species.ApplyAttackTransformation(enemy);
        p.ActiveCombat = null;
        p.AddMessage($"Uciekasz przed {enemy.Name}.");
        EventJournalManager.Instance.Log($"Gracz {playerId} uciekl z walki z {enemy.Name}");
    }
}

public sealed class PickUpCommand : ICommand
{
    public void Execute(GameModel model, int playerId) => model.GetPlayer(playerId).TryPickUp(model.Map);
}

public sealed class DropCommand : ICommand
{
    public void Execute(GameModel model, int playerId) => model.GetPlayer(playerId).DropItemFromHand(model.Map);
}

public sealed class EquipCommand : ICommand
{
    public void Execute(GameModel model, int playerId) => model.GetPlayer(playerId).Equip(model.Map);
}

public sealed class SwapHandCommand : ICommand
{
    public void Execute(GameModel model, int playerId) => model.GetPlayer(playerId).SwapSelectedHand();
}

public sealed class SelectSlotCommand : ICommand
{
    public int Slot { get; set; }  // 0..4
    public SelectSlotCommand() {}
    public SelectSlotCommand(int slot)
    {
        Slot = slot;
    }
    public void Execute(GameModel model, int playerId) => model.GetPlayer(playerId).ChangeSlot(Slot);
}

public sealed class DismissMessageCommand : ICommand
{
    public void Execute(GameModel model, int playerId) => model.GetPlayer(playerId).RemoveOldestMessage();
}

public sealed class ToggleSlotEditorCommand : ICommand
{
    public void Execute(GameModel model, int playerId)
    {
        var p = model.GetPlayer(playerId);
        if (p.ActiveCombat != null)
            return;
        if (p.SlotEditor == null)
            p.SlotEditor = new SlotEditorSession { InventoryCursor = p.SelectedSlot };
        else
            p.SlotEditor = null;
    }
}

public sealed class MoveSlotEditorCursorCommand : ICommand
{
    public int Delta { get; set; }
    public MoveSlotEditorCursorCommand() {}
    public MoveSlotEditorCursorCommand(int delta) { Delta = delta; }

    public void Execute(GameModel model, int playerId)
    {
        var p = model.GetPlayer(playerId);
        var ed = p.SlotEditor;
        if (ed == null) return;

        if (ed.FocusedInventoryIndex == null)
        {
            int max = p.Inventory.Length - 1;
            ed.InventoryCursor = Math.Clamp(ed.InventoryCursor + Delta, 0, max);
            return;
        }

        if (ed.SlotPath == null) return;
        var focused = p.Inventory[ed.FocusedInventoryIndex.Value];
        if (focused == null) return;

        var paths = new List<List<int>>();
        SlotOps.FlatPaths(focused.GetSlots(), new List<int>(), paths);
        if (paths.Count == 0) return;

        int idx = paths.FindIndex(path => path.SequenceEqual(ed.SlotPath));
        if (idx < 0) idx = 0;
        idx = Math.Clamp(idx + Delta, 0, paths.Count - 1);
        ed.SlotPath = paths[idx];
    }
}

public sealed class EnterFocusedItemCommand : ICommand
{
    public void Execute(GameModel model, int playerId)
    {
        var p = model.GetPlayer(playerId);
        var ed = p.SlotEditor;
        if (ed == null || ed.FocusedInventoryIndex != null)
            return;

        var item = p.Inventory[ed.InventoryCursor];
        if (item == null)
        {
            p.AddMessage("Slot jest pusty.");
            return;
        }
        if (!item.HasSlots)
        {
            p.AddMessage($"{item.Name} nie ma slotow.");
            return;
        }
        ed.FocusedInventoryIndex = ed.InventoryCursor;
        ed.MaxRenderedHeight = Math.Max(ed.MaxRenderedHeight, item.CountDisplayLines());
        ed.SlotPath = item.GetSlots().Count > 0 ? new List<int> { 0 } : null;
    }
}

public sealed class LeaveFocusedItemCommand : ICommand
{
    public void Execute(GameModel model, int playerId)
    {
        var p = model.GetPlayer(playerId);
        var ed = p.SlotEditor;
        if (ed == null)
            return;
        ed.FocusedInventoryIndex = null;
        ed.SlotPath = null;
    }
}

public sealed class RemoveFromSlotCommand : ICommand
{
    public void Execute(GameModel model, int playerId)
    {
        var p = model.GetPlayer(playerId);
        var ed = p.SlotEditor;
        if (ed == null || ed.FocusedInventoryIndex == null || ed.SlotPath == null || ed.SlotPath.Count == 0)
            return;
        var focused = p.Inventory[ed.FocusedInventoryIndex.Value];
        if (focused == null) return;

        Item parent = focused;
        for (int i = 0; i < ed.SlotPath.Count - 1; i++)
            parent = parent.GetSlots()[ed.SlotPath[i]]!;
        int slotIdx = ed.SlotPath[^1];

        var child = parent.GetSlots()[slotIdx];
        if (child == null)
        {
            p.AddMessage("Slot pusty.");
            return;
        }
        int freeInv = Array.FindIndex(p.Inventory, s => s == null);
        if (freeInv < 0)
        {
            p.AddMessage("Ekwipunek pelny.");
            return;
        }
        parent.SetSlot(slotIdx, null);
        p.Inventory[freeInv] = child;
        EventJournalManager.Instance.Log($"Wyjeto {child.Name} ze slotu {string.Join('.', ed.SlotPath)}");
    }
}

public sealed class InsertToSlotCommand : ICommand
{
    public int InventoryIndex { get; set; }
    public InsertToSlotCommand() {}
    public InsertToSlotCommand(int inventoryIndex) { InventoryIndex = inventoryIndex; }

    public void Execute(GameModel model, int playerId)
    {
        var p = model.GetPlayer(playerId);
        var ed = p.SlotEditor;
        if (ed == null || ed.FocusedInventoryIndex == null || ed.SlotPath == null || ed.SlotPath.Count == 0)
            return;
        if (InventoryIndex < 0 || InventoryIndex >= p.Inventory.Length) return;
        if (InventoryIndex == ed.FocusedInventoryIndex.Value)
        {
            p.AddMessage("Nie mozna wlozyc itemu w samego siebie.");
            return;
        }
        var source = p.Inventory[InventoryIndex];
        if (source == null)
        {
            p.AddMessage("Wybrany slot ekwipunku jest pusty.");
            return;
        }
        var focused = p.Inventory[ed.FocusedInventoryIndex.Value];
        if (focused == null) return;

        Item parent = focused;
        for (int i = 0; i < ed.SlotPath.Count - 1; i++)
            parent = parent.GetSlots()[ed.SlotPath[i]]!;
        int slotIdx = ed.SlotPath[^1];

        if (!parent.CanAccept(source))
        {
            p.AddMessage($"{source.Name} nie pasuje do tego slotu.");
            return;
        }
        var displaced = parent.GetSlots()[slotIdx];
        p.Inventory[InventoryIndex] = null;
        parent.SetSlot(slotIdx, source);
        if (displaced != null)
        {
            int freeInv = Array.FindIndex(p.Inventory, s => s == null);
            if (freeInv < 0)
            {
                parent.SetSlot(slotIdx, displaced);
                p.Inventory[InventoryIndex] = source;
                p.AddMessage("Brak miejsca na wypchniety item.");
                return;
            }
            p.Inventory[freeInv] = displaced;
        }
        ed.MaxRenderedHeight = Math.Max(ed.MaxRenderedHeight, focused.CountDisplayLines());
        EventJournalManager.Instance.Log($"Wlozono {source.Name} w slot {string.Join('.', ed.SlotPath)}");
    }
}

public static class SlotOps
{
    public static void FlatPaths(IReadOnlyList<Item?> slots, List<int> prefix, List<List<int>> result)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var path = new List<int>(prefix) { i };
            result.Add(path);
            var child = slots[i];
            if (child != null && child.HasSlots)
                FlatPaths(child.GetSlots(), path, result);
        }
    }
}

public sealed class UnknownKeyCommand : ICommand
{
    public string KeyName { get; set; } = "";
    public UnknownKeyCommand() {}
    public UnknownKeyCommand(string keyName)
    {
        KeyName = keyName;
    }

    public void Execute(GameModel model, int playerId)
    {
        model.GetPlayer(playerId).AddMessage($"Nieznany klawisz: {KeyName}");
        EventJournalManager.Instance.Log($"Wcisnieto nieznany przycisk: {KeyName}");
    }
}
