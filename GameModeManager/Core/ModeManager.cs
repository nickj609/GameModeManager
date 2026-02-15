// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Models;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class ModeManager : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private ILogger<ModeManager> _logger;
        private Config _config = new Config();

        // Define class constructor
        public ModeManager(PluginState pluginState, ILogger<ModeManager> logger)
        {
            _logger = logger;
            _pluginState = pluginState;
        }

        // Define class methods
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        public void OnMapStart(string map)
        {
            string _modeConfig = PluginExtensions.RemoveCfgExtension(_pluginState.Game.CurrentMode.Config);
            string _settingsConfig = $"{_modeConfig}_settings.cfg";

            new Timer(.5f, () =>
            {
                Server.ExecuteCommand($"exec {_settingsConfig}");
                Server.ExecuteCommand("mp_restartgame 1");
            });
        }

        public void OnLoad(Plugin plugin)
        {
            foreach (ModeEntry _mode in _config.GameModes.List)
            {
                HashSet<IMapGroup> mapGroups = new HashSet<IMapGroup>();

                foreach (string _mapGroup in _mode.MapGroups)
                {
                    if (_pluginState.Game.MapGroups.TryGetValue(_mapGroup, out IMapGroup? mapGroup))
                        mapGroups.Add(mapGroup);
                    else
                        _logger.LogError($"Cannot find {_mapGroup} in map group list.");
                }

                IMode? gameMode;

                if (mapGroups.Count > 0)
                {
                    if (_mode.DefaultMap != null)
                    {
                        if (long.TryParse(_mode.DefaultMap, out long workshopId) && _pluginState.Game.MapsByWorkshopId.TryGetValue(workshopId, out IMap? workshopMap))
                        {
                            gameMode = new Mode(_mode.Name, _mode.Config, workshopMap, mapGroups);
                        }
                        else if (_pluginState.Game.Maps.TryGetValue(_mode.DefaultMap, out IMap? defaultMap))
                        {
                            gameMode = new Mode(_mode.Name, _mode.Config, defaultMap, mapGroups);
                        }
                        else
                        {
                            _logger.LogWarning($"Cannot find default map {_mode.DefaultMap} in maps list for mode {_mode.Name}.");
                            gameMode = new Mode(_mode.Name, _mode.Config, mapGroups);
                        }
                    }
                    else
                    {
                        gameMode = new Mode(_mode.Name, _mode.Config, mapGroups);
                    }

                    _pluginState.Game.Modes.Add(_mode.Name, gameMode);
                }
                else
                {
                    _logger.LogWarning($"No mapgroups found for mode {_mode.Name}. No map list created.");
                }
            };

            if (_pluginState.Game.Modes.TryGetValue(_config.GameModes.Default.Name, out IMode? currentMode))
                _pluginState.Game.CurrentMode = currentMode;
            else
                _logger.LogWarning($"Cannot find mode {_config.GameModes.Default.Name} in mode list.");
        }
    }
}