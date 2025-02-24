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
        private GameRules _gameRules;
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private TimeLimitManager _timeLimitManager;

        // Define class instance
        public PlayerMenu(MenuFactory menuFactory, PluginState pluginState, IStringLocalizer iLocalizer, TimeLimitManager timeLimitManager, GameRules gameRules)
        {
            _gameRules = gameRules;
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

        // Define method to update the game command menu
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
                        if (player != null && _config.Votes.Enabled && _config.Votes.Maps && _config.Maps.Mode == 0)
                        {
                            _menuFactory.OpenMenu(_pluginState.VoteMapMenu, player);
                        }
                        else if(player != null && _config.Votes.Enabled && _config.Votes.Maps &&  _config.Maps.Mode == 1)
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
                    case "!nextmap":
                    _pluginState.GameMenu.AddMenuOption("Current Map", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Print to chat
                        if (player != null)
                        {
                            if (_pluginState.NextMap != null && _pluginState.NextMode == null)
                            {
                                player.PrintToChat(_localizer.Localize("nextmap.message", _pluginState.NextMap.DisplayName));
                            }
                            else if (_pluginState.NextMap == null && _pluginState.NextMode != null)
                            {
                                player.PrintToChat(_localizer.Localize("nextmap.message", "Random"));
                            }
                            else
                            {
                                player.PrintToChat(_localizer.Localize("general.validation.no-vote"));
                            }
                        }
                    });
                    break;
                    case "!nextmode":
                    _pluginState.GameMenu.AddMenuOption("Current Map", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Print to chat
                        if (player != null)
                        {
                            if (_pluginState.NextMode != null)
                            {
                                player.PrintToChat(_localizer.Localize("nextmode.message", _pluginState.NextMode.Name));
                            }
                            else if (_pluginState.NextMap != null && _pluginState.NextMode == null)
                            {
                                player.PrintToChat(_localizer.Localize("nextmode.message", _pluginState.CurrentMode.Name));
                            }
                            else
                            {
                                player.PrintToChat(_localizer.Localize("general.validation.no-vote"));
                            }
                        }
                    });
                    break;
                    case "!rtv":
                    _pluginState.GameMenu.AddMenuOption("Current Map", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Start vote
                        player.ExecuteClientCommand("css_rtv");
                        
                    });
                    break;
                    case "!nominate":
                    _pluginState.GameMenu.AddMenuOption("Current Map", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Open nomination menu
                        if (player != null)
                        {
                            if (_pluginState.DisableCommands || !_config.RTV.NominationEnabled)
                            {
                                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.disabled"));
                                return;
                            }

                            if (_gameRules.WarmupRunning)
                            {
                                if (!_config.RTV.EnabledInWarmup)
                                {
                                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                                    return;
                                }
                            }
                            else if (_config.RTV.MinRounds > 0 && _config.RTV.MinRounds > _gameRules.TotalRoundsPlayed)
                            {
                                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-rounds", _config.RTV.MinRounds));
                                return;
                            }

                            if (Extensions.ValidPlayerCount() < _config!.RTV.MinPlayers)
                            {
                                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-players", _config!.RTV.MinPlayers));
                                return;
                            }

                            if (_config.RTV.IncludeModes)
                            {
                                if (_config.RTV.Style.Equals("wasd", StringComparison.OrdinalIgnoreCase) && _pluginState.NominationWASDMenu != null)
                                {
                                    _menuFactory.OpenWasdMenu(player, _pluginState.NominationWASDMenu);
                                }
                                else
                                {
                                    _menuFactory.OpenMenu(_pluginState.NominationMenu, player);
                                }
                            }
                            else
                            {
                                if (_config.RTV.Style.Equals("wasd", StringComparison.OrdinalIgnoreCase) && _pluginState.NominateMapWASDMenu != null)
                                {
                                    _menuFactory.OpenWasdMenu(player, _pluginState.NominateMapWASDMenu);
                                }
                                else
                                {
                                    _menuFactory.OpenMenu(_pluginState.NominateMapMenu, player);
                                }
                            }
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
                                if (_config.Votes.Maps && _config.Maps.Mode == 1)
                                {
                                    if (_pluginState.VoteMapsWASDMenu != null)
                                    {
                                        _pluginState.VoteMapsWASDMenu.Prev = option.Parent?.Options?.Find(option);
                                        _menuFactory.OpenWasdMenu(player, _pluginState.VoteMapsWASDMenu);
                                    }
                                }
                                else if (_config.Votes.Maps && _config.Maps.Mode == 0)
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