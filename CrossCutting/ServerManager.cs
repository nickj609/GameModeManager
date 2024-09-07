// Included libraries
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager
{
    // Define ServerManager class
    public static class ServerManager
    {
        // Define reusable method to change map
        public static void ChangeMap(Map nextMap)
        {
            // If map valid, change map based on map type
            if (Server.IsMapValid(nextMap.Name))
            {
                Server.ExecuteCommand($"changelevel \"{nextMap.Name}\"");
            }
            else if (nextMap.WorkshopId != -1)
            {
                Server.ExecuteCommand($"host_workshop_map \"{nextMap.WorkshopId}\"");
            }
            else
            {
                Server.ExecuteCommand($"ds_workshop_changelevel \"{nextMap.Name}\"");
            }
        }

        // Define reusable method to change mode
        public static void ChangeMode(Mode mode, Plugin plugin, PluginState pluginState, float delay)
        {
            if (plugin != null)
            {
                // Change mode
                plugin.AddTimer(delay, () => 
                {
                    if (pluginState.WarmupModeEnabled == false)
                    {
                        Server.ExecuteCommand($"exec {mode.Config}");
                    }
                    else
                    {
                        Server.ExecuteCommand($"exec {pluginState.WarmupMode.Config}");
                    }
                    ChangeMap(mode.DefaultMap);
                }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

                // Set current mode
                pluginState.CurrentMode = mode; 
            }
        }

        // Define method to trigger mode and map rotations
        public static void TriggerRotation(Plugin plugin,Config config, PluginState pluginState, ILogger logger, StringLocalizer localizer )
        {  
            // Check if rotations are enabled
            if(config.Rotation.Enabled)
            {
                // If mode rotations are enabled, change mode on mode interval
                if (config.Rotation.ModeRotation && pluginState.MapRotations % config.Rotation.ModeInterval == 0)
                {  
                    // Log information
                    logger.LogInformation("Game has ended. Picking random game mode...");
            
                    // Get random game mode
                    Random _rnd = new Random();
                    int _randomIndex = _rnd.Next(0, pluginState.Modes.Count); 
                    Mode _randomMode = pluginState.Modes[_randomIndex];

                    // Change mode
                    Server.PrintToChatAll(localizer.LocalizeWithPrefix("Game has ended. Changing mode..."));  
                    ChangeMode(_randomMode, plugin ,pluginState, config.GameModes.Delay);
                }
                else // Change map
                {
                    // Define random map
                    Map _randomMap;    

                    // Choose random map           
                    if (config.Rotation.Cycle == 2) // If cycle = 2, select from specified map groups in config
                    {
                        List<Map> _mapList = new List<Map>();

                        foreach (string mapGroup in config.Rotation.MapGroups)
                        {
                            MapGroup? _mapGroup = pluginState.MapGroups.FirstOrDefault(m => m.Name.Equals(mapGroup, StringComparison.OrdinalIgnoreCase));

                            if (_mapGroup != null)
                            {
                                foreach (Map _map in _mapGroup.Maps)
                                {
                                    _mapList.Add(_map);
                                }
                            }
                        } 

                        // Get a random map from current game mode
                        Random _rnd = new Random();
                        int _randomIndex = _rnd.Next(0, _mapList.Count); 
                        _randomMap = _mapList[_randomIndex];

                    }
                    else if (config.Rotation.Cycle == 1) // If cycle = 1, select from all maps
                    {
                        // Log message
                        logger.LogInformation("Game has ended. Picking random map from all maps list...");

                        // Get a random map from all registered maps
                        Random _rnd = new Random();
                        int _randomIndex = _rnd.Next(0, pluginState.Maps.Count); 
                        _randomMap = pluginState.Maps[_randomIndex];

                    
                    }
                    else // Select from current mode
                    {
                        // Log message
                        logger.LogInformation("Game has ended. Picking random map from current mode...");

                        // Get a random map from current game mode
                        Random _rnd = new Random();
                        int _randomIndex = _rnd.Next(0, pluginState.CurrentMode.Maps.Count); 
                        _randomMap = pluginState.CurrentMode.Maps[_randomIndex];
                    }

                    // Change map
                    Server.PrintToChatAll(localizer.LocalizeWithPrefix("Game has ended. Changing map..."));
                    ChangeMap(_randomMap);
                }
                pluginState.MapRotations++;
            }
        }

        // Define method to trigger schedule change
        public static void TriggerScheduleChange(Plugin plugin, ScheduleEntry state, PluginState pluginState, Config config)
        {
            Mode? _mode = pluginState.Modes.FirstOrDefault(m => m.Name.Equals(state.Mode, StringComparison.OrdinalIgnoreCase));

            if (_mode != null)
            {
                // Check if current mode is different from target mode
                if (pluginState.CurrentMode != _mode)
                {
                    ChangeMode(_mode, plugin, pluginState, config.GameModes.Delay);
                }
            }
        }
    }
}