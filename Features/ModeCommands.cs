// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin
    {
        // Construct server game mode command handler
        [ConsoleCommand("css_gamemode", "Sets the current mapgroup.")]
        [CommandHelper(minArgs: 1, usage: "<comp>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnGameModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) 
            {
                Mode? _mode = PluginState.Modes.FirstOrDefault(m => m.Name.ToLower() == command.ArgByIndex(1) || m.Config == $"{command.ArgByIndex(1)}.cfg");

                if(_mode != null)
                {
                    PluginState.CurrentMode = _mode;
                }
                else
                {
                    Logger.LogWarning($"Unable to find game mode {command.ArgByIndex(1)}. Setting default game mode.");
                    PluginState.CurrentMode = PluginState.DefaultMode;
                }
            }
        }
        // Construct admin change mode command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "<mode>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_mode", "Changes the game mode to the mode specified in the command argument.")]
        public void OnModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                // Define variables
                Mode? _mode = PluginState.Modes.FirstOrDefault(m => m.Name == $"{command.ArgByIndex(1)}");

                if (_mode != null)
                {
                    // Create mode message
                    string _message = _localizer.LocalizeWithPrefix("changemode.message", player.PlayerName, _mode.Name);

                    // Write to chat
                    Server.PrintToChatAll(_message);

                    // Change mode
                    AddTimer(Config.GameMode.Delay, () => 
                    {
                        Server.ExecuteCommand($"exec {_mode.Config}");
                    }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

                    // Set current mode
                    PluginState.CurrentMode = _mode;
                }
                else
                {
                    // Reply with not found message
                    command.ReplyToCommand($"Can't find mode: {command.ArgByIndex(1)}");
                }
            }
            else
            {
                Console.Error.WriteLine("css_mode is a client only command.");
            }
        }

        // Construct admin mode menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_modes", "Provides a list of game modes.")]
        public void OnModesCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && MenuFactory.ModeMenu != null)
            {
                // Open menu
                MenuFactory.ModeMenu.Title = Localizer["modes.menu-title"];
                MenuFactory.OpenMenu(MenuFactory.ModeMenu, Config.GameMode.Style, player);
            }
            else if (player == null)
            {
                Console.Error.WriteLine("css_modes is a client only command.");
            }
        }
    }
}