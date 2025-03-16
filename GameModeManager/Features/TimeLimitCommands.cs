// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class TimeLimitCommands : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private Config _config = new Config();
        private ILogger<TimeLimitCommands> _logger;
        private StringLocalizer _timeLeftLocalizer;
        private TimeLimitManager _timeLimitManager;
        private StringLocalizer _timeLimitLocalizer;

        // Define class instance
        public TimeLimitCommands(TimeLimitManager timeLimitManager, PluginState pluginState, IStringLocalizer iLocalizer, ILogger<TimeLimitCommands> logger)
        {
            _logger = logger;
            _pluginState = pluginState;
            _timeLimitManager = timeLimitManager;
            _timeLeftLocalizer = new StringLocalizer(iLocalizer, "timeleft.prefix");
            _timeLimitLocalizer = new StringLocalizer(iLocalizer, "timelimit.prefix");
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            if (_config.Commands.TimeLimit)
            {
                plugin.AddCommand("timelimit", "Forces rotation on time limit end. Default time limit is mp_timelimit.", OnTimeLimitCommand);
            }
            if (_config.Commands.TimeLeft)
            {
                plugin.AddCommand("timeleft", "Prints in the chat the timeleft in the current map", OnTimeLeftCommand);
            }
        }

        // Define admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "<true|false> optional: [seconds]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void OnTimeLimitCommand(CCSPlayerController? player, CommandInfo command)
        {
            // Parse boolean argument
            bool timeLimit = bool.TryParse(command.ArgByIndex(1), out var parsedValue) ? parsedValue : false;

            if (timeLimit)
            {
                // Check if a time limit is provided
                if (command.ArgCount == 3)
                {
                    // Parse integer argument, handling potential exceptions
                    if (int.TryParse(command.ArgByIndex(2), out var seconds))
                    {
                        if (_pluginState.TimeLimitEnabled == true)
                        {
                            command.ReplyToCommand(_timeLimitLocalizer.LocalizeWithPrefix("timelimit.enabled-error"));
                        }
                        else
                        {
                            if (player == null)
                            {
                                _pluginState.TimeLimit = seconds;
                                _pluginState.TimeLimitCustom = true;
                                _pluginState.TimeLimitScheduled = true;
                                Server.ExecuteCommand($"mp_timelimit {seconds}");
                                _logger.LogInformation($"Time limit scheduled: mp_timelimit = {_timeLimitManager.TimeRemaining} seconds. Custom time limit = {seconds} seconds");
                            }
                            else
                            {
                                _timeLimitManager.EnableTimeLimit(seconds);
                                command.ReplyToCommand(_timeLimitLocalizer.LocalizeWithPrefix("timelimit.enabled"));
                            }
                        }
                    }
                    else
                    {
                        command.ReplyToCommand(_timeLimitLocalizer.LocalizeWithPrefix("timelimit.value-error"));
                    }
                }
                else
                {
                    if (_pluginState.TimeLimitEnabled == true)
                    {
                        command.ReplyToCommand(_timeLimitLocalizer.LocalizeWithPrefix("timelimit.enabled-error"));
                    }
                    else
                    {
                        if (player == null)
                        {
                            _pluginState.TimeLimitScheduled = true;
                            _logger.LogInformation($"Time limit scheduled: mp_timelimit = {_timeLimitManager.TimeRemaining} seconds");
                        }
                        else
                        {
                            if (_timeLimitManager.TimePlayed != 0 && _timeLimitManager.TimeRemaining != 0)
                            {
                                _timeLimitManager.EnableTimeLimit();
                                command.ReplyToCommand(_timeLimitLocalizer.LocalizeWithPrefix("timelimit.enabled"));
                            }
                            else
                            {
                                command.ReplyToCommand(_timeLimitLocalizer.LocalizeWithPrefix(_timeLimitManager.GetTimeLeftMessage()));
                            }
                        }
                    }
                }
            }
            else
            {
                if(_pluginState.TimeLimitEnabled == true)
                {
                    _timeLimitManager.DisableTimeLimit();
                    command.ReplyToCommand(_timeLimitLocalizer.LocalizeWithPrefix("timelimit.disabled"));
                }
                else
                {
                    command.ReplyToCommand(_timeLimitLocalizer.LocalizeWithPrefix("timelimit.disabled-error"));
                }
            }
        }

        // Define time limit command handler
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnTimeLeftCommand(CCSPlayerController? player, CommandInfo command)
        {
            command.ReplyToCommand(_timeLeftLocalizer.LocalizeWithPrefix(_timeLimitManager.GetTimeLeftMessage()));
        }
    }
}