// Included libraries
using GameModeManager.Models;
using GameModeManager.Contracts;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class ModeManager : IPluginDependency<Plugin, Config>
    {
       // Define dependencies
        private PluginState _pluginState;
        private ILogger<ModeManager> _logger;
        private Config _config = new Config();
        private readonly MapManager _mapManager;
        private readonly MenuFactory _menuFactory;

        // Define class instance
        public ModeManager(PluginState pluginState, MenuFactory menuFactory, MapManager mapManager, ILogger<ModeManager> logger)
        {
            _logger = logger;
            _mapManager = mapManager;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
        }

        // Load config
         public void OnConfigParsed(Config config)
        {
            _config = config;
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
            _menuFactory.CreateModeMenus();
            _menuFactory.CreateMapMenus();

            // Create RTV map list
            _mapManager.UpdateRTVMapList();
        }
    }
}