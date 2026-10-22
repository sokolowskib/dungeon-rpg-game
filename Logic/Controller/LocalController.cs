namespace rpg;

/// <summary>
/// local controller trzyma w sobie cor wczesniej zbudowany.
///
/// flow:
/// 1.wrzucasz w chain of responsibility console key;
/// 2.cor wypluwa ci icommand
/// 3.comm.execute z MyPlayerId
///
/// </summary>

public class LocalController
{
    private readonly InputHandler _chain;
    private readonly IGameView _view;
    public int MyPlayerId { get; }

    public bool IsRunning { get; private set; } = true;

    public LocalController(IGameView view, int myPlayerId = 1)
    {
        _view = view;
        MyPlayerId = myPlayerId;

        var editor = new SlotEditorHandler();
        editor.SetNext(new CombatHandler())
            .SetNext(new MovementHandler())
            .SetNext(new InventoryHandler())
            .SetNext(new MessageHandler())
            .SetNext(new UnknownKeyHandler());
        _chain = editor;
    }

    public void HandleKey(ConsoleKey key, GameModel model)
    {
        if (key == ConsoleKey.Escape)
        {
            IsRunning = false;
            return;
        }
        if (key == ConsoleKey.J)
        {
            _view.RenderFullJournal(EventJournalManager.Instance.GetAll());
            return;
        }

        ICommand? cmd = _chain.Handle(key, StateMapper.ToDto(model), MyPlayerId);
        cmd?.Execute(model, MyPlayerId);
    }
}
