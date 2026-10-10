using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GigaDock.Infrastructure.Linux;

/// <summary>obs-websocket v5 somente em loopback. Senha apenas no escopo da chamada, sem persistência/log.</summary>
public static class ObsLocalClient
{
    public static string Authentication(string password, string salt, string challenge)
    {
        var secret = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret + challenge)));
    }

    public static async Task<string> RequestAsync(int port, string password, string requestType, CancellationToken token = default)
    {
        if (port is < 1 or > 65535 || requestType is not ("GetRecordStatus" or "GetStreamStatus" or "StartRecord" or "StopRecord")) throw new ArgumentException("Escolha uma porta e um comando OBS válidos.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token); timeout.CancelAfter(TimeSpan.FromSeconds(10));
        using var socket = new ClientWebSocket();
        try
        {
            await socket.ConnectAsync(new Uri($"ws://127.0.0.1:{port}"), timeout.Token);
            using var hello = await ReceiveAsync(socket, timeout.Token);
            if (hello.RootElement.GetProperty("op").GetInt32() != 0) throw new IOException("Resposta OBS inesperada.");
            var data = hello.RootElement.GetProperty("d");
            string? authentication = null;
            if (data.TryGetProperty("authentication", out var auth)) authentication = Authentication(password, auth.GetProperty("salt").GetString()!, auth.GetProperty("challenge").GetString()!);
            await SendAsync(socket, new { op = 1, d = new { rpcVersion = 1, authentication, eventSubscriptions = 0 } }, timeout.Token);
            using var identified = await ReceiveAsync(socket, timeout.Token);
            if (identified.RootElement.GetProperty("op").GetInt32() != 2) throw new IOException("O OBS não autenticou a conexão.");
            var id = Guid.NewGuid().ToString("N");
            await SendAsync(socket, new { op = 6, d = new { requestType, requestId = id } }, timeout.Token);
            for (var i = 0; i < 10; i++)
            {
                using var response = await ReceiveAsync(socket, timeout.Token);
                if (response.RootElement.GetProperty("op").GetInt32() != 7) continue;
                var result = response.RootElement.GetProperty("d");
                if (result.GetProperty("requestId").GetString() != id) continue;
                if (!result.GetProperty("requestStatus").GetProperty("result").GetBoolean()) throw new IOException("O OBS recusou o comando. Confira o estado de gravação.");
                if (requestType is "StartRecord" or "StopRecord") return requestType == "StartRecord" ? "Gravação iniciada." : "Gravação encerrada.";
                return result.GetProperty("responseData").GetProperty("outputActive").GetBoolean() ? "Ativo no OBS." : "Inativo no OBS.";
            }
            throw new IOException("O OBS não respondeu ao comando.");
        }
        catch (Exception error) when (error is WebSocketException or OperationCanceledException or JsonException)
        { throw new IOException("Não foi possível conectar ao OBS local. Confira a porta, a senha e se o servidor WebSocket está ativado."); }
    }

    private static async Task SendAsync(ClientWebSocket socket, object message, CancellationToken token) =>
        await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(message).AsMemory(), WebSocketMessageType.Text, true, token);

    private static async Task<JsonDocument> ReceiveAsync(ClientWebSocket socket, CancellationToken token)
    {
        var buffer = new byte[8192]; using var message = new MemoryStream();
        ValueWebSocketReceiveResult part;
        do
        {
            part = await socket.ReceiveAsync(buffer.AsMemory(), token);
            if (part.MessageType != WebSocketMessageType.Text || message.Length + part.Count > 1024 * 1024) throw new IOException("Mensagem OBS inválida.");
            message.Write(buffer, 0, part.Count);
        } while (!part.EndOfMessage);
        return JsonDocument.Parse(message.ToArray());
    }
}
