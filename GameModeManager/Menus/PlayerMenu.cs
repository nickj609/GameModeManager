// Included libraries
using WASDSharedAPI;
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
        // Define class dependencies
        private MapMenus _mapMenus;
        private ModeMenus _modeMenus;
        private GameRules _gameRules;
        private VoteManager _voteManager;
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;
        private SettingMenus _settingMenus;
        private Config _config = new Config();
        private NominateMenus _nominateMenus;
        private TimeLimitManager _timeLimitManager;
        private MaxRoundsManager _maxRoundsManager;

        // Define class instance
        public PlayerMenu(MenuFactory menuFactory, PluginState pluginState, IStringLocalizer iLocalizer, TimeLimitManager timeLimitManager, GameRules gameRules, SettingMenus settingMenus, NominateMenus nominateMenus, MapMenus mapMenus, ModeMenus modeMenus, VoteManager voteManager, MaxRoundsManager maxRoundsManager)
        {
            _mapMenus = mapMenus;
            _modeMenus = modeMenus;
            _gameRules = gameRules;
            _voteManager = voteManager;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _settingMenus = settingMenus;
            _nominateMenus = nominateMenus;
            _timeLimitManager = timeLimitManager;
            _maxRoundsManager = maxRoundsManager;
            _localizer = new StringLocalizer(iLocalizer, "timeleft.prefix");
        }

        // Define class properties
        private IWasdMenu? playerWasdMenu;
        private BaseMenu playerMenu = new ChatMenu("Command List");

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define methods to get menus
        public BaseMenu GetMenu()
        {
            return playerMenu;
        }

        public IWasdMenu? GetWasdMenu()
        {
            return playerWasdMenu;
        }

        // Define method to update the game command menu
        public void Load()
        {
            playerMenu = _menuFactory.AssignMenu(_config.Settings.Style, "Game Commands");

            // Add menu options for each command in the command list
            foreach (string _command in _pluginState.PlayerCommands)
            {
                switch(_command)
                {
                    case "!changemap":
                    playerMenu.AddMenuOption("Change Map", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Open sub menu
                        if (player != null && _config.Votes.Enabled && _config.Votes.Maps && _config.Maps.Mode == 0)
                        {
                            BaseMenu menu;
                            menu = _mapMenus.GetMenu("VoteCurrentMode");
                            _menuFactory.OpenMenu(menu, player);
                        }
                        else if(player != null && _config.Votes.Enabled && _config.Votes.Maps &&  _config.Maps.Mode == 1)
                        {
                             BaseMenu menu;
                            menu = _mapMenus.GetMenu("VoteAll");
                            _menuFactory.OpenMenu(menu, player);
                        }
                    });
                    break;
                    case "!changemode":
                    playerMenu.AddMenuOption("Change Mode", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Open sub menu
                        if (player != null && _config.Votes.Enabled && _config.Votes.GameModes)
                        {
                            BaseMenu menu;
                            menu = _modeMenus.GetMenu("Vote");
                            _menuFactory.OpenMenu(menu, player);
                        }
                    });
                    break;
                    case "!changesetting":
                   playerMenu.AddMenuOption("Change Setting", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Open sub menu
                        if (player != null && _config.Votes.Enabled && _config.Votes.GameSettings)
                        {
                            _menuFactory.OpenMenu(_settingMenus.GetMenu("Vote"), player);
                        }
                    });
                    break;
                    case "!currentmode":
                    playerMenu.AddMenuOption("Current Mode", (player, option) =>
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
                    playerMenu.AddMenuOption("Current Map", (player, option) =>
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
                    playerMenu.AddMenuOption("Next Map", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Print to chat
                        if (player != null)
                        {
                            if (_pluginState.NextMap != null && _pluginState.NextMode == null)
                            {
                                player.PrintToChat(_localizer.Localize("rtv.nextmap.message", _pluginState.NextMap.DisplayName));
                            }
                            else if (_pluginState.NextMap == null && _pluginState.NextMode != null)
                            {
                                player.PrintToChat(_localizer.Localize("rtv.nextmap.message", "Random"));
                            }
                            else
                            {
                                player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.no-vote"));
                            }
                        }
                    });
                    break;
                    case "!nextmode":
                    playerMenu.AddMenuOption("Next Mode", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Print to chat
                        if (player != null)
                        {
                            if (_pluginState.NextMode != null)
                            {
                                player.PrintToChat(_localizer.Localize("rtv.nextmode.message", _pluginState.NextMode.Name));
                            }
                            else if (_pluginState.NextMap != null && _pluginState.NextMode == null)
                            {
                                player.PrintToChat(_localizer.Localize("rtv.nextmode.message", _pluginState.CurrentMode.Name));
                            }
                            else
                            {
                                player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.no-vote"));
                            }
                        }
                    });
                    break;
                    case "!rtv":
                    playerMenu.AddMenuOption("RockTheVote", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Start vote
                        player.ExecuteClientCommand("css_rtv");
                        
                    });
                    break;
                    case "!nominate":
                    playerMenu.AddMenuOption("Nominate", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Open nomination menu
                        if (player != null)
                        {
                            if (_pluginState.EofVoteHappened)
                            {
                                if (!_timeLimitManager.UnlimitedTime())
                                {
                                    string timeleft = _voteManager.GetTimeLeft();
                                    player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix","rtv.schedule-change", timeleft));
                                }
                                else if (!_maxRoundsManager.UnlimitedRounds)
                                {
                                    string roundsleft = _voteManager.GetRoundsLeft();
                                    player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix","rtv.schedule-change", roundsleft));
                                }
                                return;
                            }

                            if (_pluginState.DisableCommands || !_pluginState.NominationEnabled)
                            {
                                player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.disabled"));
                                return;
                            }

                            if (_gameRules.WarmupRunning)
                            {
                                if (!_config.RTV.EnabledInWarmup)
                                {
                                    player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.warmup"));
                                    return;
                                }
                            }
                            else if (_config.RTV.MinRounds > 0 && _config.RTV.MinRounds > _gameRules.TotalRoundsPlayed)
                            {
                                player!.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.minimum-rounds", _config.RTV.MinRounds));
                                return;
                            }

                            if (Extensions.ValidPlayerCount() < _config!.RTV.MinPlayers)
                            {
                                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-players", _config!.RTV.MinPlayers));
                                return;
                            }

                            if (_config.RTV.IncludeModes)
                            {
                                BaseMenu menu;
                                menu = _nominateMenus.GetMenu("All");
                                _menuFactory.OpenMenu(menu, player);
                            }
                            else
                            {
                                BaseMenu menu;
                                menu = _nominateMenus.GetMenu("Map");
                                _menuFactory.OpenMenu(menu, player);
                            }
                        }
                    });
                    break;
                    case "!timeleft":
                    playerMenu.AddMenuOption("Time Left", (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Print to chat
                        if (player != null)
                        {
                            player.PrintToChat(_localizer.LocalizeWithPrefixInternal("timeleft.prefix", _timeLimitManager.GetTimeLeftMessage()));
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
                playerWasdMenu = _menuFactory.AssignWasdMenu("Game Commands");

                // Add menu options for each command in the command list
                foreach (string _command in _pluginState.PlayerCommands)
                {
                    switch(_command)
                    {
                        case "!changemap":
                        playerWasdMenu?.Add("Change Map", (player, option) =>
                        {
                            if (player != null && _config.Votes.Enabled )
                            {
                                if (_config.Votes.Maps && _config.Maps.Mode == 1)
                                {
                                    IWasdMenu? menu;
                                    menu = _mapMenus.GetWasdMenu("VoteAll");

                                    if (menu != null)
                                    {
                                        menu.Prev = option.Parent?.Options?.Find(option);
                                        _menuFactory.OpenWasdSubMenu(player, menu);
                                    }
                                }
                                else if (_config.Votes.Maps && _config.Maps.Mode == 0)
                                {
                                    IWasdMenu? menu;
                                    menu = _mapMenus.GetWasdMenu("VoteCurrentMode");

                                    if (menu != null)
                                    {
                                        menu.Prev = option.Parent?.Options?.Find(option);
                                        _menuFactory.OpenWasdSubMenu(player, menu);
                                    }
                                }
                            }
                        });
                        break;
                        case "!changemode":
                        playerWasdMenu?.Add("Change Mode", (player, option) =>
                        {
                            if (player != null && _config.Votes.Enabled && _config.Votes.GameModes)
                            {
                                IWasdMenu? menu;
                                menu = _mapMenus.GetWasdMenu("Vote");

                                if (menu != null)
                                {
                                    menu.Prev = option.Parent?.Options?.Find(option);
                                    _menuFactory.OpenWasdMenu(player, menu);
                                }
                            }
                        });
                        break;
                        case "!changesetting":
                        playerWasdMenu?.Add("Change Setting", (player, option) =>
                        {
                            if (player != null && _config.Votes.Enabled && _config.Votes.GameSettings)
                            {
                                IWasdMenu? menu;
                                menu = _settingMenus.GetWasdMenu("Vote");

                                if (menu != null)
                                {
                                    menu.Prev = option.Parent?.Options?.Find(option);
                                    _menuFactory.OpenWasdSubMenu(player, menu);

                                }
                            }
                        });
                        break;
                        case "!currentmode":
                        playerWasdMenu?.Add("Current Mode", (player, option) =>
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
                        playerWasdMenu?.Add("Current Map", (player, option) =>
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
                        playerWasdMenu?.Add("Time Left", (player, option) =>
                        {
                            // Close menu
                            _menuFactory.CloseWasdMenu(player);

                            // Print to chat
                            if (player != null)
                            {
                                player.PrintToChat(_localizer.LocalizeWithPrefixInternal("timeleft.prefix", _timeLimitManager.GetTimeLeftMessage()));
                            }
                        });
                        break;
                        case "!nextmap":
                        playerWasdMenu?.Add("Next Map", (player, option) =>
                        {
                             // Close menu
                            _menuFactory.CloseWasdMenu(player);

                            // Print to chat
                            if (player != null)
                            {
                                if (_pluginState.NextMap != null && _pluginState.NextMode == null)
                                {
                                    player.PrintToChat(_localizer.Localize("rtv.nextmap.message", _pluginState.NextMap.DisplayName));
                                }
                                else if (_pluginState.NextMap == null && _pluginState.NextMode != null)
                                {
                                    player.PrintToChat(_localizer.Localize("rtv.nextmap.message", "Random"));
                                }
                                else
                                {
                                    player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.no-vote"));
                                }
                            }
                        });
                        break;
                        case "!nextmode":
                        playerWasdMenu?.Add("Next Mode", (player, option) =>
                        {
                             // Close menu
                            _menuFactory.CloseWasdMenu(player);

                            // Print to chat
                            if (player != null)
                            {
                                if (_pluginState.NextMode != null)
                                {
                                    player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "rtv.nextmode.message", _pluginState.NextMode.Name));
                                }
                                else if (_pluginState.NextMap != null && _pluginState.NextMode == null)
                                {
                                    player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "rtv.nextmode.message", _pluginState.CurrentMode.Name));
                                }
                                else
                                {
                                    player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.no-vote"));
                                }
                            }
                        });
                        break;
                        case "!rtv":
                        playerWasdMenu?.Add("RockTheVote", (player, option) =>
                        {
                            // Close menu
                            _menuFactory.CloseWasdMenu(player);

                            // Start vote
                            player.ExecuteClientCommand("css_rtv");
                        });
                        break;
                        case "!nominate":
                        playerWasdMenu?.Add("Nominate", (player, option) =>
                        {
                            // Close menu
                            _menuFactory.CloseWasdMenu(player);

                            // Open nomination menu
                            if (player != null)
                            {
                                if (_pluginState.EofVoteHappened)
                                {
                                    if (!_timeLimitManager.UnlimitedTime())
                                    {
                                        string timeleft = _voteManager.GetTimeLeft();
                                        player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix","rtv.schedule-change", timeleft));
                                    }
                                    else if (!_maxRoundsManager.UnlimitedRounds)
                                    {
                                        string roundsleft = _voteManager.GetRoundsLeft();
                                        player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix","rtv.schedule-change", roundsleft));
                                    }
                                    return;
                                }

                                if (_pluginState.DisableCommands || !_pluginState.NominationEnabled)
                                {
                                    player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.disabled"));
                                    return;
                                }

                                if (_gameRules.WarmupRunning)
                                {
                                    if (!_config.RTV.EnabledInWarmup)
                                    {
                                        player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.warmup"));
                                        return;
                                    }
                                }
                                else if (_config.RTV.MinRounds > 0 && _config.RTV.MinRounds > _gameRules.TotalRoundsPlayed)
                                {
                                    player!.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.minimum-rounds", _config.RTV.MinRounds));
                                    return;
                                }

                                if (Extensions.ValidPlayerCount() < _config!.RTV.MinPlayers)
                                {
                                    player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.minimum-players", _config!.RTV.MinPlayers));
                                    return;
                                }

                                if (_config.RTV.IncludeModes)
                                {
                                    IWasdMenu? menu;
                                    menu = _nominateMenus.GetWasdMenu("All");

                                    if (menu != null)
                                    {
                                        menu.Prev = option.Parent?.Options?.Find(option);
                                        _menuFactory.OpenWasdSubMenu(player, menu);
                                    }
                                }
                                else
                                {
                                    IWasdMenu? menu;
                                    menu = _nominateMenus.GetWasdMenu("Map");

                                    if (menu != null)
                                    {
                                        menu.Prev = option.Parent?.Options?.Find(option);
                                        _menuFactory.OpenWasdSubMenu(player, menu);
                                    }
                                }
                            }
                        });
                        break;
                    }
                }
            }
        }
    }
}