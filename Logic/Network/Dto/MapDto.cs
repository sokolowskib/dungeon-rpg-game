namespace rpg;

public class MapDto
{
    public int Width { get; set; }
    public int Height { get; set; }
    public char[][] Tiles { get; set; } = [];

    public List<string> Instructions { get; set; } = new();
    public List<EnemyDto> Enemies { get; set; } = new();
    public List<ItemPileDto> ItemsOnGround { get; set; } = new();
}
