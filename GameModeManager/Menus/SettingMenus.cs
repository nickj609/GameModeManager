// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Models;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class SettingMenus: IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Config _config = new();
        private PluginState _pluginState;
        private StringLocalizer _localizer;

        // Define class constructor
        public SettingMenus(PluginState pluginState, StringLocalizer localizer)
        {
            _localizer = localizer;
            _pluginState = pluginState;
        }

        // Define class properties
        public IMenu? MainMenu;
        public IMenu? VoteMenu;
        public IMenu? EnableMenu;
        public IMenu? DisableMenu;

        // Define on config parsed
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }
        // Define load method
        public void Load()
        {
            // Create sub menus
            DisableMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("settings.menu-title"));
            EnableMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("settings.menu-title"));

            // Add enable sub menu options
            foreach (Setting _setting in _pluginState.Game.Settings.Values)
            {
                EnableMenu?.AddMenuOption(_setting.DisplayName, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("enable.changesetting.message", player.PlayerName, _setting.DisplayName));
                    Server.ExecuteCommand($"exec {_config.Settings.Folder}/{_setting.Enable}");
                });
            }

            // Add disable sub menu options
            foreach (Setting _setting in _pluginState.Game.Settings.Values)
            {
                DisableMenu?.AddMenuOption(_setting.DisplayName, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("disable.changesetting.message", player.PlayerName, _setting.DisplayName));
                    Server.ExecuteCommand($"exec {_config.Settings.Folder}/{_setting.Disable}");
                });
            }

            // Create main menu
            MainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("settings.menu-actions"));

            MainMenu?.AddMenuOption(_localizer.Localize("menu.enable"), (player, option) =>
            {
                EnableMenu?.Open(player);
            });
            
            MainMenu?.AddMenuOption(_localizer.Localize("menu.disable"), (player, option) =>
            {
                DisableMenu?.Open(player);
            });

            // Create vote menu
            VoteMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("settings.menu-title"));
            if (_config.Votes.GameSettings)
            {
                foreach (Setting _setting in _pluginState.Game.Settings.Values)
                {
                    VoteMenu?.AddMenuOption(_setting.DisplayName, (player, option) =>
                    {
                        MenuFactory.Api?.CloseMenu(player);
                        CustomVoteManager.CustomVotesApi.Get()?.StartCustomVote(player, _setting.Name);
                    });
                }
            }
        }
    }
}