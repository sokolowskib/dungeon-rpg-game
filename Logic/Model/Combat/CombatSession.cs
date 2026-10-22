namespace rpg;

/// <summary>
/// ekstra wrapper na combat, trzyma pola zeby gamemodel mogl wiedziec ktore pola sa zablokowane. kazdy gracz trzyma
/// nullable combatsession i po tym sprawdza sie czy jest w walce czy nie
/// </summary>

public class CombatSession
{
    public Combat Combat { get; }
    public int X { get; }
    public int Y { get; }

    public CombatSession(Combat combat, int x, int y)
    {
        Combat = combat;
        X = x;
        Y = y;
    }
}
