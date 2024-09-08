// Included libraries
using GameModeManager.Models;
using GameModeManager.Contracts;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class ModeManager : IPluginDependency<Plugin, Config>
    {
       // Define dependencies
        private PluginState _pluginState;
        private Config _config = new Config();
        private MapGroupManager _mapGroupManager;
        private readonly MapManager _mapManager;
        private readonly MenuFactory _menuFactory;

        // Define class instance
        public ModeManager(PluginState pluginState, MenuFactory menuFactory, MapManager mapManager, MapGroupManager mapGroupManager)
        {
            _mapManager = mapManager;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _mapGroupManager = mapGroupManager;
        }

        // Load config
         public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        { 
            // Create modes for each game mode in game mode list
            foreach(ModeEntry _mode in _config.GameModes.List)
            {
                // Create map group list
                List<MapGroup> mapGroups = _mapGroupManager.CreateMapGroupList(_mode.MapGroups);
                
                // Add mode to new mode list
                Mode gameMode = new Mode(_mode.Name, _mode.Config, _mode.DefaultMap, mapGroups);
                _pluginState.Modes.Add(gameMode); 
            }

            // Set current mode
            _pluginState.CurrentMode =  new Mode(_config.Warmup.Default.Name, _config.Warmup.Default.Config, _config.Warmup.Default.DefaultMap, new List<MapGroup>());

            // Create mode menus
            _menuFactory.CreateModeMenus();
            _menuFactory.CreateMapMenus();

            // Create RTV map list
            _mapManager.UpdateRTVMapList();
        }
    }
}