// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager
{
    public class RTVCommand : IPluginDependency<Plugin, Config>
    {

        // Define dependencies
        private ILogger _logger;
        private PluginState _pluginState;
        private Config _config = new Config();

        // Define class instance
        public RTVCommand(PluginState pluginState, ILogger logger)
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
            _logger = plugin.Logger;
            plugin.AddCommand("css_rtv_enabled", "Enables or disables RTV.", OnRTVCommand);
        }

        // Define server rtv command handler
        [CommandHelper(minArgs: 1, usage: "<true|false>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnRTVCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) 
            {
               if (command.ArgByIndex(1).ToLower() == "true" && _pluginState.RTVEnabled == false && _logger != null && _config != null)
               {
                    _logger.LogInformation($"Enabling RTV...");
                    Server.ExecuteCommand($"css_plugins load {_config.RTV.Plugin}");

                    _logger.LogInformation($"Disabling game mode and map rotations...");
                    _pluginState.RTVEnabled = true;
               }
               else if (command.ArgByIndex(1).ToLower() == "false" && _pluginState.RTVEnabled && _logger != null && _config != null)
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