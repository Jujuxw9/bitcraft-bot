using System.Net.WebSockets;
using System.Text;

namespace BitcraftBot.Server.Protocol;

// Liaison WebSocket chiffrée avec un module SpacetimeDB.
// LECTURE SEULE (D-039) : cette classe sait seulement s'abonner à des tables. Elle n'a aucun moyen
// d'appeler un « reducer », donc aucune action ne peut partir vers le serveur.
public sealed class SpacetimeConnection : IAsyncDisposable
{
    private readonly ClientWebSocket _socket = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;

    public event Action<string>? MessageReceived;

    // Fin de liaison : raison lisible (fermée par nous, par le serveur, erreur réseau).
    public event Action<string>? Closed;

    public WebSocketState State => _socket.State;

    public async Task ConnectAsync(Uri uri, string token, CancellationToken cancel)
    {
        _socket.Options.AddSubProtocol(ServerEndpoints.JsonProtocol);
        _socket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
        _socket.Options.SetRequestHeader("User-Agent", ServerEndpoints.UserAgent);
        _socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
        await _socket.ConnectAsync(uri, cancel);
        _receiveCts = new CancellationTokenSource();
        _receiveTask = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token));
    }

    // Seul message envoyé par le bot : la liste des tables à suivre (requêtes SQL de lecture).
    public async Task SubscribeAsync(IEnumerable<string> queries, uint requestId, CancellationToken cancel)
    {
        var bytes = Encoding.UTF8.GetBytes(ServerMessages.Subscribe(queries, requestId));
        await _sendLock.WaitAsync(cancel);
        try
        {
            await _socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancel);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancel)
    {
        var buffer = new byte[64 * 1024];
        var message = new MemoryStream();
        var reason = "liaison fermée";
        try
        {
            while (!cancel.IsCancellationRequested && _socket.State == WebSocketState.Open)
            {
                var result = await _socket.ReceiveAsync(buffer, cancel);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    reason = $"fermée par le serveur ({result.CloseStatus} {result.CloseStatusDescription})".Trim();
                    break;
                }
                message.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage)
                {
                    continue;
                }
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    MessageReceived?.Invoke(Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length));
                }
                message.SetLength(0);
            }
            if (cancel.IsCancellationRequested)
            {
                reason = "fermée par le bot";
            }
        }
        catch (OperationCanceledException)
        {
            reason = "fermée par le bot";
        }
        catch (WebSocketException ex)
        {
            reason = $"erreur réseau ({ex.Message})";
        }
        Closed?.Invoke(reason);
    }

    public async Task CloseAsync()
    {
        try
        {
            if (_socket.State == WebSocketState.Open)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "fin", timeout.Token);
            }
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException)
        {
            // Fermeture au mieux : la liaison est de toute façon abandonnée.
        }
        _receiveCts?.Cancel();
        if (_receiveTask is not null)
        {
            await Task.WhenAny(_receiveTask, Task.Delay(2000));
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        _socket.Dispose();
        _receiveCts?.Dispose();
        _sendLock.Dispose();
    }
}
