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

            // Add menu options for each command in the command list
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
                            if (_config.Maps.Style.Equals("wasd") && _pluginState.VoteMapWASDMenu != null)
                            {
                                _menuFactory.OpenWasdMenu(player, _pluginState.VoteMapWASDMenu);
                            }
                            else
                            {
                                _menuFactory.OpenMenu(_pluginState.VoteMapMenu, player);
                            }
                        }
                        else if(player != null && _config.Votes.Enabled && _config.Votes.Maps && _config.Votes.AllMaps)
                        {
                            if (_config.Maps.Style.Equals("wasd")  && _pluginState.VoteMapsWASDMenu != null)
                            {
                                _menuFactory.OpenWasdMenu(player, _pluginState.VoteMapsWASDMenu);
                            }
                            else
                            {
                                _menuFactory.OpenMenu(_pluginState.VoteMapsMenu, player);
                            }
                        }
                        break;
                        case "!changemode":
                        if (player != null && _config.Votes.Enabled && _config.Votes.GameModes)
                        {
                            if (_config.Maps.Style.Equals("wasd") && _pluginState.VoteModesWASDMenu != null)
                            {
                                _menuFactory.OpenWasdMenu(player, _pluginState.VoteModesWASDMenu);
                            }
                            else
                            {
                                _menuFactory.OpenMenu(_pluginState.VoteModesMenu, player);
                            }
                        }
                        break;
                        case "!changesetting":
                        if (player != null && _config.Votes.Enabled && _config.Votes.GameSettings)
                        {
                            if (_config.Maps.Style.Equals("wasd") && _pluginState.VoteSettingsWASDMenu != null)
                            {
                                _menuFactory.OpenWasdMenu(player, _pluginState.VoteSettingsWASDMenu);
                            }
                            else
                            {
                                _menuFactory.OpenMenu(_pluginState.VoteSettingsMenu, player);
                            }
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

        public void LoadWASDMenu()
        {
             if (_config.Commands.Style.Equals("wasd"))
            {
                // Assign menu
                _pluginState.GameWASDMenu = _menuFactory.AssignWasdMenu("Game Commands");

                // Add menu options for each command in the command list
                foreach (string _command in _pluginState.PlayerCommands)
                {
                    _pluginState.GameWASDMenu?.Add(_command, (player, option) =>
                    {
                        // Close menu
                        _menuFactory.CloseWasdMenu(player);

                        switch(option.OptionDisplay)
                        {
                            case "!changemap":
                            if (player != null && _config.Votes.Enabled && _config.Votes.Maps && !_config.Votes.AllMaps)
                            {
                                if (_config.Maps.Style.Equals("wasd") && _pluginState.VoteMapWASDMenu != null)
                                {
                                    _menuFactory.OpenWasdMenu(player, _pluginState.VoteMapWASDMenu);
                                }
                                else
                                {
                                    _menuFactory.OpenMenu(_pluginState.VoteMapMenu, player);
                                }
                            }
                            else if(player != null && _config.Votes.Enabled && _config.Votes.Maps && _config.Votes.AllMaps)
                            {
                                if (_config.Maps.Style.Equals("wasd")  && _pluginState.VoteMapsWASDMenu != null)
                                {
                                    _menuFactory.OpenWasdMenu(player, _pluginState.VoteMapsWASDMenu);
                                }
                                else
                                {
                                    _menuFactory.OpenMenu(_pluginState.VoteMapsMenu, player);
                                }
                            }
                            break;
                            case "!changemode":
                            if (player != null && _config.Votes.Enabled && _config.Votes.GameModes)
                            {
                                if (_config.Maps.Style.Equals("wasd") && _pluginState.VoteModesWASDMenu != null)
                                {
                                    _menuFactory.OpenWasdMenu(player, _pluginState.VoteModesWASDMenu);
                                }
                                else
                                {
                                    _menuFactory.OpenMenu(_pluginState.VoteModesMenu, player);
                                }
                            }
                            break;
                            case "!changesetting":
                            if (player != null && _config.Votes.Enabled && _config.Votes.GameSettings)
                            {
                                if (_config.Maps.Style.Equals("wasd") && _pluginState.VoteSettingsWASDMenu != null)
                                {
                                    _menuFactory.OpenWasdMenu(player, _pluginState.VoteSettingsWASDMenu);
                                }
                                else
                                {
                                    _menuFactory.OpenMenu(_pluginState.VoteSettingsMenu, player);
                                }
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
}