// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class RTVCommand : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
         private RTVManager _rtvManager;
        private PluginState _pluginState;

        // Define class instance
        public RTVCommand(PluginState pluginState, RTVManager rtvManager)
        {
            _rtvManager = rtvManager;
            _pluginState = pluginState;
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
                    _rtvManager.EnableRTV();
               }
               else if (command.ArgByIndex(1).Equals("false", StringComparison.OrdinalIgnoreCase) && _pluginState.RTVEnabled)
               {
                    _rtvManager.DisableRTV();
               }
               else
               {
                    command.ReplyToCommand($"Unexpected argument: {command.ArgByIndex(1)}");
               }
            }
        }
    }
}