namespace rpg;

/// <summary>
/// interfejs warstwy view z mvc, pozwala podmienic sposob renderowania
/// </summary>

public interface IGameView
{
    void ShowIntro(List<string> lines);
    void Render(GameStateDto state, int myId);
    void RenderFullJournal(List<string> entries);
    void RenderGameOver(string journalFileName = "");
    void RenderVictory(List<PlayerDto> players);
}
