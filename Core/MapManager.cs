// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

// Declare namespace
namespace GameModeManager
{
    // Define class
    public class MapManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private PluginState _pluginState;
        private Config _config = new Config();

        // Define class instance
        public MapManager(PluginState pluginState)
        {
            _pluginState = pluginState;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        { 
            // Register event map transition handler to set current map
            plugin.RegisterEventHandler<EventMapTransition>((@event, info) =>
            {

                Map _map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(Server.MapName, StringComparison.OrdinalIgnoreCase)) ?? new Map(Server.MapName);
                _pluginState.CurrentMap = _map;

                return HookResult.Continue;
            }, HookMode.Post); 
        }
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define reusable method to update map list
        public void UpdateRTVMapList()
        {  
            if (_config.RTV.Enabled)
            {

                // Get map list
                List<Map> _mapList = new List<Map>();

                if (_config.RTV.Mode == 1)
                {
                    _mapList = _pluginState.Maps;
                }
                else
                {
                    _mapList = _pluginState.CurrentMode.Maps;
                }

                // Update map list for RTV Plugin
                using (StreamWriter writer = new StreamWriter(_config.RTV.MapList))
                {
                    foreach (Map _map in _mapList)  
                    {
                        if (_map.WorkshopId < 0)
                        {
                            writer.WriteLine(_map.Name);
                        }
                        else
                        {
                            if(_config.RTV.MapFormat)
                            {
                                writer.WriteLine($"ws:{_map.WorkshopId}");
                            }
                            else
                            {
                                writer.WriteLine($"{_map.Name}:{_map.WorkshopId}");
                            }
                        }
                    } 
                } 
                
                // Reload RTV Plugin
                Server.ExecuteCommand($"css_plugins reload {_config.RTV.Plugin}");
            }
        }
    }
}