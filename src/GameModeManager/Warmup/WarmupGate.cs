// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Warmup
{
    // Define class
    // What has to happen before GameModeManager's warmup can end:
    // - Waiting for players: with fewer than MinPlayers on a team, the game's own warmup timer is paused (mp_warmup_pausetimer),
    //   so its countdown stops where it is and carries on once enough players have joined.
    // - Ready-up: players type !ready. The timer stays paused, and the warmup ends once ReadyPercent of the players on a team
    //   are ready.
    // Both come from the config (Warmup.MinPlayers, Warmup.Ready, Warmup.ReadyPercent), and a mode or map cfg can set its own
    // with css_warmup_minplayers and css_warmup_ready. A mode change, or a map change with Warmup.PerMap, puts the config's back.
    public class WarmupGate : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private WarmupManager _warmupManager;
        private StringLocalizer _localizer;
        private ILogger<WarmupGate> _logger;
        private Config _config = new();

        // Define class constructor
        public WarmupGate(PluginState pluginState, WarmupManager warmupManager, StringLocalizer localizer, ILogger<WarmupGate> logger)
        {
            _logger = logger;
            _localizer = localizer;
            _pluginState = pluginState;
            _warmupManager = warmupManager;
        }

        // Define class properties
        private const int ReminderSeconds = 20; // How often the waiting or ready line repeats in chat
        private readonly HashSet<int> _ready = new();
        private bool _paused;
        private int _secondsToReminder;

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            if (!_config.Warmup.Enabled)
                return;

            plugin.AddCommand("css_warmup_minplayers", "Players needed before warmup can end. Usage: css_warmup_minplayers <n>", OnMinPlayersCommand);
            plugin.AddCommand("css_warmup_ready", "Ready-up in warmup. Usage: css_warmup_ready <true|false> [percent]", OnReadyCommand);
            plugin.AddCommand("css_ready", "Ready for the match to start (warmup with ready-up).", OnPlayerReady);
            plugin.AddCommand("css_unready", "Not ready any more.", OnPlayerUnready);
            plugin.RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
            {
                if (@event.Userid != null)
                    _ready.Remove(@event.Userid.Slot);
                return HookResult.Continue;
            });
            plugin.AddTimer(1.0f, Check, TimerFlags.REPEAT);
        }

        // Define on map start behavior
        public void OnMapStart(string mapName)
        {
            _ready.Clear();
            _paused = false;
        }

        // Define class methods
        private void Check()
        {
            if (!_pluginState.Game.WarmupRunning)
            {
                _ready.Clear();
                SetPaused(false);
                return;
            }

            List<CCSPlayerController> players = PlayersOnTeams();
            int minPlayers = _pluginState.Game.WarmupMinPlayers;
            bool waiting = minPlayers > 0 && players.Count < minPlayers;
            bool readyUp = _pluginState.Game.WarmupReady;
            int ready = players.Count(player => _ready.Contains(player.Slot));
            bool allReady = players.Count > 0 && ready * 100 >= _pluginState.Game.WarmupReadyPercent * players.Count;

            if (readyUp && !waiting && allReady)
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("warmup.all-ready"));
                SetPaused(false);
                _warmupManager.EndWarmup();
                return;
            }

            SetPaused(waiting || readyUp);
            if (!waiting && !readyUp)
                return;

            if (--_secondsToReminder > 0)
                return;
            _secondsToReminder = ReminderSeconds;
            if (waiting)
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("warmup.waiting-players", players.Count, minPlayers));
            else
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("warmup.ready-status", ready, players.Count));
        }

        // The game's warmup countdown stops while paused, and carries on from there
        private void SetPaused(bool paused)
        {
            if (paused == _paused)
                return;

            _paused = paused;
            _secondsToReminder = paused ? 1 : 0;
            Server.ExecuteCommand($"mp_warmup_pausetimer {(paused ? 1 : 0)}");
        }

        private static List<CCSPlayerController> PlayersOnTeams()
        {
            return PlayerExtensions.ValidPlayers(false).Where(player => player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist).ToList();
        }

        // Define command handlers
        // For mode and map cfgs: how many players warmup waits for
        [CommandHelper(minArgs: 1, usage: "<n>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnMinPlayersCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (int.TryParse(command.ArgByIndex(1), out int players) && players >= 0)
                _pluginState.Game.WarmupMinPlayers = players;
            else
                _logger.LogWarning("css_warmup_minplayers: '{Value}' isn't a number of players", command.ArgByIndex(1));
        }

        // For mode and map cfgs: ready-up on or off, and optionally the share of players who must be ready
        [CommandHelper(minArgs: 1, usage: "<true|false> [percent]", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnReadyCommand(CCSPlayerController? player, CommandInfo command)
        {
            string value = command.ArgByIndex(1);
            _pluginState.Game.WarmupReady = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
            if (command.ArgCount > 2 && int.TryParse(command.ArgByIndex(2), out int percent) && percent is > 0 and <= 100)
                _pluginState.Game.WarmupReadyPercent = percent;
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnPlayerReady(CCSPlayerController? player, CommandInfo command)
        {
            if (!Usable(player) || !_ready.Add(player!.Slot))
                return;

            List<CCSPlayerController> players = PlayersOnTeams();
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("warmup.ready-player", player.PlayerName, players.Count(p => _ready.Contains(p.Slot)), players.Count));
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnPlayerUnready(CCSPlayerController? player, CommandInfo command)
        {
            if (!Usable(player) || !_ready.Remove(player!.Slot))
                return;

            List<CCSPlayerController> players = PlayersOnTeams();
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("warmup.unready-player", player.PlayerName, players.Count(p => _ready.Contains(p.Slot)), players.Count));
        }

        // Only in GameModeManager's warmup with ready-up on. Otherwise silent: another plugin (MatchZy) may use !ready too.
        private bool Usable(CCSPlayerController? player)
        {
            return player != null && player.IsValid && _pluginState.Game.WarmupRunning && _pluginState.Game.WarmupReady
                && player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist;
        }
    }
}
