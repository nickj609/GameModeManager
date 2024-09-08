// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class RTVCommand : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private PluginState _pluginState;
        private ILogger<RTVCommand> _logger;
        private Config _config = new Config();

        // Define class instance
        public RTVCommand(PluginState pluginState, ILogger<RTVCommand> logger)
        {
            _logger = logger;
            _pluginState = pluginState;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            plugin.AddCommand("css_rtv_enabled", "Enables or disables RTV.", OnRTVCommand);
        }

        // Define server rtv command handler
        [CommandHelper(minArgs: 1, usage: "<true|false>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnRTVCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) 
            {
               if (command.ArgByIndex(1).Equals("true", StringComparison.OrdinalIgnoreCase) && !_pluginState.RTVEnabled)
               {
                    _logger.LogInformation($"Enabling RTV...");
                    Server.ExecuteCommand($"css_plugins load {_config.RTV.Plugin}");

                    _logger.LogInformation($"Disabling game mode and map rotations...");
                    _pluginState.RTVEnabled = true;
               }
               else if (command.ArgByIndex(1).Equals("false", StringComparison.OrdinalIgnoreCase) && _pluginState.RTVEnabled)
               {
                
                    _logger.LogInformation($"Disabling RTV...");
                    Server.ExecuteCommand($"css_plugins unload {_config.RTV.Plugin}");

                    _logger.LogInformation($"Enabling game mode and map rotations...");
                    _pluginState.RTVEnabled = false;
               }
               else
               {
                    command.ReplyToCommand($"Unexpected argument: {command.ArgByIndex(1)}");
               }
            }
        }
    }
}