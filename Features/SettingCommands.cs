// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager
{
    public class SettingCommands : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Config? _config;
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;

        // Define class instance
        public SettingCommands(PluginState pluginState, StringLocalizer localizer, MenuFactory menuFactory)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            plugin.AddCommand("css_setting", "Changes the game setting.", OnSettingCommand);
            plugin.AddCommand("css_settings", "Shows a list of game settings.", OnSettingsCommand);
        }

        // Define admin change setting command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 2, usage: "<enable|disable> <setting name>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnSettingCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                // Get args
                string _status = $"{command.ArgByIndex(1).ToLower()}";
                string _settingName = $"{command.ArgByIndex(2)}";

                // Find game setting
                Setting? _option = _pluginState.Settings.FirstOrDefault(s => s.Name == _settingName);

                if(_option != null) 
                {
                    if (_status == "enable" && _localizer != null && _config != null) 
                    {
                        // Create message
                        string _message = _localizer.LocalizeWithPrefix("enable.changesetting.message", player.PlayerName, _settingName);

                        // Write to chat
                        Server.PrintToChatAll(_message);

                        // Change game setting
                        Server.ExecuteCommand($"exec {_config.Settings.Folder}/{_option.Enable}");
                    }
                    else if (_status == "disable" && _localizer != null && _config != null)
                    {
                        // Create message
                        string _message = _localizer.LocalizeWithPrefix("disable.changesetting.message", player.PlayerName, _settingName);

                        // Write to chat
                        Server.PrintToChatAll(_message);

                        // Change game setting
                        Server.ExecuteCommand($"exec {_config.Settings.Folder}/{_option.Disable}");
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

        // Define admin setting menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnSettingsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _pluginState.SettingsMenu != null && _localizer != null && _config != null)
            {
                // Open menu
                _pluginState.SettingsMenu.Title = _localizer.Localize("settings.menu-actions");
                _menuFactory.OpenMenu(_pluginState.SettingsMenu, _config.Settings.Style, player);
            }
            else if (player == null)
            {
                Console.Error.WriteLine("css_settings is a client only command.");
            }
        }
    }
}