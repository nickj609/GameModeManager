// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class EnforceTimeLimitCommand : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin? _plugin;
        private GameRules _gameRules;
        private PluginState _pluginState;
        private IStringLocalizer _iLocalizer;
        private Config _config = new Config();
        private MaxRoundsManager _maxRoundsManager;
        private TimeLimitManager _timeLimitManager;

        // Define class instance
        public EnforceTimeLimitCommand(TimeLimitManager timeLimitManager, PluginState pluginState, GameRules gameRules, IStringLocalizer localizer, MaxRoundsManager maxRoundsManager)
        {
            _gameRules = gameRules;
            _iLocalizer = localizer;
            _pluginState = pluginState;
            _maxRoundsManager = maxRoundsManager;
            _timeLimitManager = timeLimitManager;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            _plugin.AddCommand("css_enforcetimelimit", "Forces rotation on time limit end. Default time limit is mp_timelimit.", OnEnforceTimeLimitCommand);

            if (_config.Commands.TimeLeft)
            {
                plugin.AddCommand("timeleft", "Prints in the chat the timeleft in the current map", OnTimeLimitCommand);
            }
        }

        // Define admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "<true|false> optional: [seconds]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void OnEnforceTimeLimitCommand(CCSPlayerController? player, CommandInfo command)
        {
            // Validate argument count
            if (command.ArgCount < 1 || command.ArgCount > 2)
            {
                // Provide informative error message
                command.ReplyToCommand("Invalid usage. Please use: /enforcetimelimit <true|false> [seconds]");
                return;
            }

            // Parse boolean argument
            bool enforceTimeLimit = bool.TryParse(command.ArgByIndex(1), out var parsedValue) ? parsedValue : false;

            // Handle time limit enforcement or removal
            if (enforceTimeLimit)
            {
                // Check if a time limit is provided
                if (command.ArgCount == 2)
                {
                    // Parse integer argument, handling potential exceptions
                    if (int.TryParse(command.ArgByIndex(2), out var seconds))
                    {
                        if (_pluginState.TimeLimitEnabled == true)
                        {
                            string _message = "Time limit is already enabled. " + GetTimeLimitMessage();
                            command.ReplyToCommand(_message);
                        }
                        else
                        {
                            if (player == null)
                            {
                                _pluginState.TimeLimit = seconds;
                                _pluginState.TimeLimitCustom = true;
                                _pluginState.TimeLimitScheduled = true;
                            }
                            else
                            {
                                _timeLimitManager.EnforceTimeLimit(seconds);
                            }
                        }
                    }
                    else
                    {
                        // Provide informative error message
                        command.ReplyToCommand("Invalid time limit. Please enter a valid integer.");
                    }
                }
                else
                {
                    if (_pluginState.TimeLimitEnabled == true)
                    {
                        string _message = "Time limit is already enabled. " + GetTimeLimitMessage();
                        command.ReplyToCommand(_message);
                    }
                    else
                    {
                        if (player == null)
                        {
                            _pluginState.TimeLimitScheduled = true;
                        }
                        else
                        {
                            if (_timeLimitManager.TimePlayed != 0 && _timeLimitManager.TimeRemaining != 0)
                            {
                                _timeLimitManager.EnforceTimeLimit();
                            }
                            else
                            {
                                string _message = GetTimeLimitMessage();
                                command.ReplyToCommand(_message);
                            }
                        }
                    }
                }
            }
            else
            {
                if(_pluginState.TimeLimitEnabled == true)
                {
                    _timeLimitManager.RemoveTimeLimit();
                    command.ReplyToCommand("Time limit is disabled.");
                }
                else
                {
                    command.ReplyToCommand("Time limit is already disabled.");
                }
            }
        }

        // Define time limit command handler
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnTimeLimitCommand(CCSPlayerController? player, CommandInfo command)
        {
            // Get timelimit messagge
            string message = GetTimeLimitMessage();

            // Send message    
            if (player != null)
            {
                command.ReplyToCommand(message);
            }
            else
            {
                command.ReplyToCommand(message);
            }
        }
        public string GetTimeLimitMessage()
        {
            // Define message
            string _message;

            // Define prefix
            StringLocalizer _timeLocalizer = new StringLocalizer(_iLocalizer, "timeleft.prefix");

            // If warmup, send general message
            if (_gameRules.WarmupRunning)
            {
                return _timeLocalizer.LocalizeWithPrefix("timeleft.warmup");
            }

            // Create message based on map conditions
            if (!_timeLimitManager.UnlimitedTime) // If time not over
            {
                if (_timeLimitManager.TimeRemaining > 1)
                {
                    // Get remaining time
                    TimeSpan remaining = TimeSpan.FromSeconds((double)_timeLimitManager.TimeRemaining);

                    // If hours left
                    if (remaining.Hours > 0)
                    {
                        _message = _timeLocalizer.LocalizeWithPrefix("timeleft.remaining-time-hour", remaining.Hours.ToString("00"), remaining.Minutes.ToString("00"), remaining.Seconds.ToString("00"));
                    }
                    else if (remaining.Minutes > 0) // If minutes left
                    {
                        _message = _timeLocalizer.LocalizeWithPrefix("timeleft.remaining-time-minute", remaining.Minutes, remaining.Seconds);
                    }
                    else // If seconds left
                    {
                        _message = _timeLocalizer.LocalizeWithPrefix("timeleft.remaining-time-second", remaining.Seconds);
                    }
                }
                else // If time over
                {
                    _message = _timeLocalizer.LocalizeWithPrefix("timeleft.remaining-time-over");
                }
            }
            else if (!_maxRoundsManager.UnlimitedRounds) // If round limit not reached
            {
                if (_maxRoundsManager.RemainingRounds > 1) // If remaining rounds more than 1
                {
                    _message = _timeLocalizer.LocalizeWithPrefix("timeleft.remaining-rounds", _maxRoundsManager.RemainingRounds);
                }
                else // If last round
                {
                    _message = _timeLocalizer.LocalizeWithPrefix("timeleft.last-round");
                }
            }
            else // If no time or round limit
            {
                _message = _timeLocalizer.LocalizeWithPrefix("timeleft.no-time-limit");
            }

            // Return message
            return _message;
        }
    }
}