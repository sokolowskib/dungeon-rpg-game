using System.Net.Sockets;

namespace rpg;

/// <summary>
/// obsluga klienta tcp.
/// Tworza sie dwa watki:
/// 1.czyta w tle potok, modyfikuje stan kliencki tak, zeby drukowal sie obecny stan mapy
/// 2.czyta z klawiatury, chain obsluguje input, koduje do json i wysyla przez siec
/// </summary>

public class NetworkClient
{
    readonly string _host;
    readonly int _port;
    readonly Client _state = new();
    readonly IGameView _view = new ConsoleView();
    readonly NetworkController _controller;
    readonly object _renderLock = new();

    public NetworkClient(string host, int port)
    {
        _host = host;
        _port = port;
        _controller = new NetworkController(_view, _state);
    }

    public void Run()
    {
        Console.CursorVisible = false;
        Console.Clear();
        using var tcp = new TcpClient();
        tcp.Connect(_host, _port);

        var stream = tcp.GetStream();
        var reader = new StreamReader(stream);
        var writer = new StreamWriter(stream) { AutoFlush = true };

        // synchronicznie czytam intro
        var firstLine = reader.ReadLine();
        if (firstLine != null)
        {
            var first = MessageCodec.DecodeServer(firstLine);
            first?.Apply(_state);
            if (_state.IntroLines.Count > 0)
                _view.ShowIntro(_state.IntroLines);
        }

        var readerThread =
            new Thread(() => ReadLoop(reader)) { IsBackground = true };  // odpalenie watku odbierajacego stan z serwera
        readerThread.Start();

        while (_controller.IsRunning)  // petla zczytywania z klawiatury
        {
            var key = Console.ReadKey(intercept: true).Key;
            if (_state.IsVictory || _state.IsGameOver)
                break;
            ICommand? cmd;
            lock (_renderLock) cmd = _controller.HandleKey(key);
            if (cmd == null)
                continue;
            try
            {
                writer.WriteLine(MessageCodec.Encode(cmd));
            }
            catch (IOException)
            {
                break;
            }
        }
    }

    void ReadLoop(StreamReader reader)  // czyta potok i zmienia stan
    {
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var msg = MessageCodec.DecodeServer(line);
            if (msg == null)
                continue;
            msg.Apply(_state);
            lock (_renderLock)
            {
                if (_state.IsVictory)
                    _view.RenderVictory(_state.FinalPlayers);
                else if (_state.IsGameOver)
                    _view.RenderGameOver();
                else if (_state.LastState != null)
                    _view.Render(_state.LastState, _state.MyId);
            }
        }
    }
}
