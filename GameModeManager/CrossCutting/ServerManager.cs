// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Models;
using GameModeManager.Contracts;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.CrossCutting
{
    // Define class
    public class ServerManager : IPluginDependency<Plugin, Config>
    {
        // Define Dependencies
        private Plugin? _plugin;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private ILogger<ServerManager> _logger;

        // Define class instance
        public ServerManager(PluginState pluginState, ILogger<ServerManager> logger, StringLocalizer localizer)
        {
            _logger = logger;
            _localizer = localizer;
            _pluginState = pluginState;
        }

        // Load config
         public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define On Load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
        }

        // Define reusable method to change map
        public void ChangeMap(Map nextMap)
        {
            // Change map
            if (_plugin != null)
            {
                _plugin.AddTimer(_config.Maps.Delay, () => 
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
        }

        // Define reusable method to change map
        public void ChangeMap(Map nextMap, float delay)
        {
            // Change map
            if (_plugin != null)
            {
                _plugin.AddTimer(delay, () => 
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
        }

        // Define reusable method to change mode
        public void ChangeMode(Mode mode)
        {
            if (_plugin != null)
            {
                // Change mode
                _plugin.AddTimer(_config.GameModes.Delay, () => 
                {
                    // Execute mode config
                    Server.ExecuteCommand($"exec {mode.Config}");

                }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            }
        }

        // Define reusable method to change mode with desired map
        public void ChangeMode(Mode mode, float delay)
        {
            if (_plugin != null)
            {
                // Disable warmup scheduler
                _pluginState.WarmupScheduled = false;

                // Change mode
                _plugin.AddTimer(delay, () => 
                {
                    // Execute mode config
                    Server.ExecuteCommand($"exec {mode.Config}");

                }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            }
        }

        // Define method to trigger mode and map rotations
        public void TriggerRotation()
        {  
            // Check if rotations are enabled
            if(_config.Rotation.Enabled)
            {
                // If mode rotations are enabled, change mode on mode interval
                if (_config.Rotation.ModeRotation && _pluginState.MapRotations != 0 && _pluginState.MapRotations % _config.Rotation.ModeInterval == 0)
                {  
                    // Log information
                    _logger.LogInformation("Game has ended. Picking random game mode...");
            
                    // Get random game mode
                    Random _rnd = new Random();
                    int _randomIndex = _rnd.Next(0, _pluginState.Modes.Count); 
                    Mode _randomMode = _pluginState.Modes[_randomIndex];

                    // Change mode
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("Game has ended. Changing mode..."));  
                    ChangeMode(_randomMode);
                }
                else // Change map
                {
                    // Define random map
                    Map _randomMap = GetRandomMap();

                    // Change map
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("Game has ended. Changing map..."));
                    ChangeMap(_randomMap);
                }
                _pluginState.MapRotations++;
            }
        }

        // Define method to trigger schedule change
        public void TriggerScheduleChange(ScheduleEntry state)
        {
            // Find mode
            Mode? _mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(state.Mode, StringComparison.OrdinalIgnoreCase));

            // Change mode
            if (_mode != null)
            {
                // Check if current mode is different from target mode
                if (_pluginState.CurrentMode != _mode)
                {
                    ChangeMode(_mode);
                }
            }
        }

        // Define reusable method to get random map
        public Map GetRandomMap()
        {    
            // Define random map
            Map _randomMap; 

            // Set random map
            if (_config.Rotation.Cycle == 2) // If cycle = 2, select from specified map groups in config
            {
                // Create map list
                List<Map> _mapList = new List<Map>();

                foreach (string mapGroup in _config.Rotation.MapGroups)
                {
                    // Find map group
                    MapGroup? _mapGroup = _pluginState.MapGroups.FirstOrDefault(m => m.Name.Equals(mapGroup, StringComparison.OrdinalIgnoreCase));

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
            else if (_config.Rotation.Cycle == 1) // If cycle = 1, select from all maps
            {
                // Get a random map from all registered maps
                Random _rnd = new Random();
                int _randomIndex = _rnd.Next(0, _pluginState.Maps.Count); 
                _randomMap = _pluginState.Maps[_randomIndex];
            
            }
            else // Select from current mode
            {
                // Get a random map from current game mode
                Random _rnd = new Random();
                int _randomIndex = _rnd.Next(0, _pluginState.CurrentMode.Maps.Count); 
                _randomMap = _pluginState.CurrentMode.Maps[_randomIndex];
            }
            return _randomMap;
        }
    }
}