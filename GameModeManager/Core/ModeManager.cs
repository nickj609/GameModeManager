// Included libraries
using GameModeManager.Menus;
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class ModeManager : IPluginDependency<Plugin, Config>
    {
       // Define class dependencies
        private PluginState _pluginState;
        private readonly MapMenus _mapMenus;
        private ILogger<ModeManager> _logger;
        private readonly ModeMenus _modeMenus;
        private Config _config = new Config();

        // Define class instance
        public ModeManager(PluginState pluginState, ModeMenus modeMenus, MapMenus mapMenus, ILogger<ModeManager> logger)
        {
            _logger = logger;
            _mapMenus = mapMenus;
            _modeMenus = modeMenus;
            _pluginState = pluginState;
        }

        // Load config
         public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            string _modeConfig = Extensions.RemoveCfgExtension(_pluginState.CurrentMode.Config);
            string _settingsConfig = $"{_modeConfig}_settings.cfg";

            new Timer(.5f, () => 
            {
                Server.ExecuteCommand($"exec {_settingsConfig}");
                Server.ExecuteCommand("mp_restartgame 1");
            });
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        { 
            foreach(ModeEntry _mode in _config.GameModes.List)
            {
                List<MapGroup> mapGroups = new List<MapGroup>();

                foreach(string _mapGroup in _mode.MapGroups)
                {
                    MapGroup? mapGroup = _pluginState.MapGroups.FirstOrDefault(m => m.Name.Equals(_mapGroup, StringComparison.OrdinalIgnoreCase));

                    if(mapGroup != null)
                    {
                        mapGroups.Add(mapGroup);
                    }
                    else
                    {
                        _logger.LogError($"Cannot find {_mapGroup} in map group list.");
                    }
                }

                // Create game mode
                Mode? gameMode;

                if(mapGroups.Count > 0)
                {
                    if(_mode.DefaultMap != null)
                    {
                        gameMode = new Mode(_mode.Name, _mode.Config, _mode.DefaultMap, mapGroups);
                    }
                    else
                    {
                        gameMode = new Mode(_mode.Name, _mode.Config, mapGroups);
                    }

                    // Add mode to list
                    _pluginState.Modes.Add(gameMode);
                }
                else
                {
                    _logger.LogError($"Cannot create map group list.");
                }
            }
               
            // Set current mode
            Mode? currentMode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(_config.GameModes.Default.Name, StringComparison.OrdinalIgnoreCase));

            if(currentMode != null)
            {
                _pluginState.CurrentMode = currentMode;
            }
            else
            {
                _logger.LogError($"Cannot find mode {_config.GameModes.Default.Name} in modes list.");
            }

            // Create mode menus
            _mapMenus.Load();
            _modeMenus.Load();
        }
    }
}