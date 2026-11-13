// Included libraries
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using GameModeManager.Shared.Models;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;

// Declare namespace
namespace GameModeManager.Panel
{
    // Define class
    // The web panel's agent (Panel in GameModeManager.json, off by default). It connects out to the panel, so the game host
    // opens no port and the panel never uses RCON over the internet. Threads: PanelConnection's loop runs on a background task;
    // everything here that touches CounterStrikeSharp (commands, the status, events) runs on the game thread, reached through
    // Server.NextFrame. The console's RCON call is the one async step, and it uses no CounterStrikeSharp call.
    public class PanelAgent : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PanelReport _report;
        private PanelCommands _commands;
        private PanelData _data;
        private GameModeEvents _events;
        private ILogger<PanelAgent> _logger;
        private Config _config = new();

        // Define class constructor
        public PanelAgent(PanelReport report, PanelCommands commands, PanelData data, GameModeEvents events, ILogger<PanelAgent> logger)
        {
            _report = report;
            _commands = commands;
            _data = data;
            _events = events;
            _logger = logger;
        }

        // Define class properties
        private PanelConnection? _connection;
        private CounterStrikeSharp.API.Modules.Timers.Timer? _statusTimer;
        private CancellationTokenSource _consoleStop = new();
        private bool _statusQueued;
        private BasePlugin? _plugin;

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            if (!_config.Panel.Enabled)
                return;

            PanelSecrets? secrets = ReadSecrets();
            if (secrets == null)
                return;
            if (!Uri.TryCreate(_config.Panel.Url, UriKind.Absolute, out Uri? url) || (url.Scheme != "wss" && url.Scheme != "ws"))
            {
                _logger.LogError("[Panel] Panel.Url must be the panel's wss:// agent address, like wss://agent.example.com/agent. The panel is off.");
                return;
            }
            if (url.Scheme == "ws" && !url.IsLoopback)
                _logger.LogWarning("[Panel] Panel.Url is ws://, not wss://: the link isn't encrypted. Use the tunnel's wss:// address.");

            string hello = JsonSerializer.Serialize(new
            {
                type = "hello",
                protocol = PanelProtocol.Version,
                server = _config.Panel.ServerName,
                gmmVersion = plugin.ModuleVersion,
                cssVersion = CounterStrikeSharp.API.Api.GetVersionString()
            }, PanelProtocol.Json);

            _connection = new PanelConnection(url, secrets, hello, OnCommand, () => Server.NextFrame(SendStatus), _logger);
            _connection.Start();
            _statusTimer = plugin.AddTimer(30, SendStatus, TimerFlags.REPEAT);
            Subscribe();
            _plugin = plugin;
            plugin.AddCommandListener("say", OnSay, HookMode.Post);
            plugin.AddCommandListener("say_team", OnSayTeam, HookMode.Post);
            _logger.LogInformation("[Panel] Connecting to the panel as {Server}", _config.Panel.ServerName);
        }

        // Called from Plugin.Unload: stops the link so a reload doesn't leave two
        public void Stop()
        {
            if (_connection == null)
                return;
            Unsubscribe();
            _statusTimer?.Kill();
            _plugin?.RemoveCommandListener("say", OnSay, HookMode.Post);
            _plugin?.RemoveCommandListener("say_team", OnSayTeam, HookMode.Post);
            _plugin = null;
            _consoleStop.Cancel();
            _connection.Stop();
            _connection = null;
        }

        // Define class methods
        // panel-secrets.json next to GameModeManager.json: never in the main config, which the server repo keeps in git
        private PanelSecrets? ReadSecrets()
        {
            string root = new FileInfo(typeof(BasePlugin).Assembly.Location).Directory!.Parent!.FullName;
            string path = Path.Combine(root, "configs", "plugins", "GameModeManager", Path.GetFileName(_config.Panel.SecretsFile));
            if (!File.Exists(path))
            {
                _logger.LogError("[Panel] {File} is missing next to GameModeManager.json: it holds the panel token. The panel is off.", Path.GetFileName(path));
                return null;
            }
            try
            {
                PanelSecrets? secrets = JsonSerializer.Deserialize<PanelSecrets>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (string.IsNullOrWhiteSpace(secrets?.Token))
                {
                    _logger.LogError("[Panel] {File} has no Token. The panel is off.", Path.GetFileName(path));
                    return null;
                }
                return secrets;
            }
            catch (JsonException error)
            {
                _logger.LogError("[Panel] {File} isn't valid JSON ({Error}). The panel is off.", Path.GetFileName(path), error.Message);
                return null;
            }
        }

        // From the connection's task: straight to the game thread
        private void OnCommand(PanelCommand command)
        {
            Server.NextFrame(() => Handle(command));
        }

        // On the game thread
        private void Handle(PanelCommand command)
        {
            if (command.Name == "console")
            {
                RunConsole(command);
                return;
            }
            // A page of a plugin's data: looked up here, read from its database on a background task, answered from there.
            // Reading changes nothing, so no new status follows.
            if (command.Name == "query")
            {
                _data.Query(command, result => Reply(command, result), _consoleStop.Token);
                return;
            }
            // Who someone is on this server, for the panel's roles: asked for the person signing in, changes nothing
            if (command.Name == "whois")
            {
                Reply(command, Whois(command.Arg("steamid")));
                return;
            }
            PanelResult result = _commands.Run(command);
            Reply(command, result);
            QueueStatus();
        }

        // The admin flags and groups CounterStrikeSharp's AdminManager has for a SteamID on this server: CS2-SimpleAdmin's
        // admins (it loads them into AdminManager), admins.json and admin_groups.json, group flags merged in. The panel gives
        // that person what the same flags give in game.
        private static PanelResult Whois(string steamId)
        {
            if (!PanelData.IsSteamId(steamId) || !ulong.TryParse(steamId, out ulong id))
                return PanelResult.Refused("That isn't a SteamID64.");
            AdminData? admin = AdminManager.GetPlayerAdminData(new SteamID(id));
            return new PanelResult
            {
                Ok = true,
                Data = new PanelDataPage
                {
                    Columns = new() { new("flags", "Flags"), new("groups", "Groups"), new("immunity", "Immunity", "number") },
                    Rows = new()
                    {
                        new()
                        {
                            ["flags"] = admin == null ? "" : string.Join(' ', admin.GetAllFlags().OrderBy(f => f)),
                            ["groups"] = admin == null ? "" : string.Join(' ', admin.Groups.OrderBy(g => g)),
                            ["immunity"] = (admin?.Immunity ?? 0).ToString()
                        }
                    },
                    Total = 1
                }
            };
        }

        // The status follows the answer, and everything changed in one frame goes in one status
        private void QueueStatus()
        {
            if (_statusQueued)
                return;
            _statusQueued = true;
            Server.NextFrame(() =>
            {
                _statusQueued = false;
                SendStatus();
            });
        }

        private void Reply(PanelCommand command, PanelResult result)
        {
            result.Id = command.Id;
            _connection?.Send(JsonSerializer.Serialize(result, PanelProtocol.Json));
        }

        // On the game thread
        private void SendStatus()
        {
            if (_connection?.Connected != true)
                return;
            try
            {
                string? consoleProblem = PanelConsole.Where(_config.Panel.Console).Problem;
                var message = new { type = "status", status = _report.Build(consoleProblem) };
                _connection.Send(JsonSerializer.Serialize(message, PanelProtocol.Json));
            }
            catch (Exception error)
            {
                // Between maps some of the game's state isn't there yet; the next status will be
                _logger.LogDebug("[Panel] Status skipped: {Error}", error.Message);
            }
        }

        // The console: checked and read on the game thread, then RCON off it. The answer is a JSON string, so sending it from
        // the background task is safe.
        private void RunConsole(PanelCommand command)
        {
            string line = command.Arg("line");
            if (PanelConsole.Refuse(line) is string refused)
            {
                Reply(command, PanelResult.Refused(refused));
                return;
            }
            var (target, problem) = PanelConsole.Where(_config.Panel.Console);
            if (target == null)
            {
                Reply(command, PanelResult.Refused(problem ?? "The console isn't available."));
                return;
            }

            _logger.LogInformation("[Panel] {Admin} ({SteamId}) console: {Line}", command.AdminName, command.AdminSteamId, line.Trim());
            CancellationToken stop = _consoleStop.Token;
            _ = Task.Run(async () =>
            {
                PanelResult result;
                try
                {
                    string output = await PanelConsole.RunAsync(target, line.Trim(), stop);
                    result = new PanelResult { Ok = true, Message = "Sent.", Output = output };
                }
                catch (Exception error) when (error is IOException or System.Net.Sockets.SocketException or OperationCanceledException or InvalidOperationException or InvalidDataException or EndOfStreamException)
                {
                    result = PanelResult.Refused(error is OperationCanceledException
                        ? "RCON didn't answer in time."
                        : $"Couldn't reach this server's RCON ({error.Message}). Is the server started with -usercon?");
                }
                Reply(command, result);
            });
        }

        // GameModeManager's events, for the panel's Recent events (English, like the log)
        private void Subscribe()
        {
            _events.ModeChanged += OnModeChanged;
            _events.MapChanging += OnMapChanging;
            _events.NextMapSet += OnNextMapSet;
            _events.SettingChanged += OnSettingChanged;
            _events.ValueSettingChanged += OnValueSettingChanged;
            _events.ActionRun += OnActionRun;
            _events.VoteFinished += OnVoteFinished;
            _events.RoundRestored += OnRoundRestored;
            _events.WarmupStarted += OnWarmupStarted;
            _events.WarmupEnded += OnWarmupEnded;
        }

        private void Unsubscribe()
        {
            _events.ModeChanged -= OnModeChanged;
            _events.MapChanging -= OnMapChanging;
            _events.NextMapSet -= OnNextMapSet;
            _events.SettingChanged -= OnSettingChanged;
            _events.ValueSettingChanged -= OnValueSettingChanged;
            _events.ActionRun -= OnActionRun;
            _events.VoteFinished -= OnVoteFinished;
            _events.RoundRestored -= OnRoundRestored;
            _events.WarmupStarted -= OnWarmupStarted;
            _events.WarmupEnded -= OnWarmupEnded;
        }

        private void OnModeChanged(IMode old, IMode current) => Event("mode", $"Mode changed to {current.Name}");
        private void OnMapChanging(string map) => Event("map", $"Map changing to {map}");
        private void OnNextMapSet(string next) => Event("next", $"Next: {next}");
        private void OnSettingChanged(ISetting setting, bool on) => Event("setting", $"{setting.DisplayName} turned {(on ? "on" : "off")}");
        private void OnValueSettingChanged(IValueSetting setting, string label) => Event("setting", $"{setting.Name} set to {label}");
        private void OnActionRun(IGameAction action) => Event("action", $"Action {action.Name} run");
        private void OnVoteFinished(string title, bool passed, int yes, int no) =>
            Event("vote", $"Vote {(passed ? "passed" : "failed")} ({yes} yes, {no} no): {System.Text.RegularExpressions.Regex.Replace(title, @"[\x00-\x1F]+", "")}");
        private void OnRoundRestored(int round) => Event("round", $"Back to the start of round {round}");
        private void OnWarmupStarted(IMode mode) => Event("warmup", $"Warmup started: {mode.Name}");
        private void OnWarmupEnded() => Event("warmup", "Warmup ended");

        // Chat, for the panel's chat log (staff with @css/chat read it there). Sent as it happens; the panel keeps it.
        private HookResult OnSay(CCSPlayerController? player, CounterStrikeSharp.API.Modules.Commands.CommandInfo info) => Chat(player, info, false);
        private HookResult OnSayTeam(CCSPlayerController? player, CounterStrikeSharp.API.Modules.Commands.CommandInfo info) => Chat(player, info, true);

        private HookResult Chat(CCSPlayerController? player, CounterStrikeSharp.API.Modules.Commands.CommandInfo info, bool teamOnly)
        {
            if (_connection?.Connected != true || player == null || !player.IsValid || player.IsBot || player.AuthorizedSteamID == null)
                return HookResult.Continue;
            string text = info.GetArg(1).Trim();
            if (text.Length == 0)
                return HookResult.Continue;
            if (text.Length > 256)
                text = text[..256];
            _connection.Send(JsonSerializer.Serialize(new
            {
                type = "chat",
                steamId = player.AuthorizedSteamID.SteamId64.ToString(),
                name = player.PlayerName,
                team = PanelServerData.TeamName(player.TeamNum),
                text,
                teamOnly,
                at = DateTimeOffset.UtcNow
            }, PanelProtocol.Json));
            return HookResult.Continue;
        }

        private void Event(string name, string text)
        {
            if (_connection?.Connected != true)
                return;
            _connection.Send(JsonSerializer.Serialize(new { type = "event", name, text, at = DateTimeOffset.UtcNow }, PanelProtocol.Json));
            QueueStatus();
        }
    }
}
