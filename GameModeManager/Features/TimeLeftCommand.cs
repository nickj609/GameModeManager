// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class TimeLeftCommand : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Config _config = new();
        private StringLocalizer _localizer;
        private readonly GameRules _gameRules;
        private TimeLimitManager _timeLimitManager;
        private MaxRoundsManager _maxRoundsManager;

        // Define class instance
        public TimeLeftCommand(TimeLimitManager timeLimitManager, MaxRoundsManager maxRoundsManager, GameRules gameRules, IStringLocalizer stringLocalizer)
        {
            _gameRules = gameRules;
            _timeLimitManager = timeLimitManager;
            _maxRoundsManager = maxRoundsManager;
            _localizer = new StringLocalizer(stringLocalizer, "timeleft.prefix");
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            if (_config.Commands.TimeLeft)
            {
                plugin.AddCommand("timeleft", "Prints in the chat the timeleft in the current map", CommandHandler);
            }
        }

        // Define command helper
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void CommandHandler(CCSPlayerController? player, CommandInfo command)
        {
            // Define message
            string _message;

            // If warmup, send general message
            if (_gameRules.WarmupRunning)
            {
                if (player != null)
                {
                    command.ReplyToCommand(_localizer.LocalizeWithPrefix("timeleft.warmup"));
                }
                else
                {
                    command.ReplyToCommand(_localizer.LocalizeWithPrefix("timeleft.warmup"));
                }
                
                return;
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
                        _message = _localizer.LocalizeWithPrefix("timeleft.remaining-time-hour", remaining.Hours.ToString("00"), remaining.Minutes.ToString("00"), remaining.Seconds.ToString("00"));
                    }
                    else if (remaining.Minutes > 0) // If minutes left
                    {
                        _message = _localizer.LocalizeWithPrefix("timeleft.remaining-time-minute", remaining.Minutes, remaining.Seconds);
                    }
                    else // If seconds left
                    {
                        _message = _localizer.LocalizeWithPrefix("timeleft.remaining-time-second", remaining.Seconds);
                    }
                }
                else // If time over
                {
                    _message = _localizer.LocalizeWithPrefix("timeleft.remaining-time-over");
                }
            }
            else if (!_maxRoundsManager.UnlimitedRounds) // If round limit not reached
            {
                if (_maxRoundsManager.RemainingRounds > 1) // If remaining rounds more than 1
                {
                    _message = _localizer.LocalizeWithPrefix("timeleft.remaining-rounds", _maxRoundsManager.RemainingRounds);
                }
                else // If last round
                {
                    _message = _localizer.LocalizeWithPrefix("timeleft.last-round");
                }
            }
            else // If no time or round limit
            {
                _message = _localizer.LocalizeWithPrefix("timeleft.no-time-limit");
            }

            // Send message    
            if (player != null)
            {
                command.ReplyToCommand(_message);
            }
            else
            {
                command.ReplyToCommand(_message);
            }
        }
    }
}