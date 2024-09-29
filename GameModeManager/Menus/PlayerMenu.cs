// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class PlayerMenu : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
         private StringLocalizer _localizer;
        private Config _config = new Config();
        private TimeLimitManager _timeLimitManager;

        // Define class instance
        public PlayerMenu(MenuFactory menuFactory, PluginState pluginState, IStringLocalizer iLocalizer, TimeLimitManager timeLimitManager)
        {
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _timeLimitManager = timeLimitManager;
            _localizer = new StringLocalizer(iLocalizer, "timeleft.prefix");
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define reusable method to update the game command menu
        public void Load()
        {
            // Assign menu
            _pluginState.GameMenu = _menuFactory.AssignMenu(_config.Settings.Style, "Game Commands");

            // Add menu options for each map in the new map list
            foreach (string _command in _pluginState.PlayerCommands)
            {
                _pluginState.GameMenu.AddMenuOption(_command, (player, option) =>
                {
                    // Close menu
                    MenuManager.CloseActiveMenu(player);

                    switch(option.Text)
                    {
                        case "!changemap":
                        if (player != null && _config.Votes.Enabled && _config.Votes.Maps && !_config.Votes.AllMaps)
                        {
                            _menuFactory.OpenMenu(_pluginState.VoteMapMenu, player);
                        }
                        else if(player != null && _config.Votes.Enabled && _config.Votes.Maps && _config.Votes.AllMaps)
                        {
                             _menuFactory.OpenMenu(_pluginState.VoteMapsMenu, player);
                        }
                        break;
                        case "!changemode":
                        if (player != null && _config.Votes.Enabled && _config.Votes.GameModes)
                        {
                            _menuFactory.OpenMenu(_pluginState.VoteModesMenu, player);
                        }
                        break;
                        case "!changesetting":
                        if (player != null && _config.Votes.Enabled && _config.Votes.GameSettings)
                        {
                            _menuFactory.OpenMenu(_pluginState.VoteSettingsMenu, player);
                        }
                        break;
                        case "!currentmode":
                        if (player != null)
                        {
                            player.PrintToChat(_localizer.Localize("currentmode.message", _pluginState.CurrentMode.Name));
                        }
                        break;
                        case "!currentmap":
                        if (player != null)
                        {
                            player.PrintToChat(_localizer.Localize("currentmap.message", _pluginState.CurrentMap.Name));
                        }
                        break;
                        case "!timeleft":
                        if (player != null)
                        {
                            player.PrintToChat(_localizer.LocalizeWithPrefix(_timeLimitManager.GetTimeLeftMessage()));
                        }
                        break;
                    }
                });
            }
        }
    }
}