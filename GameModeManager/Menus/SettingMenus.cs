// Included libraries
using WASDSharedAPI;
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class SettingMenus : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;
        private Config _config = new Config();

        // Define class instance
        public SettingMenus(MenuFactory menuFactory, PluginState pluginState, StringLocalizer localizer)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
        }

        // Define class properties
        private IWasdMenu? settingsWASDMenu;
        private IWasdMenu? voteSettingsWASDMenu;
        private IWasdMenu? settingsEnableWASDMenu;
        private IWasdMenu? settingsDisableWASDMenu;
        private BaseMenu settingsMenu = new ChatMenu("Setting Actions");
        private BaseMenu voteSettingsMenu = new ChatMenu("Settings List");
        private BaseMenu settingsEnableMenu = new ChatMenu("Settings List");
        private BaseMenu settingsDisableMenu = new ChatMenu("Settings List");

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define methods to get menus
        public BaseMenu GetMenu(string Name)
        {
            if (Name.Equals("Main Menu"))
            {
                return settingsMenu;
            }
            else
            {
                return voteSettingsMenu;
            }
        }

        public IWasdMenu? GetWasdMenu(String Name)
        {
            if (Name.Equals("Main Menu"))
            {
                return settingsWASDMenu;
            }
            else
            {
                return voteSettingsWASDMenu;
            }
        }
        
        // Define method to load menus
        public void Load()
        {
            settingsMenu = _menuFactory.AssignMenu(_config.Settings.Style, "Setting Actions");
            settingsEnableMenu = _menuFactory.AssignMenu(_config.Settings.Style, "Settings List");
            settingsDisableMenu = _menuFactory.AssignMenu(_config.Settings.Style, "Settings List");

            // Add enable menu options
            foreach (Setting _setting in _pluginState.Settings)
            {
                settingsEnableMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
                {
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("enable.changesetting.message", player.PlayerName, option.Text));
                    Server.ExecuteCommand($"exec {_config.Settings.Folder}/{_setting.Enable}");
                    MenuManager.CloseActiveMenu(player);
                });
            }

            // Add disable menu options
            foreach (Setting _setting in _pluginState.Settings)
            {
                settingsDisableMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
                {
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("disable.changesetting.message", player.PlayerName, option.Text));
                    Server.ExecuteCommand($"exec {_config.Settings.Folder}/{_setting.Disable}");
                    MenuManager.CloseActiveMenu(player);
                });
            }

            // create enable settings sub menu option
            settingsMenu.AddMenuOption(_localizer.Localize("menu.enable"), (player, option) =>
            {
                settingsEnableMenu.Title = _localizer.Localize("settings.menu-title");

                if(player != null)
                {
                    _menuFactory.OpenMenu(settingsEnableMenu, player);
                }
            });

            // Create disable settings menu sub menu option
            settingsMenu.AddMenuOption(_localizer.Localize("menu.disable"), (player, option) =>
            {
                settingsDisableMenu.Title = _localizer.Localize("settings.menu-title");

                if(player != null)
                {
                    _menuFactory.OpenMenu(settingsDisableMenu, player);   
                }
            });
            
            // Create user settings menu
            if (_config.Votes.GameSettings)
            {
               voteSettingsMenu = _menuFactory.AssignMenu(_config.Settings.Style, "Setting List");
                
                // Add menu options
                foreach (Setting _setting in _pluginState.Settings)
                {
                    // Create menu option
                    voteSettingsMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Start vote
                        _pluginState.CustomVotesApi.Get()?.StartCustomVote(player, _setting.Name); 
                    });
                }
            }
        }

        // Define method to load WASD menus
        public void LoadWASDMenu()
        {
             if (_config.Settings.Style.Equals("wasd"))
            {
                settingsWASDMenu = _menuFactory.AssignWasdMenu("Setting Actions");
                settingsEnableWASDMenu = _menuFactory.AssignWasdMenu("Settings List");
                settingsDisableWASDMenu = _menuFactory.AssignWasdMenu("Settings List");

                // Add enable sub menu options
                foreach (Setting _setting in _pluginState.Settings)
                {
                    settingsEnableWASDMenu?.Add(_setting.DisplayName, (player, option) =>
                    {
                        // Close menu
                        _menuFactory.CloseWasdMenu(player);
                        
                        // Enable setting
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("enable.changesetting.message", player.PlayerName, _setting.DisplayName));
                        Server.ExecuteCommand($"exec {_config.Settings.Folder}/{_setting.Enable}");
                    });
                }

                // Add disable sub menu options
                foreach (Setting _setting in _pluginState.Settings)
                {
                    settingsDisableWASDMenu?.Add(_setting.DisplayName, (player, option) =>
                    {
                        // Close menu
                        _menuFactory.CloseWasdMenu(player);
                        
                        // Disable setting
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("disable.changesetting.message", player.PlayerName, _setting.DisplayName));
                        Server.ExecuteCommand($"exec {_config.Settings.Folder}/{_setting.Disable}");
                    });
                }

                // create enable settings sub menu option
                settingsWASDMenu?.Add(_localizer.Localize("menu.enable"), (player, option) =>
                {
                    if(settingsEnableWASDMenu != null)
                    {
                        settingsEnableWASDMenu.Title = _localizer.Localize("settings.menu-title");
                        settingsEnableWASDMenu.Prev = option.Parent?.Options?.Find(option);
                        _menuFactory.OpenWasdSubMenu(player, settingsEnableWASDMenu);
                    }
                });

                // Create disable settings menu sub menu option
                settingsWASDMenu?.Add(_localizer.Localize("menu.disable"), (player, option) =>
                {
                    if(settingsDisableWASDMenu != null)
                    {
                        settingsDisableWASDMenu.Title = _localizer.Localize("settings.menu-title");
                        settingsDisableWASDMenu.Prev = option.Parent?.Options?.Find(option);
                        _menuFactory.OpenWasdSubMenu(player, settingsDisableWASDMenu);   
                    }
                });

                // Create user settings menu
                if (_config.Votes.GameSettings)
                {
                    voteSettingsWASDMenu = _menuFactory.AssignWasdMenu("Setting List");

                    // Add menu options
                    foreach (Setting _setting in _pluginState.Settings)
                    {
                        voteSettingsWASDMenu?.Add(_setting.DisplayName, (player, option) =>
                        {
                            // Close menu
                            _menuFactory.CloseWasdMenu(player);

                            // Start vote
                            _pluginState.CustomVotesApi.Get()?.StartCustomVote(player, _setting.Name); 
                        });
                    }
                }
            }
        }
    }
}