using System.Text.Json;

namespace rpg;

/// <summary>
/// klasa serializuje i deserializuje wiadomosci serwerowe oraz komendy
/// </summary>

public static class MessageCodec
{
    static readonly JsonSerializerOptions Opts = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Encode(ICommand cmd) => JsonSerializer.Serialize<ICommand>(cmd, Opts);

    public static ICommand? Decode(string json) => JsonSerializer.Deserialize<ICommand>(json, Opts);

    public static string EncodeServer(IServerMessage msg) => JsonSerializer.Serialize<IServerMessage>(msg, Opts);

    public static IServerMessage? DecodeServer(string json) => JsonSerializer.Deserialize<IServerMessage>(json, Opts);
}
