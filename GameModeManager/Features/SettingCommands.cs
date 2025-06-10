// Included libraries
using GameModeManager.Menus;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class SettingCommands : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private SettingMenus _settingMenus;
        private StringLocalizer _localizer;
        private Config _config = new Config();

        // Define class constructor
        public SettingCommands(PluginState pluginState, StringLocalizer localizer, SettingMenus settingMenus)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _settingMenus = settingMenus;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            if (_config.Settings.Enabled)
            {
                plugin.AddCommand("css_setting", "Changes the game setting.", OnSettingCommand);
                plugin.AddCommand("css_settings", "Shows a list of game settings.", OnSettingsCommand);
            }
        }

        // Define command handlers
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(minArgs: 1, usage: "<enable|disable> <setting name>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnSettingCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                string _status = $"{command.ArgByIndex(1)}";
                string _settingName = $"{command.ArgByIndex(2)}";

                if(_pluginState.Game.Settings.TryGetValue(_settingName, out ISetting? _setting)) 
                {
                    if (_status.Equals("enable", StringComparison.OrdinalIgnoreCase)) 
                    {
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("enable.changesetting.message", player.PlayerName, _settingName));
                        Server.ExecuteCommand($"exec {_config.Settings.Folder}/{_setting.Enable}");
                    }
                    else if (_status.Equals("disable", StringComparison.OrdinalIgnoreCase))
                    {
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("disable.changesetting.message", player.PlayerName, _settingName));
                        Server.ExecuteCommand($"exec {_config.Settings.Folder}/{_setting.Disable}");
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

        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnSettingsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                _settingMenus.MainMenu?.Open(player);
            }
        }
    }
}