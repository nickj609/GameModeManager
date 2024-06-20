// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin
    {
        // Construct server rtv command handler
        [ConsoleCommand("css_rtv_enabled", "Enables or disables RTV.")]
        [CommandHelper(minArgs: 1, usage: "<true|false>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnRTVCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) 
            {
               if (command.ArgByIndex(1).ToLower() == "true" && PluginState.RTVEnabled == false)
               {
                    Logger.LogInformation($"Enabling RTV...");
                    Server.ExecuteCommand($"css_plugins load {Config.RTV.Plugin}");

                    Logger.LogInformation($"Disabling game mode and map rotations...");
                    PluginState.RTVEnabled = true;
               }
               else if (command.ArgByIndex(1).ToLower() == "false" && PluginState.RTVEnabled == true)
               {
                
                    Logger.LogInformation($"Disabling RTV...");
                    Server.ExecuteCommand($"css_plugins unload {Config.RTV.Plugin}");

                    Logger.LogInformation($"Enabling game mode and map rotations...");
                    PluginState.RTVEnabled = false;
               }
               else
               {
                    command.ReplyToCommand($"Unexpected argument: {command.ArgByIndex(1)}");
               }
            }
        }
    }
}