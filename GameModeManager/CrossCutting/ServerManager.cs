// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
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

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define class methods
        public void ChangeMap(IMap nextMap, int delay)
        {
            ChangeMap(delay, () =>
            {
                if (Server.IsMapValid(nextMap.Name))
                    Server.ExecuteCommand($"changelevel \"{nextMap.Name}\"");
                else if (nextMap.WorkshopId != -1)
                    Server.ExecuteCommand($"host_workshop_map \"{nextMap.WorkshopId}\"");
                else
                    Server.ExecuteCommand($"ds_workshop_changelevel \"{nextMap.Name}\"");
            });
        }

        // A map of the current mode, changed by GG1MapChooser so its recently played list knows about it
        public void ChangeMapInPool(IMap map)
        {
            if (_mapChooser.Available)
                ChangeMap(_config.Maps.Delay, () => _mapChooser.ChangeToMap(map));
            else
                ChangeMap(map, _config.Maps.Delay);
        }

        private void ChangeMap(int delay, Action change)
        {
            // Disable warmup
            _pluginState.Game.WarmupRunning = false;

            if(_config.Warmup.PerMap)
                _pluginState.Game.WarmupScheduled = false;

            // Kick bots and freeze all players
            new Timer(0.5f, () =>
            {
                Server.ExecuteCommand("bot_kick");
                PlayerExtensions.FreezePlayers();
            });

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
            _logger.LogInformation($"Current mode: {_pluginState.Game.CurrentMode.Name}");
            _logger.LogInformation($"New mode: {mode.Name}");

            // Execute mode config
            Server.ExecuteCommand($"exec {mode.Config}");

            // If no default map, set next map to random map
            IMap nextMap;
            if (mode.DefaultMap == null)
                nextMap = GetRandomMap(mode);
            else
                nextMap = mode.DefaultMap;

            ChangeMap(nextMap, _config.Maps.Delay);
        }

        // GG1MapChooser picks the next map (its vote, else random from the mode's pool); GameModeManager only
        // decides when, and rotates the mode when it's due
        public void TriggerRotation()
        {
            // Rotation off: GameModeManager leaves the map alone (GG1's own WinDrawSettings can change it instead)
            if (!_pluginState.Game.RotationsEnabled)
                return;

            bool modeDue = _config.Rotation.ModeRotation &&
                _pluginState.Game.MapRotations != 0 && _pluginState.Game.MapRotations % _config.Rotation.ModeInterval == 0;
            _pluginState.Game.MapRotations++;

            if (!modeDue)
            {
                _pluginState.Game.CountdownRunning = true;
                new CountdownTimer(_config.Maps.Delay, () =>
                {
                    _pluginState.Game.CountdownRunning = false;
                    _mapChooser.ChangeToNextMap();
                }, "Map changing in ");
                return;
            }

            List<IMode> modes = _pluginState.Game.Modes.Values.ToList();
            if (modes.Count == 0)
            {
                _logger.LogError("No game modes available for rotation.");
                return;
            }
            IMode nextMode = modes[new Random().Next(0, modes.Count)];
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rotation.change-mode", nextMode.Name));
            ChangeMode(nextMode);
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
