// Declare namespace
namespace GameModeManager
{
    // Define class
    public class ModeManager : IPluginDependency<Plugin, Config>
    {
       // Define dependencies
        private PluginState _pluginState;
        private Config _config = new Config();
        private readonly MapManager _mapManager;
        private readonly MenuFactory _menuFactory;

        // Define class instance
        public ModeManager(PluginState pluginState, MenuFactory menuFactory, MapManager mapManager)
        {
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
            // Create modes for each game mode in game mode list
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
                }
                // Add mode to new mode list
                Mode gameMode = new Mode(_mode.Name, _mode.Config, mapGroups);
                _pluginState.Modes.Add(gameMode); 
            }

            // Set default mode
            _pluginState.CurrentMode = _pluginState.Modes.FirstOrDefault(m => m.Name == _config.GameModes.Default) ?? PluginState.DefaultMode;

            // Create mode menus
            _menuFactory.CreateModeMenus();
            _menuFactory.CreateMapMenus();

            // Create RTV map list
            _mapManager.UpdateRTVMapList();
            
        }
    }
}