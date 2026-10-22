namespace rpg;

/// <summary>
/// chain of responsibility pod spodem, przyjmuje console key i oddaje Command implementujaca icommand
/// Klient wysyla zwrocony cmd na serwer;
/// local wola cmd.Execute(model, myId) bezposrednio.
/// </summary>

public abstract class InputHandler
{
    private InputHandler? _next;

    public InputHandler SetNext(InputHandler next)
    {
        _next = next;
        return next;
    }

    public virtual ICommand? Handle(ConsoleKey key, GameStateDto state, int myId)
    {
        return _next?.Handle(key, state, myId);
    }
}

public class SlotEditorHandler : InputHandler
{
    public override ICommand? Handle(ConsoleKey key, GameStateDto state, int myId)
    {
        var me = state.Players.FirstOrDefault(p => p.ID == myId);
        if (me == null)
            return base.Handle(key, state, myId);

        if (me.SlotEditor != null)
        {
            return key switch {
                ConsoleKey.T          => new ToggleSlotEditorCommand(),
                ConsoleKey.UpArrow    => new MoveSlotEditorCursorCommand(-1),
                ConsoleKey.DownArrow  => new MoveSlotEditorCursorCommand(+1),
                ConsoleKey.Enter      => new EnterFocusedItemCommand(),
                ConsoleKey.LeftArrow  => new LeaveFocusedItemCommand(),
                ConsoleKey.Backspace  => new RemoveFromSlotCommand(),
                ConsoleKey.D1         => new InsertToSlotCommand(0),
                ConsoleKey.D2         => new InsertToSlotCommand(1),
                ConsoleKey.D3         => new InsertToSlotCommand(2),
                ConsoleKey.D4         => new InsertToSlotCommand(3),
                ConsoleKey.D5         => new InsertToSlotCommand(4),
                ConsoleKey.P          => new DismissMessageCommand(),
                _                     => null,
            };
        }

        if (key == ConsoleKey.T && me.Combat == null)
            return new ToggleSlotEditorCommand();

        return base.Handle(key, state, myId);
    }
}

public class CombatHandler : InputHandler
{
    public override ICommand? Handle(ConsoleKey key, GameStateDto state, int myId)
    {
        var me = state.Players.FirstOrDefault(p => p.ID == myId);
        if (me == null)
            return base.Handle(key, state, myId);

        if (me.Combat != null)
        {
            return key switch {
                ConsoleKey.D1 => new CombatTurnCommand(1),
                ConsoleKey.D2 => new CombatTurnCommand(2),
                ConsoleKey.D3 => new CombatTurnCommand(3),
                ConsoleKey.Q  => new FleeCommand(),
                _ => null,
            };
        }

        if (key == ConsoleKey.F && state.Map.Enemies.Any(e => e.X == me.X && e.Y == me.Y))
            return new AttackCommand();

        return base.Handle(key, state, myId);
    }
}

public class MovementHandler : InputHandler
{
    public override ICommand? Handle(ConsoleKey key, GameStateDto state, int myId)
    {
        return key switch {
            ConsoleKey.W => new MoveCommand("up"),   ConsoleKey.S => new MoveCommand("down"),
            ConsoleKey.A => new MoveCommand("left"), ConsoleKey.D => new MoveCommand("right"),
            _ => base.Handle(key, state, myId),
        };
    }
}

public class InventoryHandler : InputHandler
{
    public override ICommand? Handle(ConsoleKey key, GameStateDto state, int myId)
    {
        return key switch {
            ConsoleKey.E => new PickUpCommand(),       ConsoleKey.Q => new DropCommand(),
            ConsoleKey.G => new SwapHandCommand(),     ConsoleKey.Tab => new EquipCommand(),
            ConsoleKey.D1 => new SelectSlotCommand(0), ConsoleKey.D2 => new SelectSlotCommand(1),
            ConsoleKey.D3 => new SelectSlotCommand(2), ConsoleKey.D4 => new SelectSlotCommand(3),
            ConsoleKey.D5 => new SelectSlotCommand(4),
            _ => base.Handle(key, state, myId),
        };
    }
}

public class MessageHandler : InputHandler
{
    public override ICommand? Handle(ConsoleKey key, GameStateDto state, int myId)
    {
        if (key == ConsoleKey.P)
            return new DismissMessageCommand();
        return base.Handle(key, state, myId);
    }
}

public class UnknownKeyHandler : InputHandler
{
    public override ICommand? Handle(ConsoleKey key, GameStateDto state, int myId)
    {
        return new UnknownKeyCommand(key.ToString());
    }
}
