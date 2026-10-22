namespace rpg;

/// <summary>
/// klient trzyma to po swojej stronie, potrzebne do renderowania stanu gry
/// </summary>

public class Client
{
    public int MyId { get; set; }
    public GameStateDto? LastState { get; set; }
    public bool IsGameOver { get; set; }
    public bool IsVictory { get; set; }
    public List<PlayerDto> FinalPlayers { get; set; } = new();
    public List<string> IntroLines { get; set; } = new();
}
