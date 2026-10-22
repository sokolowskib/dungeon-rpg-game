using System.Text.Json;

namespace rpg;

public class GameConfig
{
    public string PlayerName { get; set; } = "Bohater";
    public string DungeonTheme { get; set; } = "minecraft";
    public string JournalPath { get; set; } = "./";

    // szuka config.json w katalogu roboczym lub katalog wyzej (uruchomienie z Game/)
    public static GameConfig Load()
    {
        foreach (string path in new[] { "config.json", "../config.json" })
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<GameConfig>(File.ReadAllText(path)) ?? new GameConfig();
        }
        return new GameConfig();
    }
}
