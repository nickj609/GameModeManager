// Included libraries
using GameModeManager.Menus;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using WASDMenuAPI.Shared.Models;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class SettingCommands : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Plugin? _plugin;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private Config _config = new Config();

        // Define class instance
        public SettingCommands(PluginState pluginState, StringLocalizer localizer)
        {
            _localizer = localizer;
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
            _plugin = plugin;

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
                ISetting? _option = _pluginState.Game.Settings.FirstOrDefault(s => s.Name.Equals(_settingName, StringComparison.OrdinalIgnoreCase));

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

        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnSettingsCommand(CCSPlayerController? player, CommandInfo command)
        {
            MenuFactory _menuFactory = new MenuFactory(_plugin);
            SettingMenus _settingMenus = new SettingMenus(_plugin, _pluginState, _localizer, _config);
            
            if (player != null)
            {
                if (_config.Settings.Style.Equals("wasd"))
                {
                    IWasdMenu? menu = _settingMenus.WasdMenus.MainMenu;

                    if (menu != null)
                    {
                        _menuFactory.WasdMenus.OpenMenu(player, menu);
                    }
                }
                else
                {
                    BaseMenu menu = _settingMenus.BaseMenus.MainMenu;

                    if (menu != null)
                    {
                        menu.Title = _localizer.Localize("settings.menu-actions");
                        _menuFactory.BaseMenus.OpenMenu(menu, player);
                    }
                }
            }
        }
    }
}