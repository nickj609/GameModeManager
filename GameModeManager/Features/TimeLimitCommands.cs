// Included libraries
using GameModeManager.Core;
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

        // Define class constructor
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
                plugin.AddCommand("css_timelimit", "Forces rotation on time limit end. Usage: timelimit <true|false> [seconds]", OnTimeLimitCommand);
            }
            if (_config.Commands.TimeLeft)
            {
                _pluginState.Game.PlayerCommands.Add("!timeleft");
                plugin.AddCommand("timeleft", "Prints in the chat the timeleft in the current map", OnTimeLeftCommand);
            }
        }

        // Define admin map menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(minArgs: 1, usage: "<true|false> optional: <seconds>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void OnTimeLimitCommand(CCSPlayerController? player, CommandInfo command)
        {
            // Parse the first argument
            if (!bool.TryParse(command.ArgByIndex(1), out bool enableRequested))
            {
                command.ReplyToCommand(_timeLimitLocalizer.LocalizeWithPrefix("timelimit.invalid-bool-error", command.ArgByIndex(1))); // Suggest adding this localization key
                return;
            }

            // Check if timelimit is already enabled/disabled
            if (enableRequested && _pluginState.TimeLimit.Enabled)
            {
                command.ReplyToCommand(_timeLimitLocalizer.LocalizeWithPrefix("timelimit.enabled-error")); // Already enabled
                return;
            }
            
            if (!enableRequested && !_pluginState.TimeLimit.Enabled)
            {
                command.ReplyToCommand(_timeLimitLocalizer.LocalizeWithPrefix("timelimit.disabled-error")); // Already disabled
                return;
            }

            string executor = player == null ? "Console" : player.PlayerName;

            if (enableRequested)
            {
                int? specificTime = null; 

                // Check if a specific time limit is provided as the second argument
                if (command.ArgCount >= 3)
                {
                    if (int.TryParse(command.ArgByIndex(2), out int seconds) && seconds >= 0)
                    {
                        specificTime = seconds;
                    }
                    else
                    {
                        command.ReplyToCommand(_timeLimitLocalizer.LocalizeWithPrefix("timelimit.value-error"));
                        return; 
                    }
                }

                if (specificTime.HasValue)
                {
                    _timeLimitManager.EnableTimeLimit(specificTime.Value); 
                    command.ReplyToCommand(_timeLimitLocalizer.LocalizeWithPrefix("timelimit.enabled")); 
                    _logger.LogInformation("Time limit enabled by {Executor} with duration: {Seconds} seconds.", executor, specificTime.Value);
                }
                else
                {
                    _timeLimitManager.EnableTimeLimit(); 
                    command.ReplyToCommand(_timeLimitLocalizer.LocalizeWithPrefix("timelimit.enabled"));
                    _logger.LogInformation("Time limit enabled by {Executor} using default duration.", executor);
                }
            }
            else 
            {
                _timeLimitManager.DisableTimeLimit(); 
                command.ReplyToCommand(_timeLimitLocalizer.LocalizeWithPrefix("timelimit.disabled"));
                _logger.LogInformation("Time limit disabled by {Executor}.", executor);
            }
        }

        // Define time limit command handler
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnTimeLeftCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
                return;

            command.ReplyToCommand(_timeLeftLocalizer.LocalizeWithPrefix(_timeLimitManager.GetTimeLeftMessage()));
        }
    }
}