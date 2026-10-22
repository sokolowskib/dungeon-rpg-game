namespace rpg;

/// <summary>
/// klasa dto do combatu, potrzebne wiecej pol zeby gui walki bylo w pelni funkcjonalne
/// </summary>

public class CombatDto
{
    public string EnemyName { get; set; } = "";
    public int EnemyHp { get; set; }
    public int EnemyDamage { get; set; }
    public int EnemyArmor { get; set; }
    public List<string> FightLog { get; set; } = new();
}
