// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Commands;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using TimerFlags = CounterStrikeSharp.API.Modules.Timers.TimerFlags;
using CountdownTimer = GameModeManager.Timers.CountdownTimer;

// Declare namespace
namespace GameModeManager.CrossCutting
{
    // Define class
    public class ServerManager : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private ILogger<ServerManager> _logger;
        private MapChooserBridge _mapChooser;

        // Define class constructor
        public ServerManager(PluginState pluginState, ILogger<ServerManager> logger, StringLocalizer localizer, MapChooserBridge mapChooser)
        {
            _logger = logger;
            _mapChooser = mapChooser;
            _localizer = localizer;
            _pluginState = pluginState;
        }

        // Define class properties
        private Timer? _holdTimer;
        private float _holdStarted;
        private bool _holdTimedOut;
        private const float HoldLimit = 180f; // Longest a map change holds players: host_workshop_map keeps the old map running while it downloads
        private bool MapChanging => _holdTimer != null || _pluginState.Game.CountdownRunning;

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        // Every map change ends in one of these commands, whoever started it, so they start the hold too
        public void OnLoad(Plugin plugin)
        {
            foreach (string command in new[] { "map", "changelevel", "host_workshop_map", "ds_workshop_changelevel" })
                plugin.AddCommandListener(command, OnMapChangeCommand, HookMode.Pre);
        }

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            _holdTimer = null;
            _holdTimedOut = false;
            _pluginState.Game.CountdownRunning = false;

            // GG1MapChooser counts down 5 seconds before its command: hold players from when it decides
            new Timer(0.5f, () =>
            {
                if (!_holdTimedOut && _mapChooser.MapChanging() == true)
                    HoldPlayers();
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        // Define command handlers
        private HookResult OnMapChangeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
                HoldPlayers();

            return HookResult.Continue;
        }

        // Define class methods
        // The one place players get frozen for a map change. They stay frozen until the map ends: a new round, a respawn
        // or a closing menu would otherwise free them while a workshop map downloads.
        public void HoldPlayers()
        {
            if (_holdTimer != null)
                return;

            // Warmup ends with the map. Its cfg unloaded the mode's plugins, so put the mode back for the next map.
            if (_pluginState.Game.WarmupRunning)
            {
                _pluginState.Game.WarmupRunning = false;
                Server.ExecuteCommand($"exec {_pluginState.Game.CurrentMode.Config}");
            }

            if (_config.Warmup.PerMap)
                _pluginState.Game.WarmupScheduled = false;

            Server.ExecuteCommand("bot_kick");
            PlayerExtensions.FreezePlayers();

            _holdStarted = Server.CurrentTime;
            _holdTimer = new Timer(0.5f, () =>
            {
                if (Server.CurrentTime - _holdStarted < HoldLimit)
                {
                    PlayerExtensions.FreezePlayers();
                    return;
                }

                _logger.LogError("The map did not change within {Limit} seconds: unfreezing players.", HoldLimit);
                _holdTimedOut = true;
                _holdTimer?.Kill();
                _holdTimer = null;
                PlayerExtensions.UnfreezePlayers();
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        public void ChangeMap(IMap nextMap, int delay)
        {
            ChangeMap(delay, () => RunMapCommand(nextMap));
        }

        private void RunMapCommand(IMap nextMap)
        {
            if (Server.IsMapValid(nextMap.Name))
                Server.ExecuteCommand($"changelevel \"{nextMap.Name}\"");
            else if (nextMap.WorkshopId != -1)
                Server.ExecuteCommand($"host_workshop_map \"{nextMap.WorkshopId}\"");
            else
                Server.ExecuteCommand($"ds_workshop_changelevel \"{nextMap.Name}\"");
        }

        // A map of the current mode, changed by GG1MapChooser so its recently played list knows about it
        public void ChangeMapInPool(IMap map)
        {
            if (_mapChooser.Available)
                ChangeMap(_config.Maps.Delay, () =>
                {
                    _mapChooser.ChangeToMap(map);

                    // GG1 does nothing if the name doesn't match one map in its list, which would leave players frozen
                    new Timer(2.0f, () =>
                    {
                        if (_mapChooser.MapChanging() == false)
                        {
                            _logger.LogWarning("GG1MapChooser didn't change to {Map}: changing to it directly.", map.Name);
                            RunMapCommand(map);
                        }
                    }, TimerFlags.STOP_ON_MAPCHANGE);
                });
            else
                ChangeMap(map, _config.Maps.Delay);
        }

        private void ChangeMap(int delay, Action change)
        {
            if (_pluginState.Game.CountdownRunning)
            {
                _logger.LogInformation("A map change is already counting down: this request is ignored.");
                return;
            }

            HoldPlayers();

            // Display Countdown
            _pluginState.Game.CountdownRunning = true;
            CountdownTimer timer = new CountdownTimer(delay, () =>
            {
                // Change map
                change();

                // Disable countdown flag
                _pluginState.Game.CountdownRunning = false;

            }, "Map changing in ");
        }

        public void ChangeMode(IMode mode)
        {
            ChangeMode(mode, _config.Maps.Delay);
        }

        public void ChangeMode(IMode mode, int delay)
        {
            _logger.LogInformation($"Current mode: {_pluginState.Game.CurrentMode.Name}");
            _logger.LogInformation($"New mode: {mode.Name}");

            ChangeMap(delay, () =>
            {
                // Execute mode config. Not before the countdown: its settings and restarts would hit the map being left.
                Server.ExecuteCommand($"exec {mode.Config}");

                // If no default map, set next map to random map
                IMap nextMap;
                if (mode.DefaultMap == null)
                    nextMap = GetRandomMap(mode);
                else
                    nextMap = mode.DefaultMap;

                // The cfg runs first: its game_type and game_mode apply when the next map loads
                new Timer(0.25f, () => RunMapCommand(nextMap), TimerFlags.STOP_ON_MAPCHANGE);
            });
        }

        // GG1MapChooser picks the next map (its vote, else random from the mode's pool); GameModeManager only
        // decides when, and rotates the mode when it's due
        public void TriggerRotation()
        {
            // Rotation off: GameModeManager leaves the map alone (GG1's own WinDrawSettings can change it instead)
            if (!_pluginState.Game.RotationsEnabled)
                return;

            // The win panel, the time limit and the empty server timer can all ask for the same change
            if (MapChanging)
                return;

            bool modeDue = _config.Rotation.ModeRotation &&
                _pluginState.Game.MapRotations != 0 && _pluginState.Game.MapRotations % _config.Rotation.ModeInterval == 0;
            _pluginState.Game.MapRotations++;

            // A rotation has to end up somewhere else: not the current mode, and not a mode with no maps
            List<IMode> modes = _pluginState.Game.Modes.Values.Where(mode => !mode.Equals(_pluginState.Game.CurrentMode) && mode.Maps.Count > 0 && _config.GameModes.InRotation(mode.Name)).ToList();
            if (!modeDue || modes.Count == 0)
            {
                ChangeMap(_config.Maps.Delay, ChangeToNextMap);
                return;
            }

            IMode nextMode = modes[new Random().Next(0, modes.Count)];
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rotation.change-mode", nextMode.Name));
            ChangeMode(nextMode);
        }

        private void ChangeToNextMap()
        {
            // GG1 skips the change while its vote runs, which would leave the server sitting on the win panel
            if (_mapChooser.VoteInProgress())
            {
                new Timer(1.0f, ChangeToNextMap, TimerFlags.STOP_ON_MAPCHANGE);
                return;
            }

            _mapChooser.ChangeToNextMap();

            // GG1 does nothing when it has no map to give (all played recently, invalid mapcycle), or isn't installed
            new Timer(2.0f, () =>
            {
                if (_mapChooser.Available && _mapChooser.MapChanging() != false)
                    return;

                IMap nextMap = GetRandomMap(_pluginState.Game.CurrentMode);
                _logger.LogWarning("GG1MapChooser has no next map: changing to {Map}.", nextMap.Name);
                RunMapCommand(nextMap);
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        public void TriggerScheduleChange(ScheduleEntry state)
        {
            if (_pluginState.Game.Modes.TryGetValue(state.Mode, out IMode? _mode) && !_pluginState.Game.CurrentMode.Equals(_mode))
                ChangeMode(_mode);
        }

        public IMap GetRandomMap(IMode mode)
        {
            if (mode.Maps.Count == 0)
            {
                _logger.LogError($"Mode '{mode.Name}' has no maps; staying on the current map.");
                return _pluginState.Game.CurrentMap;
            }
            return mode.Maps.ElementAt(new Random().Next(0, mode.Maps.Count));
        }
    }
}
