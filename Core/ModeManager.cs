// Declare namespace
namespace GameModeManager
{
    // Define class
    public class ModeManager : IPluginDependency<Plugin, Config>
    {
       // Define dependencies
        private static Config? _config;
        private PluginState _pluginState;

        // Define class instance
        public ModeManager(PluginState pluginState)
        {
            _pluginState = pluginState;
        }

        // Load config
         public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        { 
            if (_config != null)
            {
                // Create modes for each game mode in game mode list
                foreach(var _mode in _config.GameModes.List)
                {
                    string name =_mode.Key;
                    Dictionary<string, List<string>> modeConfig = _mode.Value;
                    string configName = modeConfig.FirstOrDefault().Key;
                    List<string> _mapGroups = modeConfig.FirstOrDefault().Value;
                    List<MapGroup> mapGroups = new List<MapGroup>();
                    MapGroup? mapGroup;

                    foreach(string _mapGroup in _mapGroups)
                    {
                        mapGroup = _pluginState.MapGroups.FirstOrDefault(m => m.Name == _mapGroup) ?? PluginState.DefaultMapGroup;
                        mapGroups.Add(mapGroup);
                    }
                    // Add mode to new mode list
                    Mode gameMode = new Mode(name, configName, mapGroups);
                    _pluginState.Modes.Add(gameMode); 
                }

                // Set default mode
                _pluginState.CurrentMode =  _pluginState.Modes.FirstOrDefault(m => m.Name == _config.GameModes.DefaultMode) ?? PluginState.DefaultMode;
            }
        }
    }
}