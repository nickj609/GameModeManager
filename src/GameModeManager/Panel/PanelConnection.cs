// Included libraries
using System.Text;
using System.Text.Json;
using System.Net.WebSockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.Panel
{
    // Define class
    // The link to the web panel: an outgoing WebSocket, so the game host needs no open port. It runs on a background task and
    // never touches CounterStrikeSharp: commands go to the game thread through onCommand (PanelAgent hands them to
    // Server.NextFrame), and what goes back are finished JSON strings, queued by Send from any thread.
    // It reconnects on its own (1 s, doubling to 60 s, with jitter) and logs only when the link goes up or down, not every retry.
    public sealed class PanelConnection
    {
        // Define class properties
        private const int MaxMessageBytes = 1024 * 1024;
        private readonly Uri _url;
        private readonly PanelSecrets _secrets;
        private readonly string _hello;
        private readonly Action<PanelCommand> _onCommand;
        private readonly Action _onConnected;
        private readonly ILogger _logger;
        private readonly Channel<string> _outgoing = Channel.CreateBounded<string>(new BoundedChannelOptions(256) { FullMode = BoundedChannelFullMode.DropOldest });
        private readonly CancellationTokenSource _stop = new();
        private Task? _loop;
        private string? _lastProblem;

        // Define class constructor
        public PanelConnection(Uri url, PanelSecrets secrets, string hello, Action<PanelCommand> onCommand, Action onConnected, ILogger logger)
        {
            _url = url;
            _secrets = secrets;
            _hello = hello;
            _onCommand = onCommand;
            _onConnected = onConnected;
            _logger = logger;
        }

        public bool Connected { get; private set; }

        // Define class methods
        public void Start()
        {
            _loop = Task.Run(() => RunAsync(_stop.Token));
        }

        // Stops the loop and closes the link, waiting at most 2 seconds, so a plugin reload never leaves an old loop running
        // (the panel would refuse the new one as a second link for this server)
        public void Stop()
        {
            _stop.Cancel();
            _outgoing.Writer.TryComplete();
            try
            {
                _loop?.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException)
            {
            }
        }

        // From any thread. Dropped when not connected: the agent sends a full status on every (re)connect anyway.
        public void Send(string json)
        {
            if (Connected)
                _outgoing.Writer.TryWrite(json);
        }

        private async Task RunAsync(CancellationToken stop)
        {
            int failures = 0;
            while (!stop.IsCancellationRequested)
            {
                bool wasUp = false;
                TimeSpan wait = TimeSpan.Zero;
                try
                {
                    using var socket = new ClientWebSocket();
                    socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);   // Cloudflare drops idle links after about 100 s
                    socket.Options.SetRequestHeader("Authorization", $"Bearer {_secrets.Token}");
                    if (_secrets.AccessClientId.Length > 0)
                    {
                        socket.Options.SetRequestHeader("CF-Access-Client-Id", _secrets.AccessClientId);
                        socket.Options.SetRequestHeader("CF-Access-Client-Secret", _secrets.AccessClientSecret);
                    }

                    using (var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(stop))
                    {
                        connectTimeout.CancelAfter(TimeSpan.FromSeconds(15));
                        await socket.ConnectAsync(_url, connectTimeout.Token);
                    }
                    await SendTextAsync(socket, _hello, stop);

                    string? first = await ReceiveTextAsync(socket, stop);
                    string? refused = null;
                    using (JsonDocument answer = JsonDocument.Parse(first ?? "{}"))
                    {
                        string type = TypeOf(answer);
                        if (type != "welcome")
                            refused = type == "error" && answer.RootElement.TryGetProperty("message", out var message) ? message.GetString() ?? "" : "no welcome";
                    }
                    if (refused != null)
                    {
                        // A protocol or version mismatch won't fix itself in a minute
                        Problem($"The panel refused this server: {refused}");
                        wait = TimeSpan.FromMinutes(5);
                        throw new InvalidDataException(refused);
                    }

                    Connected = wasUp = true;
                    failures = 0;
                    _logger.LogInformation("[Panel] Connected to {Url}", _url.GetLeftPart(UriPartial.Authority));
                    _lastProblem = null;
                    _onConnected();

                    using var linkDown = CancellationTokenSource.CreateLinkedTokenSource(stop);
                    Task writer = WriteLoopAsync(socket, linkDown.Token);
                    try
                    {
                        await ReadLoopAsync(socket, stop);
                    }
                    finally
                    {
                        linkDown.Cancel();
                        await Task.WhenAny(writer, Task.Delay(1000));
                    }
                    if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                    {
                        using var closeTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", closeTimeout.Token);
                    }
                }
                catch (OperationCanceledException) when (stop.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception error) when (error is WebSocketException or OperationCanceledException or JsonException or InvalidDataException or IOException)
                {
                    // 401: the token is wrong; 409: this server already has a link (an old one times out); 403: Cloudflare Access.
                    // A refusal was logged above with its reason.
                    if (wait == TimeSpan.Zero)
                        Problem($"Can't reach the panel at {_url.GetLeftPart(UriPartial.Authority)}: {error.Message}");
                }
                finally
                {
                    Connected = false;
                    if (wasUp)
                        _logger.LogInformation(stop.IsCancellationRequested ? "[Panel] Link to the panel closed" : "[Panel] Link to the panel closed; reconnecting");
                }

                if (wait == TimeSpan.Zero)
                {
                    failures = wasUp ? 0 : failures + 1;
                    double seconds = Math.Min(60, Math.Pow(2, Math.Max(0, failures - 1)));
                    wait = TimeSpan.FromSeconds(seconds * (0.8 + Random.Shared.NextDouble() * 0.4));
                }
                try
                {
                    await Task.Delay(wait, stop);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        // The same problem twice in a row is logged once, so a panel that's down doesn't fill the log
        private void Problem(string text)
        {
            if (text != _lastProblem)
                _logger.LogWarning("[Panel] {Problem}", text);
            _lastProblem = text;
        }

        private async Task ReadLoopAsync(ClientWebSocket socket, CancellationToken stop)
        {
            while (!stop.IsCancellationRequested)
            {
                string? text = await ReceiveTextAsync(socket, stop);
                if (text == null)
                    return;

                using JsonDocument document = JsonDocument.Parse(text);
                if (TypeOf(document) != "command")
                    continue;   // Anything else is from a newer panel; ignored, not fatal
                PanelCommand? command = document.Deserialize<PanelCommand>(PanelProtocol.Json);
                if (command != null && command.Id.Length > 0)
                    _onCommand(command);
            }
        }

        private async Task WriteLoopAsync(ClientWebSocket socket, CancellationToken linkDown)
        {
            try
            {
                while (await _outgoing.Reader.WaitToReadAsync(linkDown))
                {
                    while (_outgoing.Reader.TryRead(out string? json))
                        await SendTextAsync(socket, json, linkDown);
                }
            }
            catch (Exception error) when (error is OperationCanceledException or WebSocketException or ChannelClosedException)
            {
            }
        }

        private static async Task SendTextAsync(ClientWebSocket socket, string text, CancellationToken stop)
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, stop);
        }

        private static async Task<string?> ReceiveTextAsync(ClientWebSocket socket, CancellationToken stop)
        {
            var buffer = new byte[16 * 1024];
            using var message = new MemoryStream();
            while (true)
            {
                WebSocketReceiveResult received = await socket.ReceiveAsync(buffer, stop);
                if (received.MessageType == WebSocketMessageType.Close)
                    return null;
                message.Write(buffer, 0, received.Count);
                if (message.Length > MaxMessageBytes)
                    throw new InvalidDataException("Message too large.");
                if (received.EndOfMessage)
                    return Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
            }
        }

        private static string TypeOf(JsonDocument document) =>
            document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("type", out JsonElement type)
                ? type.GetString() ?? ""
                : "";
    }
}
