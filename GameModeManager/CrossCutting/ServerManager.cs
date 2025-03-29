// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Models;
using GameModeManager.Contracts;
using Microsoft.Extensions.Logging;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using CountdownTimer = GameModeManager.Timers.CountdownTimer;

// Declare namespace
namespace GameModeManager.CrossCutting
{
    // Define class
    public class ServerManager : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
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

        // Define method to change map
        public void ChangeMap(Map nextMap, int delay)
        {
            // Disable warmup
            _pluginState.WarmupRunning = false;
            if(_config.Warmup.PerMap)
            {
                _pluginState.WarmupScheduled = false;
            }

            // Kick bots and freeze all players
            new Timer(0.5f, () =>
            {
                Server.ExecuteCommand("bot_kick");
                Extensions.FreezePlayers();
            });

            // Revert RTV settings
            if (_pluginState.RTVEnabled && _config.RTV.PerMap)
            {
                _pluginState.EndOfMapVote = _config.RTV.EndMapVote;
                _pluginState.RTVDuration = _config.RTV.VoteDuration;
                _pluginState.ChangeImmediately = _config.RTV.ChangeImmediately;
                _pluginState.RTVRoundsBeforeEnd = _config.RTV.TriggerRoundsBeforeEnd;
                _pluginState.RTVSecondsBeforeEnd = _config.RTV.TriggerSecondsBeforeEnd;

                if(_pluginState.NominationEnabled)
                {
                    _pluginState.MaxNominationWinners = _config.RTV.MaxNominationWinners;
                }
            }

            // Display Countdown
            _pluginState.CountdownRunning = true;
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
                // Disable countdown flag
                _pluginState.CountdownRunning = false;

            }, "Map changing in ");
        }

        // Define method to change mode
        public void ChangeMode(Mode mode)
        {
            _logger.LogInformation($"Current mode: {_pluginState.CurrentMode.Name}");
            _logger.LogInformation($"New mode: {mode.Name}");

            if (_pluginState.RTVEnabled)
            {
                // Revert RTV settings
                _pluginState.EndOfMapVote = _config.RTV.EndMapVote;
                _pluginState.RTVDuration = _config.RTV.VoteDuration;
                _pluginState.ChangeImmediately = _config.RTV.ChangeImmediately;
                _pluginState.RTVRoundsBeforeEnd = _config.RTV.TriggerRoundsBeforeEnd;
                _pluginState.RTVSecondsBeforeEnd = _config.RTV.TriggerSecondsBeforeEnd;

                // Add mode to cooldown
                if(_config.RTV.IncludeModes)
                {
                    if (_pluginState.InCoolDown == 0)
                    {
                        _pluginState.OptionsOnCoolDown.Clear();
                    }
                    else
                    {
                        if (_pluginState.OptionsOnCoolDown.Count > _pluginState.InCoolDown)
                        {
                            _pluginState.OptionsOnCoolDown.RemoveAt(0);
                        }
                        _pluginState.OptionsOnCoolDown.Add(mode.Name.Trim());
                    }
                }

                if(_pluginState.NominationEnabled)
                {
                    _pluginState.MaxNominationWinners = _config.RTV.MaxNominationWinners;
                }
            }

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
            
            ChangeMap(nextMap, _config.Maps.Delay);
        }

        // Define method to trigger mode and map rotations
        public void TriggerRotation()
        {  
            if(_pluginState.RotationsEnabled)
            {
                if (_config.Rotation.ModeRotation && _pluginState.MapRotations != 0 && _pluginState.MapRotations % _config.Rotation.ModeInterval == 0)
                {  
                    Random _rnd = new Random();
                    int _randomIndex = _rnd.Next(0, _pluginState.Modes.Count); 
                    Mode _randomMode = _pluginState.Modes[_randomIndex];
                    _logger.LogInformation("Game has ended. Picking random game mode...");
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("Game has ended. Changing mode..."));  
                    ChangeMode(_randomMode);
                }
                else
                {
                    Map _randomMap = GetRandomMap(_pluginState.CurrentMode);
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("Game has ended. Changing map..."));
                    ChangeMap(_randomMap, _config.Maps.Delay);
                }
                _pluginState.MapRotations++;
            }
            else if (_pluginState.RTVEnabled)
            {
                if (_pluginState.EofVoteHappened && !_pluginState.ChangeImmediately)
                {
                    if (_pluginState.NextMode != null)
                    {
                        ChangeMode(_pluginState.NextMode); 
                    }
                    else if(_pluginState.NextMap != null)
                    {
                        ChangeMap(_pluginState.NextMap, _config.Maps.Delay);
                    }
                }
            }
        }

        // Define method to trigger schedule change
        public void TriggerScheduleChange(ScheduleEntry state)
        {
            Mode? _mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(state.Mode, StringComparison.OrdinalIgnoreCase));

            if (_mode != null && _pluginState.CurrentMode != _mode)
            {
                ChangeMode(_mode);
            }
        }

        // Define method to get random map
        public Map GetRandomMap(Mode currentMode)
        {    
            Map _randomMap; 

            if (_config.Rotation.Cycle == 2)
            {
                List<Map> _mapList = new List<Map>();

                foreach (string mapGroup in _config.Rotation.MapGroups)
                {
                    MapGroup? _mapGroup = _pluginState.MapGroups.FirstOrDefault(m => m.Name.Equals(mapGroup, StringComparison.OrdinalIgnoreCase));

                    if (_mapGroup != null)
                    {
                        foreach (Map _map in _mapGroup.Maps)
                        {
                            _mapList.Add(_map);
                        }
                    }
                } 

                Random _rnd = new Random();
                int _randomIndex = _rnd.Next(0, _mapList.Count); 
                _randomMap = _mapList[_randomIndex];

            }
            else if (_config.Rotation.Cycle == 1)
            {
                Random _rnd = new Random();
                int _randomIndex = _rnd.Next(0, _pluginState.Maps.Count); 
                _randomMap = _pluginState.Maps[_randomIndex];
            
            }
            else
            {
                Random _rnd = new Random();
                int _randomIndex = _rnd.Next(0, currentMode.Maps.Count); 
                _randomMap = currentMode.Maps[_randomIndex];
            }
            return _randomMap;
        }
    }
}