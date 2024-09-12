// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Models;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.CrossCutting
{
    // Define class
    public static class ServerManager
    {
        // Define reusable method to change map
        public static void ChangeMap(Map nextMap, Config config, Plugin plugin, PluginState pluginState)
        {
            // If random default map for mode
            if (nextMap.Name.Equals("pseudorandom", StringComparison.CurrentCultureIgnoreCase))
            {
                nextMap = GetRandomMap(config, pluginState);
            }

            // Change map
            plugin.AddTimer(config.Maps.Delay, () => 
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
            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        }

        // Define reusable method to change map
        public static void ChangeMap(Map nextMap, Config config, Plugin plugin, PluginState pluginState, float delay)
        {
            // If random default map for mode
            if (nextMap.Name.Equals("pseudorandom", StringComparison.CurrentCultureIgnoreCase))
            {
                nextMap = GetRandomMap(config, pluginState);
            }

            // Change map
            plugin.AddTimer(delay, () => 
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
            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
        }

        // Define reusable method to schedule warmup
        public static bool ScheduleWarmup(PluginState pluginState)
        {
            // Check if warmup mode is enabled
            if (pluginState.WarmupModeEnabled == false)
            {
                // If not, disable warmup started flag and return false
                pluginState.WarmupStarted = false;
                return false;
            }
            else
            {
                // If so, enable warmup started flag, execute warmup mode config, and return true
                pluginState.WarmupStarted = true;
                pluginState.WarmupModeEnabled = false;
                Server.ExecuteCommand($"exec {pluginState.WarmupMode.Config}");

                // Set current mode to warmup mode
                pluginState.CurrentMode = pluginState.WarmupMode;
                return true;
            }
        }

        // Define reusable method to change mode
        public static void ChangeMode(Mode mode, Config config, Plugin plugin, PluginState pluginState, float delay)
        {
            if (plugin != null)
            {
                // Change mode
                plugin.AddTimer(delay, () => 
                {
                    // Set next mode and map
                    pluginState.NextMode = mode;
                    pluginState.NextMap = mode.DefaultMap;

                    // Check for scheduled warmup
                    if (!ScheduleWarmup(pluginState))
                    {
                        // Execute mode config
                        Server.ExecuteCommand($"exec {mode.Config}");

                        // Set current mode
                        pluginState.CurrentMode = mode; 
                    }
                    
                    // Change map
                    ChangeMap(mode.DefaultMap, config, plugin, pluginState, 0);

                }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            }
        }

        // Define reusable method to change mode with desired map
        public static void ChangeMode(Mode mode, Map map, Config config, Plugin plugin, PluginState pluginState, float delay)
        {
            if (plugin != null)
            {
                // Change mode
                plugin.AddTimer(delay, () => 
                {
                    // Set next mode and map
                    pluginState.NextMap = map;
                    pluginState.NextMode = mode;

                    // Check for scheduled warmup
                    if (!ScheduleWarmup(pluginState))
                    {
                        // Execute mode config
                        Server.ExecuteCommand($"exec {mode.Config}");

                        // Set current mode
                        pluginState.CurrentMode = mode; 
                    }
                    
                    // Change map
                    ChangeMap(map, config, plugin, pluginState);

                }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            }
        }

        // Define method to trigger mode and map rotations
        public static void TriggerRotation(Plugin plugin,Config config, PluginState pluginState, ILogger logger, StringLocalizer localizer)
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
                    ChangeMode(_randomMode, config, plugin, pluginState, config.GameModes.Delay);
                }
                else // Change map
                {
                    // Define random map
                    Map _randomMap = GetRandomMap(config, pluginState);

                    // Change map
                    Server.PrintToChatAll(localizer.LocalizeWithPrefix("Game has ended. Changing map..."));
                    ChangeMap(_randomMap, config, plugin, pluginState);
                }
                pluginState.MapRotations++;
            }
        }

        // Define method to trigger schedule change
        public static void TriggerScheduleChange(Plugin plugin, ScheduleEntry state, PluginState pluginState, Config config)
        {
            // Find mode
            Mode? _mode = pluginState.Modes.FirstOrDefault(m => m.Name.Equals(state.Mode, StringComparison.OrdinalIgnoreCase));

            // Change mode
            if (_mode != null)
            {
                // Check if current mode is different from target mode
                if (pluginState.CurrentMode != _mode)
                {
                    ChangeMode(_mode, config, plugin, pluginState, config.GameModes.Delay);
                }
            }
        }

        // Define reusable method to get random map
        public static Map GetRandomMap(Config config, PluginState pluginState)
        {    
            // Define random map
            Map _randomMap; 

            // Set random map
            if (config.Rotation.Cycle == 2) // If cycle = 2, select from specified map groups in config
            {
                // Create map list
                List<Map> _mapList = new List<Map>();

                foreach (string mapGroup in config.Rotation.MapGroups)
                {
                    // Find map group
                    MapGroup? _mapGroup = pluginState.MapGroups.FirstOrDefault(m => m.Name.Equals(mapGroup, StringComparison.OrdinalIgnoreCase));

                    // add maps from map group to map list
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
                // Get a random map from all registered maps
                Random _rnd = new Random();
                int _randomIndex = _rnd.Next(0, pluginState.Maps.Count); 
                _randomMap = pluginState.Maps[_randomIndex];
            
            }
            else // Select from current mode
            {
                // Get a random map from current game mode
                Random _rnd = new Random();
                int _randomIndex = _rnd.Next(0, pluginState.CurrentMode.Maps.Count); 
                _randomMap = pluginState.CurrentMode.Maps[_randomIndex];
            }
            return _randomMap;
        }
    }
}