// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Models;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class SettingMenus : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
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

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }
        
        // Define on load behavior
        public void Load()
        {
           // Assign menus
            _pluginState.SettingsMenu = _menuFactory.AssignMenu(_config.Settings.Style, "Setting Actions");
            _pluginState.SettingsEnableMenu = _menuFactory.AssignMenu(_config.Settings.Style, "Settings List");
            _pluginState.SettingsDisableMenu = _menuFactory.AssignMenu(_config.Settings.Style, "Settings List");

            // Add enable menu options
            foreach (Setting _setting in _pluginState.Settings)
            {
                _pluginState.SettingsEnableMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
                {
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("enable.changesetting.message", player.PlayerName, option.Text));
                    Server.ExecuteCommand($"exec {_config.Settings.Folder}/{_setting.Enable}");
                    MenuManager.CloseActiveMenu(player);
                });
            }

            // Add disable menu options
            foreach (Setting _setting in _pluginState.Settings)
            {
                _pluginState.SettingsDisableMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
                {
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("disable.changesetting.message", player.PlayerName, option.Text));
                    Server.ExecuteCommand($"exec {_config.Settings.Folder}/{_setting.Disable}");
                    MenuManager.CloseActiveMenu(player);
                });
            }

            // create enable settings sub menu option
            _pluginState.SettingsMenu.AddMenuOption(_localizer.Localize("menu.enable"), (player, option) =>
            {
                _pluginState.SettingsEnableMenu.Title = _localizer.Localize("settings.menu-title");

                if(player != null)
                {
                    _menuFactory.OpenMenu(_pluginState.SettingsEnableMenu, player);
                }
            });

            // Create disable settings menu sub menu option
            _pluginState.SettingsMenu.AddMenuOption(_localizer.Localize("menu.disable"), (player, option) =>
            {
                _pluginState.SettingsDisableMenu.Title = _localizer.Localize("settings.menu-title");

                if(player != null)
                {
                    _menuFactory.OpenMenu(_pluginState.SettingsDisableMenu, player);   
                }
            });
            
            // Create user settings menu
            if(_config.Votes.GameSettings)
            {
                CreateVoteSettingsMenu();
            }
        }
        
        // Define resuable method to set up show maps menu
        public void CreateVoteSettingsMenu()
        {
            // Assign menu
            _pluginState.VoteSettingsMenu = _menuFactory.AssignMenu(_config.Settings.Style, "Setting List");
            
            foreach (Setting _setting in _pluginState.Settings)
            {
                // Add menu option
                _pluginState.VoteSettingsMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
                {
                    // Close menu
                    MenuManager.CloseActiveMenu(player);

                    // Start vote
                    _pluginState.CustomVotesApi.Get()?.StartCustomVote(player, _setting.Name); 
                });
            }
        }
    }
}