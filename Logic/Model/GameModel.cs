namespace rpg;

/// <summary>
/// klasa reprezentujaca obecny stan gry. Jeden Map dzielony przez wszystkich
/// graczy, gracze trzymani w slowniku po id (1..9 dla multi, 1 dla lokala).
/// Walka jest per-player: kazdy ma swoje ActiveCombat (CombatSession?) z
/// wlasnym polem (X,Y). Inni gracze nie moga wejsc na pole zajete walka.
/// </summary>

public class GameModel
{
    public Map Map { get; }
    public Dictionary<int, Player> Players { get; } = new();

    public GameModel(Map map)
    {
        Map = map;
    }

    public void AddPlayer(int id, Player player)
    {
        Players[id] = player;
    }

    public Player GetPlayer(int id) => Players[id];

    public void RemovePlayer(int id) => Players.Remove(id);

    // sprawdzenie czy na danym polu jest juz walka
    public bool IsTileInCombat(int x, int y)
    {
        foreach (var p in Players.Values)
        {
            var s = p.ActiveCombat;
            if (s != null && s.X == x && s.Y == y)
                return true;
        }
        return false;
    }

    // pola na ktorych trwa walka
    public HashSet<(int, int)> GetCombatTiles()
    {
        var set = new HashSet<(int, int)>();
        foreach (var p in Players.Values)
        {
            var s = p.ActiveCombat;
            if (s != null)
                set.Add((s.X, s.Y));
        }
        return set;
    }

    // Wrogowie chodza zawsze - tylko wrog uwieziony w aktywnej walce stoi
    public void MoveAllEnemies()
    {
        Map.MoveAllEnemies(Players.Values, GetCombatTiles());
    }
}
