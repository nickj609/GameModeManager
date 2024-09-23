// Included libraries
using GameModeManager.Menus;
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using Microsoft.Extensions.Logging;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class ModeManager : IPluginDependency<Plugin, Config>
    {
       // Define dependencies
        private PluginState _pluginState;
        private readonly MapMenus _mapMenus;
        private ILogger<ModeManager> _logger;
        private readonly ModeMenus _modeMenus;
        private Config _config = new Config();
        private readonly MapManager _mapManager;

        // Define class instance
        public ModeManager(PluginState pluginState, ModeMenus modeMenus, MapMenus mapMenus, MapManager mapManager, ILogger<ModeManager> logger)
        {
            _logger = logger;
            _mapMenus = mapMenus;
            _modeMenus = modeMenus;
            _mapManager = mapManager;
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
            new Timer(3.0f, () => 
            {
                Server.ExecuteCommand($"exec {_pluginState.CurrentMode.Config}");
                Server.ExecuteCommand("mp_restartgame 1");
            });
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        { 
            // Create mode list from config
            foreach(ModeEntry _mode in _config.GameModes.List)
            {
                // Create map group list
                List<MapGroup> mapGroups = new List<MapGroup>();

                // Create map group from config
                foreach(string _mapGroup in _mode.MapGroups)
                {
                    MapGroup? mapGroup = _pluginState.MapGroups.FirstOrDefault(m => m.Name == _mapGroup);

                    // Add map group to list
                    if(mapGroup != null)
                    {
                        mapGroups.Add(mapGroup);
                    }
                    else
                    {
                        _logger.LogError($"Unable to find {_mapGroup} in map group list.");
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
                    _pluginState.Modes.Add(gameMode);
                }
                else
                {
                    _logger.LogError($"Unable to create map group list.");
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
                _logger.LogError($"Unable to find mode {_config.GameModes.Default.Name} in modes list.");
            }

            // Create mode menus
            _mapMenus.Load();
            _modeMenus.Load();

            // Create RTV map list
            _mapManager.UpdateRTVMapList();
        }
    }
}