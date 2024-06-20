// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager
{
    // Define plugin class
    public partial class Plugin : BasePlugin
    {
        private int MapRotations = 0;
        public HookResult EventGameEnd(EventCsWinPanelMatch @event, GameEventInfo info)
        {
            if (_localizer != null)
            {
                Logger.LogInformation("Game has ended. Picking random map from current map group...");
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("Game has ended. Changing map..."));
            }

            // Check if RTV is disabled in config and if so enable randomization
            if(!PluginState.RTVEnabled)
            {
                if(PluginState.CurrentMapGroup == null)
                {
                    PluginState.CurrentMapGroup = PluginState.DefaultMapGroup;
                }         

                // Check if game mode rotation is enabled
                if(Config.GameMode.Rotation && (float)MapRotations % Config.GameMode.Interval == 0)
                {  
                    // Get random game mode
                    Random _rnd = new Random();
                    int _randomIndex = _rnd.Next(0, PluginState.MapGroups.Count); 
                    MapGroup _randomMode = PluginState.MapGroups[_randomIndex];

                    // Change mode
                    Server.ExecuteCommand($"exec {_randomMode.Name}.cfg");
                }
                else
                {
                    // Get a random map
                    Random _rnd = new Random();
                    int _randomIndex = _rnd.Next(0, PluginState.CurrentMapGroup.Maps.Count); 
                    Map _randomMap = PluginState.CurrentMapGroup.Maps[_randomIndex];

                    // Change map
                    MapManager.ChangeMap(_randomMap);
                }
            }
            MapRotations++;
            return HookResult.Continue;
        }

        // Construct EventMapTransition Handler to automatically change map at game end
        public HookResult EventMapChange(EventMapTransition @event, GameEventInfo info)
        {
            if(PluginState.Maps != null)
            {
                Map _map = PluginState.Maps.FirstOrDefault(m => m.Name == Server.MapName) ?? new Map(Server.MapName);
                PluginState.CurrentMap = _map;
            }

            return HookResult.Continue;
        }
    }

    // Define MapManager class
    public class MapManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private static Config? _config;
        private static Plugin? _plugin;
        private static ILogger? _logger;
        private static StringLocalizer? _localizer;

        // Load dependencies
        public void OnLoad(Plugin plugin)
        { 
            _plugin = plugin;
            _logger = plugin.Logger;
            _localizer = new StringLocalizer(plugin.Localizer);
        }
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Construct reusable function to update map list
        public static void UpdateMapList(MapGroup _group)
        {  
            if (_logger != null)
            {
                // If using RTV Plugin
                if(_config != null &&_config.RTV.Enabled)
                {
                    // Update map list for RTV Plugin
                    try 
                    {
                        using (StreamWriter writer = new StreamWriter(_config.RTV.MapListFile))
                        {
                            foreach (Map _map in _group.Maps)  
                            {
                                if (string.IsNullOrEmpty(_map.WorkshopId))
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

                // Update map list for map menu
                try
                {
                    MenuFactory.UpdateMapMenu(_group);
                }
                catch(Exception ex)
                {
                    _logger.LogError($"{ex.Message}");
                }
            }
        }

        // Construct reusable function to change map
        public static void ChangeMap(Map _nextMap)
        {
            // If map valid, change map based on map type
            if (Server.IsMapValid(_nextMap.Name))
            {
                Server.ExecuteCommand($"changelevel \"{_nextMap.Name}\"");
            }
            else if (_nextMap.WorkshopId != null)
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