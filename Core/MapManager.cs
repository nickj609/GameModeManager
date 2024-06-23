// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager
{
    // Define plugin class
    public partial class Plugin
    {
        // Define on event game end handler
        public HookResult EventGameEnd(EventCsWinPanelMatch @event, GameEventInfo info)
        {  
            // Check if RTV is disabled in config and if so enable randomization
            if(!_pluginState.RTVEnabled)
            {
     

                // Check if game mode rotation is enabled
                if(Config.GameModes.Rotation && _pluginState.MapRotations % Config.GameModes.Interval == 0)
                {  
                    Logger.LogInformation("Game has ended. Picking random game mode...");
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("Game has ended. Changing mode..."));  
                    // Get random game mode
                    Random _rnd = new Random();
                    int _randomIndex = _rnd.Next(0, _pluginState.Modes.Count); 
                    Mode _randomMode = _pluginState.Modes[_randomIndex];

                    // Change mode
                    Server.ExecuteCommand($"exec {_randomMode.Config}");

                    // Set current mode
                    _pluginState.CurrentMode = _randomMode;
                }
                else
                {
                    Logger.LogInformation("Game has ended. Picking random map from current mode...");
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("Game has ended. Changing map..."));  
                    // Get a random map
                    Random _rnd = new Random();
                    int _randomIndex = _rnd.Next(0, _pluginState.CurrentMode.Maps.Count); 
                    Map _randomMap = _pluginState.CurrentMode.Maps[_randomIndex];

                    // Change map
                    _mapManager.ChangeMap(_randomMap);
                }
            }
            _pluginState.MapRotations++;
            return HookResult.Continue;
        }

        // Define event map transition handler to set current map
        public HookResult EventMapChange(EventMapTransition @event, GameEventInfo info)
        {

            Map _map = _pluginState.Maps.FirstOrDefault(m => m.Name == Server.MapName) ?? new Map(Server.MapName);
            _pluginState.CurrentMap = _map;

            return HookResult.Continue;
        }
    }

    // Define map manager class
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

        // Load dependencies
        public void OnLoad(Plugin plugin)
        { 
            _logger = plugin.Logger;
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

        // Define reusable method to change map
        public void ChangeMap(Map _nextMap)
        {
            // If map valid, change map based on map type
            if (Server.IsMapValid(_nextMap.Name))
            {
                Server.ExecuteCommand($"changelevel \"{_nextMap.Name}\"");
            }
            else if (_nextMap.WorkshopId != -1)
            {
                Server.ExecuteCommand($"host_workshop_map \"{_nextMap.WorkshopId}\"");
            }
            else
            {
                Server.ExecuteCommand($"ds_workshop_changelevel \"{_nextMap.Name}\"");
            }
        }

    }
}