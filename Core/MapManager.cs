// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager
{
    // Define class
    public class MapManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private static Config? _config;
        private static ILogger? _logger;
        private PluginState _pluginState;

        // Define class instance
        public MapManager(PluginState pluginState)
        {
            _pluginState = pluginState;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        { 
            _logger = plugin.Logger;

            // Register event map transition handler to set current map
            plugin.RegisterEventHandler<EventMapTransition>((@event, info) =>
            {

                Map _map = _pluginState.Maps.FirstOrDefault(m => m.Name == Server.MapName) ?? new Map(Server.MapName);
                _pluginState.CurrentMap = _map;

                return HookResult.Continue;
            }, HookMode.Post); 
        }
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define reusable method to update map list
        public void UpdateMapList()
        {  
            if (_logger != null && _config != null && _config.RTV.Enabled)
            {
                // Update map list for RTV Plugin
                try 
                {
                    using (StreamWriter writer = new StreamWriter(_config.RTV.MapListFile))
                    {
                        foreach (Map _map in _pluginState.CurrentMode.Maps)  
                        {
                            if (_map.WorkshopId == -1)
                            {
                                writer.WriteLine(_map.Name);
                            }
                            else
                            {
                                if(_config.RTV.DefaultMapFormat)
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
                } 
                catch (IOException ex)
                {
                    _logger.LogError("Could not update map list.");
                    _logger.LogError($"{ex.Message}");
                }

                // Reload RTV Plugin
                Server.ExecuteCommand($"css_plugins reload {_config.RTV.Plugin}");
            }
        }

    }
}