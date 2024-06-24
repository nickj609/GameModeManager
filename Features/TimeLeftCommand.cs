// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;

// Declare namespace
namespace GameModeManager
{
    // Define class
    public class TimeLeftCommand : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private TimeLimitManager _timeLimitManager;
        private MaxRoundsManager _maxRoundsManager;
        private readonly GameRules _gameRules;
        private StringLocalizer _localizer;
        private Config _config = new();

        // Define class instance
        public TimeLeftCommand(TimeLimitManager timeLimitManager, MaxRoundsManager maxRoundsManager, GameRules gameRules, IStringLocalizer stringLocalizer)
        {
            _gameRules = gameRules;
            _localizer = new StringLocalizer(stringLocalizer, "timeleft.prefix");
            _timeLimitManager = timeLimitManager;
            _maxRoundsManager = maxRoundsManager;

        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            if (!_config.RTV.Enabled)
            {
                plugin.AddCommand("timeleft", "Prints in the chat the timeleft in the current map", CommandHandler);
            }
        }

        // Define command helper
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void CommandHandler(CCSPlayerController? player, CommandInfo command)
        {
            string text;

            if (_gameRules.WarmupRunning)
            {
                if (player is not null)
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                else
                    Server.PrintToConsole(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                return;
            }

            if (!_timeLimitManager.UnlimitedTime)
            {
                if (_timeLimitManager.TimeRemaining > 1)
                {
                    TimeSpan remaining = TimeSpan.FromSeconds((double)_timeLimitManager.TimeRemaining);
                    if (remaining.Hours > 0)
                    {
                        text = _localizer.LocalizeWithPrefix("timeleft.remaining-time-hour", remaining.Hours.ToString("00"), remaining.Minutes.ToString("00"), remaining.Seconds.ToString("00"));
                    }
                    else if (remaining.Minutes > 0)
                    {
                        text = _localizer.LocalizeWithPrefix("timeleft.remaining-time-minute", remaining.Minutes, remaining.Seconds);
                    }
                    else
                    {
                        text = _localizer.LocalizeWithPrefix("timeleft.remaining-time-second", remaining.Seconds);
                    }
                }
                else
                {
                    text = _localizer.LocalizeWithPrefix("timeleft.time-over");
                }
            }
            else if (!_maxRoundsManager.UnlimitedRounds)
            {
                if (_maxRoundsManager.RemainingRounds > 1)
                    text = _localizer.LocalizeWithPrefix("timeleft.remaining-rounds", _maxRoundsManager.RemainingRounds);
                else
                    text = _localizer.LocalizeWithPrefix("timeleft.last-round");
            }
            else
            {
                text = _localizer.LocalizeWithPrefix("timeleft.no-time-limit");
            }

            
            if (player is not null)
            {
                player.PrintToChat(text);
            }
            else
            {
                Server.PrintToConsole(text);
            }
        }
    }
}