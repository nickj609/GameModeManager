// Included libraries
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager
{
    // Define class
    public static class ServerManager
    {
        // Define reusable method to change map
        public static void ChangeMap(Map nextMap, Config config, PluginState pluginState)
        {
            // If random default map for mode
            if (nextMap.Name.Equals("pseudorandom", StringComparison.CurrentCultureIgnoreCase))
            {
                nextMap = GetRandomMap(config, pluginState);
            }

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
                    // Execute mode config
                    if (!ScheduleWarmup(pluginState))
                    {
                        Server.ExecuteCommand($"exec {mode.Config}");
                    }
                    
                    // Change map
                    ChangeMap(mode.DefaultMap, config, pluginState);

                }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

                // Set current mode
                pluginState.CurrentMode = mode; 
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
                    ChangeMap(_randomMap, config, pluginState);
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