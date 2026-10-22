
using System;
using System.Text;
using rpg;

class Program
{
    const int DefaultPort = 5555;
    const string DefaultHost = "127.0.0.1";

    static void Main(string[] args)
    {
        //   wybor trybu po argumentach
        //   (brak)              -> local
        //   --server [port]     -> serwer na porcie (domyslnie 5555)
        //   --client [ip:port]  -> klient (domyslnie 127.0.0.1:5555)
        if (args.Length == 0)
        {
            RunLocal();
            return;
        }

        switch (args[0])
        {
            case "--server":
            {
                int port = args.Length >= 2 ? int.Parse(args[1]) : DefaultPort;
                RunServer(port);
                return;
            }
            case "--client":
            {
                string host = DefaultHost;
                int port = DefaultPort;
                if (args.Length >= 2)
                {
                    var parts = args[1].Split(':');
                    host = parts[0];
                    if (parts.Length >= 2)
                        port = int.Parse(parts[1]);
                }
                RunClient(host, port);
                return;
            }
            default:
                Console.WriteLine($"Nieznany argument: {args[0]}");
                Console.WriteLine("Uzycie: Game [--server [port] | --client [ip:port]]");
                return;
        }
    }

    static void RunServer(int port)
    {
        new Server(port).Run();
    }

    static void RunClient(string host, int port)
    {
        new NetworkClient(host, port).Run();
    }

    static void RunLocal()
    {
        Console.CursorVisible = false;
        Console.Clear();

        var config = GameConfig.Load();

        EventJournalManager.Initialize(new FileEventJournal());

        string startTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string journalFileName = $"{config.PlayerName}_{startTime}.log";
        string journalFilePath = Path.Combine(config.JournalPath, journalFileName);

        Player player = new Player(100, 10, 10, 10, 10, 10);
        Map map = new Map(40, 20);
        var theme = DungeonThemeFactory.Create(config.DungeonTheme);
        var builder = new DungeonBuilder(map);
        theme.Build(builder);

        var model = new GameModel(map);
        model.AddPlayer(1, player);

        IGameView view = new ConsoleView();

        var controller = new LocalController(view, myPlayerId: 1);

        var introLines = new List<string>();
        introLines.Add(theme.IntroMessage);
        introLines.AddRange(builder.GetIntroLines());
        view.ShowIntro(introLines);

        EventJournalManager.Instance.Log($"Rozpoczeto gre. Gracz: {config.PlayerName}, Motyw: {config.DungeonTheme}");

        while (controller.IsRunning)
        {
            if (player.Health <= 0)
            {
                EventJournalManager.Instance.Log("Gracz zginal!");
                EventJournalManager.Instance.Save(journalFilePath);
                view.RenderGameOver(journalFileName);
                Console.ReadKey(intercept: true);
                break;
            }

            if (!model.Map.GetAllEnemies().Any())
            {
                EventJournalManager.Instance.Log("ZWYCIESTWO. Wszyscy wrogowie pokonani.");
                EventJournalManager.Instance.Save(journalFilePath);
                var finals = model.Players.Select(kv => StateMapper.ToDto(kv.Value, kv.Key)).ToList();
                view.RenderVictory(finals);
                Console.ReadKey(intercept: true);
                break;
            }

            if (Console.KeyAvailable)
            {
                var cki = Console.ReadKey(intercept: true);
                controller.HandleKey(cki.Key, model);
                model.MoveAllEnemies();
            }

            view.Render(StateMapper.ToDto(model), 1);

            System.Threading.Thread.Sleep(16);
        }

        Console.CursorVisible = true;
    }
}
