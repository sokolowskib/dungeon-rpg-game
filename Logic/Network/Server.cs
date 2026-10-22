using System.Net;
using System.Net.Sockets;

namespace rpg;

/// <summary>
/// logika serwera. trzyma jedna instancje gamemodel. architektura thread-per-client
/// flow:
///	1. watek czyta linie json.
///	2. deserializuje do icommand
///	3. wykonuje pod lockiem cmd.execute()
///	4. pod innym lockiem snapshotuje i broadcastuje stan
///
/// osobny watek tickuje wrogow
/// </summary>

public class Server
{
    readonly int _port;
    readonly object _modelLock = new();    // lock na model
    readonly object _clientsLock = new();  // lock na liste klientow
    readonly GameConfig config;
    readonly List<StreamWriter> _clients = new();
    readonly GameModel _model;
    readonly List<string> _introLines;
    int _nextPlayerId = 1;
    const int MaxPlayers = 9;
    const int TickMs = 1000;
    bool _victoryBroadcast = false;

    public Server(int port)
    {
        _port = port;
        config = GameConfig.Load();
        EventJournalManager.Initialize(new FileEventJournal());
        var map = new Map(40, 20);
        var theme = DungeonThemeFactory.Create(config.DungeonTheme);
        var builder = new DungeonBuilder(map);
        theme.Build(builder);
        _model = new GameModel(map);
        _introLines = new List<string> { theme.IntroMessage };
        _introLines.AddRange(builder.GetIntroLines());
    }

    public void Run()
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        Console.WriteLine($"[server] nasluchuje na porcie {_port}");

        var tick = new Thread(TickLoop) { IsBackground = true };  // watek ruszajacy wrogow
        tick.Start();

        while (true)
        {
            var tcp = listener.AcceptTcpClient();
            int playerId;
            lock (_modelLock)
            {
                if (_nextPlayerId > MaxPlayers)  // przypadek gdy max graczy jest podlaczonych
                {
                    Console.WriteLine("[server] odrzucono polaczenie - max graczy");
                    tcp.Close();
                    continue;
                }
                playerId = _nextPlayerId++;
                _model.AddPlayer(playerId,
                                 new Player(100, 10, 10, 10, 10, 10));  // dodajesz do dikta z graczami nowego gracza
            }
            var thread = new Thread(
                () => HandleClient(tcp, playerId)) { IsBackground = true };  // dodajesz osobny watek go obslugujacego
            thread.Start();
        }
    }

    void HandleClient(TcpClient tcp, int playerId)
    {
        Console.WriteLine($"[server] gracz {playerId} polaczony");
        EventJournalManager.Instance.Log($"Podlaczono gracza. Gracz: {playerId}, Motyw: {config.DungeonTheme}");
        using var stream = tcp.GetStream();
        var writer = new StreamWriter(stream) { AutoFlush = true };
        var reader = new StreamReader(stream);

        writer.WriteLine(MessageCodec.EncodeServer(new WelcomeMessage {
            PlayerId = playerId, IntroLines = _introLines
        }));                                       // wiadomosc witajaca go w grze, zawiera intro lines
        lock (_clientsLock) _clients.Add(writer);  // pod lockiem modyfikuje liste potokow klienckich
        BroadcastState();

        try
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var cmd = MessageCodec.Decode(line);
                if (cmd == null)
                    continue;
                bool dead;
                lock (_modelLock)
                {
                    cmd.Execute(_model, playerId);
                    dead = _model.GetPlayer(playerId).Health <= 0;
                }
                BroadcastState();
                CheckVictory();
                if (dead)
                {
                    try
                    {
                        writer.WriteLine(MessageCodec.EncodeServer(new GameOverMessage()));
                    }
                    catch (IOException)
                    {
                    }
                    break;
                }
            }
        }
        catch (IOException)
        { /* klient zerwal */
        }
        finally
        {
            lock (_clientsLock) _clients.Remove(writer);  // obsluzenie usuniecia klienta
            lock (_modelLock) _model.RemovePlayer(playerId);
            BroadcastState();
            Console.WriteLine($"[server] gracz {playerId} rozlaczony");
        }
    }

    void TickLoop()
    {
        while (true)
        {
            Thread.Sleep(TickMs);
            lock (_modelLock) _model.MoveAllEnemies();
            BroadcastState();
        }
    }

    void CheckVictory()
    {
        if (_victoryBroadcast) return;
        VictoryMessage? msg = null;
        lock (_modelLock)
        {
            if (_model.Map.GetAllEnemies().Any()) return;
            if (_model.Players.Count == 0) return;
            _victoryBroadcast = true;
            var finals = _model.Players.Select(kv => StateMapper.ToDto(kv.Value, kv.Key)).ToList();
            msg = new VictoryMessage { FinalPlayers = finals };
            EventJournalManager.Instance.Log($"ZWYCIESTWO. Wszyscy wrogowie pokonani.");
        }
        string json = MessageCodec.EncodeServer(msg);
        lock (_clientsLock)
        {
            foreach (var w in _clients.ToArray())
            {
                try { w.WriteLine(json); }
                catch (IOException) { _clients.Remove(w); }
            }
        }
    }

    void BroadcastState()  // snapshot stanu i wysylanie do kazdego podlaczonego klienta
    {
        GameStateDto dto;
        lock (_modelLock) dto = StateMapper.ToDto(_model);
        string json = MessageCodec.EncodeServer(new StateUpdateMessage { State = dto });

        lock (_clientsLock)
        {
            foreach (var w in _clients.ToArray())
            {
                try
                {
                    w.WriteLine(json);
                }
                catch (IOException)
                {
                    _clients.Remove(w);
                }
            }
        }
    }
}
