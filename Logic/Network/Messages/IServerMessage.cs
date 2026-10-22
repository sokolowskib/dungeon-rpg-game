using System.Text.Json.Serialization;

namespace rpg;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(WelcomeMessage), "welcome")]
[JsonDerivedType(typeof(StateUpdateMessage), "state_update")]
[JsonDerivedType(typeof(GameOverMessage), "game_over")]
[JsonDerivedType(typeof(VictoryMessage), "victory")]

public interface IServerMessage
{
    void Apply(Client client);
}

// Pierwsza wiadomosc po polaczeniu, serwer mowi klientowi jakie ID dostal.
public class WelcomeMessage : IServerMessage
{
    public int PlayerId { get; set; }
    public List<string> IntroLines {
        get; set;
    } = new();
    public void Apply(Client c)
    {
        c.MyId = PlayerId;
        c.IntroLines = IntroLines;
    }
}

// snapshot stanu gry
public class StateUpdateMessage : IServerMessage
{
    public GameStateDto State { get; set; } = new();

    public void Apply(Client c) => c.LastState = State;
}

// koniec gry, zamykanie socketa po stronie klienta
public class GameOverMessage : IServerMessage
{
    public void Apply(Client c) => c.IsGameOver = true;
}

// wszyscy wrogowie martwi, rozstrzygniecie po levelach
public class VictoryMessage : IServerMessage
{
    public List<PlayerDto> FinalPlayers { get; set; } = new();
    public void Apply(Client c) { c.IsVictory = true; c.FinalPlayers = FinalPlayers; }
}
