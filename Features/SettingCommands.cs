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
        // Construct admin change setting command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 2, usage: "<enable|disable> <setting name>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_setting", "Changes the game setting specified.")]
        public void OnSettingCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                // Get args
                string _status = $"{command.ArgByIndex(1).ToLower()}";
                string _settingName = $"{command.ArgByIndex(2)}";

                // Find game setting
                Setting? _option = SettingsManager.Settings.FirstOrDefault(s => s.Name == _settingName);

                if(_option != null) 
                {
                    if (_status == "enable")
                    {
                        // Create message
                        string _message = Localizer["plugin.prefix"] + " " + Localizer["enable.changesetting.message", player.PlayerName, _settingName];

                        // Write to chat
                        Server.PrintToChatAll(_message);

                        // Change game setting
                        Server.ExecuteCommand($"exec {Config.Settings.Folder}/{_option.Enable}");
                    }
                    else if (_status == "disable")
                    {
                        // Create message
                        string _message = Localizer["plugin.prefix"] + " " + Localizer["disable.changesetting.message", player.PlayerName, _settingName];

                        // Write to chat
                        Server.PrintToChatAll(_message);

                        // Change game setting
                        Server.ExecuteCommand($"exec {Config.Settings.Folder}/{_option.Disable}");
                    }
                    else
                    {
                        command.ReplyToCommand($"Unexpected argument: {_status}");
                    }  
                }
                else
                {
                    command.ReplyToCommand($"Can't find setting: {_settingName}");
                }
            }
            else if (player == null)
            {
                Console.Error.WriteLine("css_settings is a client only command.");
            }
        }

        // Construct admin setting menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_settings", "Provides a list of game settings.")]
        public void OnSettingsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && MenuFactory.SettingsMenu != null)
            {
                // Open menu
                MenuFactory.SettingsMenu.Title = Localizer["settings.menu-actions"];
                MenuFactory.OpenMenu(MenuFactory.SettingsMenu, Config.Settings.Style, player);
            }
            else if (player == null)
            {
                Console.Error.WriteLine("css_settings is a client only command.");
            }
        }
    }
}