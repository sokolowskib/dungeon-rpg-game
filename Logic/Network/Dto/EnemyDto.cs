namespace rpg;

/// <summary>
/// klasa do wysylania, opisujaca wroga
/// </summary>

public class EnemyDto
{
    public string Name { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Health { get; set; }
    public int Damage { get; set; }
    public int Armor { get; set; }
    public char Symbol { get; set; }
}
