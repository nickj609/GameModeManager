// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Models;
using GameModeManager.Contracts;

// Declare namespace
namespace GameModeManager.Core
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

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on map start behavior
       public void OnMapStart(string map)
        {
            Map _map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(map, StringComparison.OrdinalIgnoreCase) || m.DisplayName.Equals(map, StringComparison.OrdinalIgnoreCase)) ?? new Map(map);
            _pluginState.CurrentMap = _map;
        }
        
        // Define method to update map list
        public void UpdateRTVMapList()
        {  
            if (_config.RTV.Enabled)
            {
                // Get map list
                List<Map> _mapList;

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