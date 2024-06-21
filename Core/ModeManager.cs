// Declare namespace
using Microsoft.Extensions.Logging;

namespace GameModeManager
{
    // Define MapGroupManager class
    public class ModeManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private static Config? _config;
        private static ILogger? _logger;

        // Load dependencies
        public void OnLoad(Plugin plugin)
        { 
            _logger = plugin.Logger;
        }
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }
        
        // Construct reusable function to parse map groups
        public static void Load()
        {
            if (_config != null && _logger != null)
            {
                foreach(var _mode in _config.GameMode.List)
                {
                    string name =_mode.Key;
                    Dictionary<string, List<string>> modeConfig = _mode.Value;
                    foreach(var _configFile in modeConfig)
                    {
                        string configName = _configFile.Key;
                        List<MapGroup> mapGroups = new List<MapGroup>();

                        foreach(string _mapGroup in _configFile.Value)
                        {
                            MapGroup? mapGroup = PluginState.MapGroups.FirstOrDefault(m => m.Name == _mapGroup);

                            if(mapGroup != null)
                            {
                                mapGroups.Add(mapGroup);
                            }
                        }

                        Mode gameMode = new Mode(name, configName, mapGroups);
                        PluginState.Modes.Add(gameMode);
                    }
                }
            }
        }
    }
}