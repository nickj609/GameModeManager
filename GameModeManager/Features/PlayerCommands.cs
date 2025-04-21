// Included libraries
using WASDSharedAPI;
using GameModeManager.Menus;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class PlayerCommands : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private MapMenus _mapMenus;
        private ModeMenus _modeMenus;
        private PlayerMenu _playerMenu;
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private SettingMenus _settingMenus;
        private StringLocalizer _localizer;
        private Config _config = new Config();

        // Define class instance
        public PlayerCommands(PluginState pluginState, IStringLocalizer iLocalizer, MenuFactory menuFactory, SettingMenus settingMenus, PlayerMenu playerMenu, MapMenus mapMenus, ModeMenus modeMenus)
        {
            _mapMenus = mapMenus;
            _modeMenus = modeMenus;
            _playerMenu = playerMenu;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _settingMenus = settingMenus;
            _localizer = new StringLocalizer(iLocalizer, "timeleft.prefix");
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            plugin.AddCommand("css_game", "Displays list of player commands", OnGameCommand);
            plugin.AddCommand("css_currentmap", "Displays current map.", OnCurrentMapCommand);
            plugin.AddCommand("css_currentmode", "Displays current map.", OnCurrentModeCommand);

            if(_config.Votes.Enabled)
            {
                if (_config.Votes.Maps)
                {
                    plugin.AddCommand("css_changemap", "Displays the vote map menu.", OnChangeMapCommand);
                }
                if (_config.Votes.GameModes)
                {
                    plugin.AddCommand("css_changemode", "Displays the vote mode menu", OnChangeModeCommand);
                }
                if (_config.Votes.GameSettings)
                {
                    plugin.AddCommand("css_changesetting", "Displays the vote setting menu", OnChangeSettingCommand);
                }
            }
        }

        // Define command handlers
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _config.Maps.Mode == 1)
            {     
                if (_config.Votes.Style.Equals("wasd"))
                {
                    IWasdMenu? menu;
                    menu = _mapMenus.GetWasdMenu("VoteAll");

                    if(menu != null)
                    {
                        _menuFactory.OpenWasdMenu(player, menu);
                    }
                }
                else
                { 
                    BaseMenu menu;
                    menu = _mapMenus.GetMenu("All");
                    menu.Title = _localizer.Localize ("modes.menu-title");
                    _menuFactory.OpenMenu(menu, player);
                }
                
            }              
            else if(player != null && _config.Maps.Mode == 0)
            {
                if (_config.Votes.Style.Equals("wasd"))
                {
                    IWasdMenu? menu;
                    menu = _mapMenus.GetWasdMenu("VoteCurrentMode");

                    if(menu != null)
                    {
                        _menuFactory.OpenWasdMenu(player, menu);
                    }
                }
                else
                {
                    BaseMenu menu;
                    menu = _mapMenus.GetMenu("CurrentMode");
                    menu.Title = _localizer.Localize ("maps.menu-title");
                    _menuFactory.OpenMenu(menu, player);
                }
            }   
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                if (_config.Votes.Style.Equals("wasd"))
                {
                    IWasdMenu? menu;
                    menu = _modeMenus.GetWasdMenu("Vote");

                    if(menu != null)
                    {
                        _menuFactory.OpenWasdMenu(player, menu);
                    }
                }
                else
                {
                    BaseMenu menu;
                    menu = _modeMenus.GetMenu("Vote");
                    menu.Title = _localizer.Localize("modes.menu-title");
                    _menuFactory.OpenMenu(menu, player);
                }
            } 
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeSettingCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                if (_config.Settings.Style.Equals("wasd"))
                {  
                    IWasdMenu? menu;
                    menu = _settingMenus.GetWasdMenu("Vote");

                    if(menu != null)
                    {
                        _menuFactory.OpenWasdMenu(player, menu);
                    }
                }
                else
                {
                    BaseMenu menu;
                    menu = _settingMenus.GetMenu("Vote");

                    if (menu != null)
                    {
                         menu.Title = _localizer.Localize("settings.menu-actions");
                        _menuFactory.OpenMenu(menu, player);
                    }
                }
            }
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnGameCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                if (_config.Commands.Style.Equals("wasd"))
                {
                    IWasdMenu? menu;
                    menu = _playerMenu.GetWasdMenu();
                    
                    if (menu != null)
                    {
                        _menuFactory.OpenWasdMenu(player, menu);
                    }
                }
                else
                {
                    BaseMenu menu;
                    menu = _playerMenu.GetMenu();

                    if (menu != null)
                    {
                        menu.Title = _localizer.Localize("game.menu-title");
                        _menuFactory.OpenMenu(menu, player);
                    }
                }
            }
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnCurrentMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                player.PrintToChat(_localizer.Localize("currentmap.message", _pluginState.CurrentMap.DisplayName));
            }
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnCurrentModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                player.PrintToChat(_localizer.Localize("currentmode.message", _pluginState.CurrentMode.Name));
            }
        } 
    }
}