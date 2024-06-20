// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin
    {
        // Construct admin change mode command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "<mode>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_mode", "Changes the game mode to the mode specified in the command argument.")]
        public void OnModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                // Define variables
                string? _option = null;
                MapGroup? _mapGroup = PluginState.MapGroups?.FirstOrDefault(g => g.Name == $"{command.ArgByIndex(1)}");
                KeyValuePair<string, string>? _mode = Config.GameMode.List?.FirstOrDefault(m => m.Key == $"{command.ArgByIndex(1)}");

                // Check if using mode list or map groups
                if (Config.GameMode.ListEnabled != true)
                {
                    _mapGroup = PluginState.MapGroups?.FirstOrDefault(g => g.Name == $"{command.ArgByIndex(1)}");

                    if (_mapGroup != null && _mapGroup.Name != null)
                    {
                        _option = _mapGroup.Name;
                    }
                }
                else
                {
                    _mode = Config.GameMode.List?.FirstOrDefault(m => m.Key == $"{command.ArgByIndex(1)}");

                    if (_mode != null && _mode is KeyValuePair<string, string> kvp)
                    {
                        _option = kvp.Key;
                    }
                }

                // Check if mode or mapgroup is found
                if (_option != null)
                {
                    // Create mode message
                    string _message = Localizer["plugin.prefix"] + " " + Localizer["changemode.message", player.PlayerName, command.ArgByIndex(1)];

                    // Write to chat
                    Server.PrintToChatAll(_message);

                    // Change mode
                    _option = _option.ToLower();
                    AddTimer(Config.GameMode.Delay, () => 
                    {
                        Server.ExecuteCommand($"exec {_option}.cfg");
                    }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
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