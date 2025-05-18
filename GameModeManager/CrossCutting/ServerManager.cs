// Included libraries
using GameModeManager.Core;
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;
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
        private VoteOptionManager _voteOptionManager;

        // Define class instance
        public ServerManager(PluginState pluginState, ILogger<ServerManager> logger, StringLocalizer localizer, VoteOptionManager voteOptionManager)
        {
            _logger = logger;
            _localizer = localizer;
            _pluginState = pluginState;
            _voteOptionManager = voteOptionManager;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define class methods
        public void ChangeMap(IMap nextMap, int delay)
        {
            // Disable warmup
            _pluginState.Game.WarmupRunning = false;
            if(_config.Warmup.PerMap)
            {
                _pluginState.Game.WarmupScheduled = false;
            }

            // Kick bots and freeze all players
            new Timer(0.5f, () =>
            {
                Server.ExecuteCommand("bot_kick");
                PlayerExtensions.FreezePlayers();
            });

            // Revert RTV settings
            if (_pluginState.RTV.Enabled && _config.RTV.PerMap)
            {
                _pluginState.RTV.EndOfMapVote = _config.RTV.EndOfMapVote;
                _pluginState.RTV.Duration = _config.RTV.VoteDuration;
                _pluginState.RTV.ChangeImmediately = _config.RTV.ChangeImmediately;
                _pluginState.RTV.KillsBeforeEnd = _config.RTV.TriggerKillsBeforeEnd;
                _pluginState.RTV.RoundsBeforeEnd = _config.RTV.TriggerRoundsBeforeEnd;
                _pluginState.RTV.SecondsBeforeEnd = _config.RTV.TriggerSecondsBeforeEnd;

                if(_pluginState.RTV.NominationEnabled)
                {
                    _pluginState.RTV.MaxNominationWinners = _config.RTV.MaxNominationWinners;
                }
            }

            // Display Countdown
            _pluginState.Game.CountdownRunning = true;
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
                _pluginState.Game.CountdownRunning = false;

            }, "Map changing in ");
        }

        public void ChangeMode(IMode mode)
        {
            _logger.LogInformation($"Current mode: {_pluginState.Game.CurrentMode.Name}");
            _logger.LogInformation($"New mode: {mode.Name}");

            if (_pluginState.RTV.Enabled)
            {
                // Revert RTV settings
                _pluginState.RTV.EndOfMapVote = _config.RTV.EndOfMapVote;
                _pluginState.RTV.Duration = _config.RTV.VoteDuration;
                _pluginState.RTV.ChangeImmediately = _config.RTV.ChangeImmediately;
                _pluginState.RTV.KillsBeforeEnd = _config.RTV.TriggerKillsBeforeEnd;
                _pluginState.RTV.RoundsBeforeEnd = _config.RTV.TriggerRoundsBeforeEnd;
                _pluginState.RTV.SecondsBeforeEnd = _config.RTV.TriggerSecondsBeforeEnd;

                // Add mode to cooldown
                if(_config.RTV.IncludeModes)
                {
                    if (_pluginState.RTV.InCoolDown == 0)
                    {
                        _pluginState.RTV.OptionsOnCoolDown.Clear();
                    }
                    else
                    {
                        if (_pluginState.RTV.OptionsOnCoolDown.Count > _pluginState.RTV.InCoolDown)
                        {
                            _pluginState.RTV.OptionsOnCoolDown.RemoveAt(0);
                        }
                        _pluginState.RTV.OptionsOnCoolDown.Add(mode.Name.Trim());
                    }
                }

                if(_pluginState.RTV.NominationEnabled)
                {
                    _pluginState.RTV.MaxNominationWinners = _config.RTV.MaxNominationWinners;
                }
            }

            // Execute mode config
            Server.ExecuteCommand($"exec {mode.Config}");

            // If no default map, set next map to random map
            IMap nextMap;
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

        public void TriggerRotation()
        {
            if (_pluginState.Game.RotationsEnabled)
            {
                if (_config.Rotation.ModeRotation && _pluginState.Game.MapRotations != 0 && _pluginState.Game.MapRotations % _config.Rotation.ModeInterval == 0)
                {
                    Random _rnd = new Random();
                    int _randomIndex = _rnd.Next(0, _pluginState.Game.Modes.Count);
                    IMode _randomMode = _pluginState.Game.Modes[_randomIndex];
                    _logger.LogDebug("Game has ended. Picking random game mode...");
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rotation.change-mode", _randomMode.Name));
                    ChangeMode(_randomMode);
                }
                else
                {
                    IMap _randomMap = GetRandomMap(_pluginState.Game.CurrentMode);
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rotation.change-map", _randomMap.DisplayName));
                    ChangeMap(_randomMap, _config.Maps.Delay);
                }
                _pluginState.Game.MapRotations++;
            }
            else if (_pluginState.RTV.Enabled)
            {
                if (_pluginState.RTV.EofVoteHappened && !_pluginState.RTV.ChangeImmediately)
                {
                    if (_pluginState.RTV.NextMode != null)
                    {
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rotation.change-mode", _pluginState.RTV.NextMode.Name));
                        ChangeMode(_pluginState.RTV.NextMode);
                    }
                    else if (_pluginState.RTV.NextMap != null)
                    {
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rotation.change-map", _pluginState.RTV.NextMap.DisplayName));
                        ChangeMap(_pluginState.RTV.NextMap, _config.Maps.Delay);
                    }
                }
                else
                {
                    // If no vote occurred, pick a random option based on RTV settings
                    _pluginState.RTV.IncludeExtend = false;
                    _voteOptionManager.LoadOptions();
                    List<string> options = _voteOptionManager.GetOptions();
                    string randomOption = options[new Random().Next(0, options.Count)];

                    if (_voteOptionManager.OptionType(_pluginState.RTV.Winner) == "mode")
                    {
                        _pluginState.RTV.NextMode = _pluginState.Game.Modes.FirstOrDefault(m => m.Name.Equals(_pluginState.RTV.Winner, StringComparison.OrdinalIgnoreCase));

                        if (_pluginState.RTV.NextMode?.DefaultMap != null)
                        {
                            _pluginState.RTV.NextMap = _pluginState.RTV.NextMode.DefaultMap;
                        }

                        if (_pluginState.RTV.NextMode != null)
                        {
                            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rotation.change-mode", randomOption));
                            ChangeMode(_pluginState.RTV.NextMode);
                            return;
                        }
                    }
                    else if (_voteOptionManager.OptionType(_pluginState.RTV.Winner) == "map")
                    {
                        _pluginState.RTV.NextMap = _pluginState.Game.Maps.FirstOrDefault(m => m.DisplayName.Equals(_pluginState.RTV.Winner, StringComparison.OrdinalIgnoreCase) || m.Name.Equals(_pluginState.RTV.Winner, StringComparison.OrdinalIgnoreCase));

                        if (_pluginState.RTV.NextMap != null)
                        {
                            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rotation.change-map", randomOption));
                            ChangeMap(_pluginState.RTV.NextMap, _config.Maps.Delay);
                        }
                        return;
                    }
                    else
                    {
                        _logger.LogError($"RTV: Map or mode {_pluginState.RTV.Winner} not found");
                    }
                }
            }
        }

        public void TriggerScheduleChange(ScheduleEntry state)
        {
            IMode? _mode = _pluginState.Game.Modes.FirstOrDefault(m => m.Name.Equals(state.Mode, StringComparison.OrdinalIgnoreCase));

            if (_mode != null && _pluginState.Game.CurrentMode != _mode)
            {
                ChangeMode(_mode);
            }
        }

        public IMap GetRandomMap(IMode currentMode)
        {    
            IMap _randomMap; 

            if (_config.Rotation.Cycle == 2)
            {
                List<IMap> _mapList = new List<IMap>();

                foreach (string mapGroup in _config.Rotation.MapGroups)
                {
                    IMapGroup? _mapGroup = _pluginState.Game.MapGroups.FirstOrDefault(m => m.Name.Equals(mapGroup, StringComparison.OrdinalIgnoreCase));

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
                int _randomIndex = _rnd.Next(0, _pluginState.Game.Maps.Count); 
                _randomMap = _pluginState.Game.Maps[_randomIndex];
            
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