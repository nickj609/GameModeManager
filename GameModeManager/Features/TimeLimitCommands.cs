// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class EnforceTimeLimitCommand : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private PluginState _pluginState;
        private Config _config = new Config();
        private TimeLimitManager _timeLimitManager;

        // Define class instance
        public EnforceTimeLimitCommand(TimeLimitManager timeLimitManager, PluginState pluginState)
        {
            _pluginState = pluginState;
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
            plugin.AddCommand("css_enforcetimelimit", "Forces rotation on time limit end. Default time limit is mp_timelimit.", OnEnforceTimeLimitCommand);

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
                            command.ReplyToCommand("Time limit is already enabled. " + _timeLimitManager.GetTimeLimitMessage());
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
                        command.ReplyToCommand("Invalid time limit. Please enter a valid integer.");
                    }
                }
                else
                {
                    if (_pluginState.TimeLimitEnabled == true)
                    {
                        command.ReplyToCommand("Time limit is already enabled. " + _timeLimitManager.GetTimeLimitMessage());
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
                                command.ReplyToCommand(_timeLimitManager.GetTimeLimitMessage());
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
            command.ReplyToCommand(_timeLimitManager.GetTimeLimitMessage());
        }
    }
}