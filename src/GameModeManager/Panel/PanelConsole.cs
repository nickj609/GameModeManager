// Included libraries
using System.Text;
using System.Net.Sockets;
using CounterStrikeSharp.API.Modules.Cvars;

// Declare namespace
namespace GameModeManager.Panel
{
    // Define class
    // The web panel owner's console. CounterStrikeSharp can't read the server console's output, but the game's own RCON returns
    // it, so each line goes through RCON on this server's own address: never over the internet, and the panel never holds the
    // password. Where reads ConVars on the game thread; RunAsync talks to the socket and must not touch CounterStrikeSharp.
    public static class PanelConsole
    {
        // Define class properties
        private const int Auth = 3, AuthResponse = 2, ExecCommand = 2;
        private static readonly string[] Blocked = { "rcon_password", "sv_password", "rcon" };

        public sealed record Target(string Host, int Port, string Password);

        // Define class methods
        // On the game thread: where RCON listens, or why the console can't work
        public static (Target? Target, string? Problem) Where(bool enabled)
        {
            if (!enabled)
                return (null, "The console is turned off in GameModeManager's config (Panel.Console).");
            string password = ConVar.Find("rcon_password")?.StringValue ?? "";
            if (password.Length == 0)
                return (null, "RCON isn't set up on this server: start it with -usercon and set rcon_password.");
            int port = ConVar.Find("hostport")?.GetPrimitiveValue<int>() ?? 27015;
            string ip = ConVar.Find("ip")?.StringValue ?? "";
            string host = ip.Length == 0 || ip is "0.0.0.0" or "localhost" ? "127.0.0.1" : ip;
            return (new Target(host, port, password), null);
        }

        // Lines that would show or change the passwords that guard the server
        public static string? Refuse(string line)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0)
                return "Type a command.";
            if (trimmed.Length > 512 || trimmed.Contains('\n') || trimmed.Contains('\r'))
                return "One command, up to 512 characters.";
            string first = trimmed.Split(' ', ';')[0].ToLowerInvariant();
            if (Blocked.Contains(first) || trimmed.Contains("rcon_password", StringComparison.OrdinalIgnoreCase))
                return "The panel doesn't send commands that show or change the server's passwords.";
            return null;
        }

        // Off the game thread. Returns the output (cut at PanelProtocol.MaxOutputChars) or throws with a reason.
        public static async Task<string> RunAsync(Target target, string line, CancellationToken stop)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            using var client = new TcpClient();
            await client.ConnectAsync(target.Host, target.Port, timeout.Token);
            using NetworkStream stream = client.GetStream();

            await WriteAsync(stream, 1, Auth, target.Password, timeout.Token);
            while (true)
            {
                var (id, type, _) = await ReadAsync(stream, timeout.Token);
                if (type != AuthResponse)
                    continue;   // An empty response comes before the auth answer
                if (id == -1)
                    throw new InvalidOperationException("RCON refused the password (rcon_password changed?).");
                break;
            }

            await WriteAsync(stream, 2, ExecCommand, line, timeout.Token);

            // The reply can come in several packets; it's done once nothing more arrives for a moment
            var output = new StringBuilder();
            while (output.Length < PanelProtocol.MaxOutputChars)
            {
                using var idle = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
                idle.CancelAfter(output.Length == 0 ? TimeSpan.FromSeconds(3) : TimeSpan.FromMilliseconds(400));
                try
                {
                    var (_, _, body) = await ReadAsync(stream, idle.Token);
                    output.Append(body);
                }
                catch (OperationCanceledException) when (!timeout.IsCancellationRequested)
                {
                    break;
                }
            }
            string text = output.ToString();
            return text.Length > PanelProtocol.MaxOutputChars ? text[..PanelProtocol.MaxOutputChars] + "\n(cut)" : text.TrimEnd();
        }

        // Source RCON packet: size, id, type, body, two zero bytes (little-endian)
        private static async Task WriteAsync(NetworkStream stream, int id, int type, string body, CancellationToken stop)
        {
            byte[] text = Encoding.UTF8.GetBytes(body);
            byte[] packet = new byte[14 + text.Length];
            BitConverter.TryWriteBytes(packet.AsSpan(0), 10 + text.Length);
            BitConverter.TryWriteBytes(packet.AsSpan(4), id);
            BitConverter.TryWriteBytes(packet.AsSpan(8), type);
            text.CopyTo(packet, 12);
            await stream.WriteAsync(packet, stop);
        }

        private static async Task<(int Id, int Type, string Body)> ReadAsync(NetworkStream stream, CancellationToken stop)
        {
            byte[] header = new byte[4];
            await stream.ReadExactlyAsync(header, stop);
            int size = BitConverter.ToInt32(header);
            if (size < 10 || size > 1024 * 1024)
                throw new InvalidDataException("RCON sent a packet that doesn't look right.");
            byte[] rest = new byte[size];
            await stream.ReadExactlyAsync(rest, stop);
            int id = BitConverter.ToInt32(rest, 0);
            int type = BitConverter.ToInt32(rest, 4);
            string body = Encoding.UTF8.GetString(rest, 8, Math.Max(0, size - 10));
            return (id, type, body);
        }
    }
}
