// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using MenuManager;

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
        private TimeLimitManager _timeLimitManager;
        private StringLocalizer _timeLimitLocalizer;

        // Define class constructor
        public TimeLimitCommands(TimeLimitManager timeLimitManager, PluginState pluginState, IStringLocalizer iLocalizer, ILogger<TimeLimitCommands> logger)
        {
            _logger = logger;
            _pluginState = pluginState;
            _timeLimitManager = timeLimitManager;
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
        }

        // Define admin map menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(minArgs: 1, usage: "<true|false> optional: <seconds>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void OnTimeLimitCommand(CCSPlayerController? player, CommandInfo command)
        {
            // Parse the first argument
            if (!bool.TryParse(command.ArgByIndex(1), out bool enableRequested))
            {
                PlayerFeedback.Reply(command, player, _timeLimitLocalizer, "notify.timelimit", MenuNotice.Warning, "timelimit.invalid-bool-error", command.ArgByIndex(1));
                return;
            }

            // Check if timelimit is already enabled/disabled
            if (enableRequested && _pluginState.TimeLimit.Enabled)
            {
                PlayerFeedback.Reply(command, player, _timeLimitLocalizer, "notify.timelimit", MenuNotice.Warning, "timelimit.enabled-error");
                return;
            }
            
            if (!enableRequested && !_pluginState.TimeLimit.Enabled)
            {
                PlayerFeedback.Reply(command, player, _timeLimitLocalizer, "notify.timelimit", MenuNotice.Warning, "timelimit.disabled-error");
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
                        PlayerFeedback.Reply(command, player, _timeLimitLocalizer, "notify.timelimit", MenuNotice.Warning, "timelimit.value-error");
                        return; 
                    }
                }

                if (specificTime.HasValue)
                {
                    _timeLimitManager.EnableTimeLimit(specificTime.Value); 
                    PlayerFeedback.Reply(command, player, _timeLimitLocalizer, "notify.timelimit", MenuNotice.Success, "timelimit.enabled");
                    _logger.LogInformation("Time limit enabled by {Executor} with duration: {Seconds} seconds.", executor, specificTime.Value);
                }
                else
                {
                    _timeLimitManager.EnableTimeLimit(); 
                    PlayerFeedback.Reply(command, player, _timeLimitLocalizer, "notify.timelimit", MenuNotice.Success, "timelimit.enabled");
                    _logger.LogInformation("Time limit enabled by {Executor} using default duration.", executor);
                }
            }
            else 
            {
                _timeLimitManager.DisableTimeLimit(); 
                PlayerFeedback.Reply(command, player, _timeLimitLocalizer, "notify.timelimit", MenuNotice.Success, "timelimit.disabled");
                _logger.LogInformation("Time limit disabled by {Executor}.", executor);
            }
        }
    }
}