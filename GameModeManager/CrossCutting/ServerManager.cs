// Included libraries
using GameModeManager.Timers;
using CounterStrikeSharp.API;
using GameModeManager.Models;
using GameModeManager.Contracts;
using Microsoft.Extensions.Logging;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.CrossCutting
{
    // Define class
    public class ServerManager : IPluginDependency<Plugin, Config>
    {
        // Define Dependencies
        private GameRules _gameRules;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private ILogger<ServerManager> _logger;

        // Define class instance
        public ServerManager(PluginState pluginState, ILogger<ServerManager> logger, StringLocalizer localizer, GameRules gameRules)
        {
            _logger = logger;
            _localizer = localizer;
            _gameRules =  gameRules;
            _pluginState = pluginState;
        }

        // Load config
         public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define reusable method to change map
        public void ChangeMap(Map nextMap)
        {
            if(_gameRules.WarmupRunning)
            {
                // End warmup
                Server.ExecuteCommand($"mp_warmup_end");
                
                // Delay freeze
                new Timer(1.5f, () =>
                {
                    Server.ExecuteCommand("bot_kick");
                    FreezePlayers();
                });
            }
            else
            {
                // Kick bots and freeze all players
                Server.ExecuteCommand("bot_kick");
                FreezePlayers();
            }

            // Disable warmup scheduler
            if (_config.Warmup.PerMap)
            {
                _pluginState.WarmupScheduled = false;
            }

            // Freeze all players
            FreezePlayers();

            // Display Countdown
            CountdownTimer timer = new CountdownTimer(_config.Maps.Delay, () => 
            {
                // Change map
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
            }, "Map changing in ");
        }

        // Define reusable method to change map
        public void ChangeMap(Map nextMap, int delay)
        {
            if(_gameRules.WarmupRunning)
            {
                // End warmup
                Server.ExecuteCommand($"mp_warmup_end");

                // Delay freeze
                new Timer(1.3f, () =>
                {
                    Server.ExecuteCommand("bot_kick");
                    FreezePlayers();
                });
            }
            else
            {
                // Kick bots and freeze all players
                Server.ExecuteCommand("bot_kick");
                FreezePlayers();
            }

            // Disable warmup scheduler
            if (_config.Warmup.PerMap)
            {
                _pluginState.WarmupScheduled = false;
            }

            // Display Countdown
            CountdownTimer timer = new CountdownTimer(delay, () => 
            {
                // Change map
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
            }, "Map changing in ");

        }

        // Define reusable method to change mode
        public void ChangeMode(Mode mode)
        {
            // Disable warmup scheduler
            _pluginState.WarmupScheduled = false;

            // Execute mode config
            Server.ExecuteCommand($"exec {mode.Config}");

            // If no default map, set next map to random map
            Map nextMap;
            if (mode.DefaultMap == null) 
            {
                nextMap = GetRandomMap(mode);
            }
            else
            {
                nextMap = mode.DefaultMap;
            }

            // Change to next map
            ChangeMap(nextMap);
        }

        // Define method to trigger mode and map rotations
        public void TriggerRotation()
        {  
            // Check if rotations are enabled
            if(_pluginState.RotationsEnabled)
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
                    Map _randomMap = GetRandomMap(_pluginState.CurrentMode);

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
        public Map GetRandomMap(Mode currentMode)
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

                    // Add maps from map group to map list
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
                int _randomIndex = _rnd.Next(0, currentMode.Maps.Count); 
                _randomMap = currentMode.Maps[_randomIndex];
            }
            return _randomMap;
        }

        // Define reusable method to freeze all players
        public void FreezePlayers()
		{
            foreach (var player in Extensions.ValidPlayers(true))
            {
			    player.Pawn.Value!.Freeze();
            }
		}

        // Define reusable method to unfreeze all players
        public void UnfreezePlayers()
		{
            foreach (var player in Extensions.ValidPlayers(true))
            {
                player.Pawn.Value!.Unfreeze();
            }
		}
    }
}