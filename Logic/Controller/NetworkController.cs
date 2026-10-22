namespace rpg;

/// <summary>
/// network controller dziala na ostatnim stanie a nie bezposrednio na modelu
/// </summary>

public class NetworkController
{
    readonly InputHandler _chain;
    readonly IGameView _view;
    readonly Client _state;

    public bool IsRunning { get; private set; } = true;

    public NetworkController(IGameView view, Client state)
    {
        _view = view;
        _state = state;

        var editor = new SlotEditorHandler();
        editor.SetNext(new CombatHandler())
            .SetNext(new MovementHandler())
            .SetNext(new InventoryHandler())
            .SetNext(new MessageHandler())
            .SetNext(new UnknownKeyHandler());
        _chain = editor;
    }

    public ICommand? HandleKey(ConsoleKey key)
    {
        if (key == ConsoleKey.Escape)
        {
            IsRunning = false;
            return null;
        }
        if (key == ConsoleKey.J)
        {
            _view.RenderFullJournal(_state.LastState?.Journal ?? new List<string>());
            if (_state.LastState != null)
                _view.Render(_state.LastState, _state.MyId);
            return null;
        }
        if (_state.LastState == null)
            return null;
        return _chain.Handle(key, _state.LastState, _state.MyId);
    }
}
