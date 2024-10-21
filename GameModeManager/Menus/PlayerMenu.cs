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
                switch(_command)
                {
                    case "!changemap":
                    _pluginState.GameMenu.AddMenuOption("Change Map", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Open sub menu
                        if (player != null && _config.Votes.Enabled && _config.Votes.Maps && !_config.Votes.AllMaps)
                        {
                            _menuFactory.OpenMenu(_pluginState.VoteMapMenu, player);
                        }
                        else if(player != null && _config.Votes.Enabled && _config.Votes.Maps && _config.Votes.AllMaps)
                        {
                             _menuFactory.OpenMenu(_pluginState.VoteMapsMenu, player);
                        }

                    });
                    break;
                    case "!changemode":
                    _pluginState.GameMenu.AddMenuOption("Change Mode", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Open sub menu
                        if (player != null && _config.Votes.Enabled && _config.Votes.GameModes)
                        {
                            _menuFactory.OpenMenu(_pluginState.VoteModesMenu, player);
                        }
                    });
                    break;
                    case "!changesetting":
                    _pluginState.GameMenu.AddMenuOption("Change Setting", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Open sub menu
                        if (player != null && _config.Votes.Enabled && _config.Votes.GameSettings)
                        {
                            _menuFactory.OpenMenu(_pluginState.VoteSettingsMenu, player);
                        }
                    });
                    break;
                    case "!currentmode":
                    _pluginState.GameMenu.AddMenuOption("Current Mode", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Print to chat
                        if (player != null)
                        {
                            player.PrintToChat(_localizer.Localize("currentmode.message", _pluginState.CurrentMode.Name));
                        }
                    });
                    break;
                    case "!currentmap":
                    _pluginState.GameMenu.AddMenuOption("Current Map", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Print to chat
                        if (player != null)
                        {
                            player.PrintToChat(_localizer.Localize("currentmap.message", _pluginState.CurrentMap.Name));
                        }
                    });
                    break;
                    case "!timeleft":
                    _pluginState.GameMenu.AddMenuOption("Time Left", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Print to chat
                        if (player != null)
                        {
                            player.PrintToChat(_localizer.LocalizeWithPrefix(_timeLimitManager.GetTimeLeftMessage()));
                        }
                    });
                    break;
                }
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
                    switch(_command)
                    {
                        case "!changemap":
                        _pluginState.GameWASDMenu?.Add("Change Map", (player, option) =>
                        {
                            if (player != null && _config.Votes.Enabled )
                            {
                                if (_config.Votes.Maps && !_config.Votes.AllMaps)
                                {
                                    if (_pluginState.VoteMapsWASDMenu != null)
                                    {
                                        _pluginState.VoteMapsWASDMenu.Prev = option.Parent?.Options?.Find(option);
                                        _menuFactory.OpenWasdMenu(player, _pluginState.VoteMapsWASDMenu);
                                    }
                                }
                                else
                                {
                                    if (_pluginState.VoteMapWASDMenu != null)
                                    {
                                        _pluginState.VoteMapWASDMenu.Prev = option.Parent?.Options?.Find(option);
                                        _menuFactory.OpenWasdMenu(player, _pluginState.VoteMapWASDMenu);
                                    }
                                }
                            }
                        });
                        break;
                        case "!changemode":
                        _pluginState.GameWASDMenu?.Add("Change Mode", (player, option) =>
                        {
                            if (player != null && _config.Votes.Enabled && _config.Votes.GameModes)
                            {
                                if (_pluginState.VoteModesWASDMenu != null)
                                {
                                    _pluginState.VoteModesWASDMenu.Prev = option.Parent?.Options?.Find(option);
                                    _menuFactory.OpenWasdMenu(player, _pluginState.VoteModesWASDMenu);
                                }
                            }
                        });
                        break;
                        case "!changesetting":
                        _pluginState.GameWASDMenu?.Add("Change Setting", (player, option) =>
                        {
                            if (player != null && _config.Votes.Enabled && _config.Votes.GameSettings)
                            {
                                if (_pluginState.VoteSettingsWASDMenu != null)
                                {
                                    _pluginState.VoteSettingsWASDMenu.Prev = option.Parent?.Options?.Find(option);
                                    _menuFactory.OpenWasdMenu(player, _pluginState.VoteSettingsWASDMenu);
                                }
                            }
                        });
                        break;
                        case "!currentmode":
                        _pluginState.GameWASDMenu?.Add("Current Mode", (player, option) =>
                        {
                            // Close menu
                            _menuFactory.CloseWasdMenu(player);

                            // Print to chat
                            if (player != null)
                            {
                                player.PrintToChat(_localizer.Localize("currentmode.message", _pluginState.CurrentMode.Name));
                            }
                        });
                        break;
                        case "!currentmap":
                        _pluginState.GameWASDMenu?.Add("Current Map", (player, option) =>
                        {
                            // Close menu
                            _menuFactory.CloseWasdMenu(player);

                            // Print to chat
                            if (player != null)
                            {
                                player.PrintToChat(_localizer.Localize("currentmap.message", _pluginState.CurrentMap.DisplayName));
                            }
                        });
                        break;
                        case "!timeleft":
                        _pluginState.GameWASDMenu?.Add("Time Left", (player, option) =>
                        {
                            // Close menu
                            _menuFactory.CloseWasdMenu(player);

                            // Print to chat
                            if (player != null)
                            {
                                player.PrintToChat(_localizer.LocalizeWithPrefix(_timeLimitManager.GetTimeLeftMessage()));
                            }
                        });
                        break;
                    }
                }
            }
        }
    }
}