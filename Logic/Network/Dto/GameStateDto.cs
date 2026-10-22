namespace rpg;

public class GameStateDto
{
    public MapDto Map { get; set; } = new();
    public List<PlayerDto> Players { get; set; } = new();
    public List<string> Journal { get; set; } = new();
}
