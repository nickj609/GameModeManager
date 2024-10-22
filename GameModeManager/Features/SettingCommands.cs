// Included libraries
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class SettingCommands : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;
        private Config _config = new Config();

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
        [CommandHelper(minArgs: 1, usage: "[enable|disable] [setting name]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnSettingCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                // Get args
                string _status = $"{command.ArgByIndex(1)}";
                string _settingName = $"{command.ArgByIndex(2)}";

                // Find game setting
                Setting? _option = _pluginState.Settings.FirstOrDefault(s => s.Name.Equals(_settingName, StringComparison.OrdinalIgnoreCase));

                if(_option != null) 
                {
                    if (_status.Equals("enable", StringComparison.OrdinalIgnoreCase)) 
                    {
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("enable.changesetting.message", player.PlayerName, _settingName));
                        Server.ExecuteCommand($"exec {_config.Settings.Folder}/{_option.Enable}");
                    }
                    else if (_status.Equals("disable", StringComparison.OrdinalIgnoreCase))
                    {
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("disable.changesetting.message", player.PlayerName, _settingName));
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
        }

        // Define admin setting menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnSettingsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                if (_config.Settings.Style.Equals("wasd") && _pluginState.SettingsWASDMenu != null)
                {
                    _menuFactory.OpenWasdMenu(player, _pluginState.SettingsWASDMenu);
                }
                else
                {
                    _pluginState.SettingsMenu.Title = _localizer.Localize("settings.menu-actions");
                    _menuFactory.OpenMenu(_pluginState.SettingsMenu, player);
                }
            }
        }
    }
}