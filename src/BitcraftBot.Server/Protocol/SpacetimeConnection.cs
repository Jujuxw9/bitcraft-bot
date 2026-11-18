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

    // Détails techniques pour le journal (niveau « Détails ») : jamais le jeton.
    public event Action<string>? Diagnostic;

    private DateTime _openedAt;
    private long _bytesReceived;
    private int _textMessages;
    private int _binaryMessages;

    public async Task ConnectAsync(Uri uri, string token, CancellationToken cancel)
    {
        _socket.Options.AddSubProtocol(ServerEndpoints.JsonProtocol);
        _socket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
        _socket.Options.SetRequestHeader("User-Agent", ServerEndpoints.UserAgent);
        _socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
        _socket.Options.CollectHttpResponseDetails = true;
        Diagnostic?.Invoke($"Ouverture de {uri} (sous-protocole demandé : {ServerEndpoints.JsonProtocol}, jeton de {token.Length} caractères).");
        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await _socket.ConnectAsync(uri, cancel);
        }
        catch (WebSocketException ex)
        {
            Diagnostic?.Invoke($"Poignée de main refusée après {watch.ElapsedMilliseconds} ms : HTTP {(int)_socket.HttpStatusCode} ({_socket.HttpStatusCode}), " +
                               $"code {ex.WebSocketErrorCode}, {ex.Message}{Inner(ex)}. En-têtes : {Headers()}.");
            throw;
        }
        _openedAt = DateTime.Now;
        Diagnostic?.Invoke($"Poignée de main acceptée en {watch.ElapsedMilliseconds} ms : HTTP {(int)_socket.HttpStatusCode}, " +
                           $"sous-protocole accepté « {_socket.SubProtocol ?? "aucun"} ». En-têtes : {Headers()}.");
        _receiveCts = new CancellationTokenSource();
        _receiveTask = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token));
    }

    // En-têtes de la réponse du serveur, sans ceux qui contiennent un jeton.
    private string Headers()
    {
        var headers = _socket.HttpResponseHeaders;
        if (headers is null || headers.Count == 0)
        {
            return "aucun";
        }
        return string.Join(" ; ", headers
            .Where(h => !h.Key.Contains("token", StringComparison.OrdinalIgnoreCase) && !h.Key.Contains("cookie", StringComparison.OrdinalIgnoreCase))
            .Select(h => $"{h.Key}={string.Join(",", h.Value)}"));
    }

    private static string Inner(Exception ex) => ex.InnerException is { } inner ? $" (cause : {inner.GetType().Name} : {inner.Message})" : "";

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
                _bytesReceived += result.Count;
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    reason = $"fermée par le serveur (code {(int?)result.CloseStatus} {result.CloseStatus}, « {result.CloseStatusDescription} »)";
                    break;
                }
                message.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage)
                {
                    continue;
                }
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    _textMessages++;
                    MessageReceived?.Invoke(Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length));
                }
                else
                {
                    // Le bot demande du JSON : un message binaire voudrait dire que le serveur a choisi un autre format.
                    _binaryMessages++;
                    Diagnostic?.Invoke($"Message binaire inattendu ({message.Length} octets), ignoré.");
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
            Diagnostic?.Invoke($"Erreur de liaison : code {ex.WebSocketErrorCode}, état {_socket.State}{Inner(ex)}.");
        }
        Diagnostic?.Invoke($"Fin de liaison après {(DateTime.Now - _openedAt).TotalMilliseconds:0} ms : {_textMessages} messages texte, " +
                           $"{_binaryMessages} binaires, {_bytesReceived} octets reçus, statut de fermeture {(int?)_socket.CloseStatus} « {_socket.CloseStatusDescription} ».");
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
