// Included libraries
using GameModeManager.Core;
using GameModeManager.Features;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Logging;

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
        private Config _config = new();
        private VoteManager _voteManager;
        private PluginState _pluginState;
        private SettingMenus _settingMenus;
        private StringLocalizer _localizer;
        private NominateMenus _nominateMenus;
        private TimeLimitManager _timeLimitManager;
        private MaxRoundsManager _maxRoundsManager;
        private AsyncVoteManager _asyncVoteManager;
        private ILogger<PlayerMenu> _logger;

        // Define class constructor
        public PlayerMenu(PluginState pluginState, StringLocalizer localizer, TimeLimitManager timeLimitManager, GameRules gameRules, VoteManager voteManager, MaxRoundsManager maxRoundsManager,
        AsyncVoteManager asyncVoteManager, MapMenus mapMenus, ModeMenus modeMenus, SettingMenus settingMenus, NominateMenus nominateMenus, ILogger<PlayerMenu> logger)
        {
            _mapMenus = mapMenus;
            _modeMenus = modeMenus;
            _localizer = localizer;
            _gameRules = gameRules;
            _pluginState = pluginState;
            _voteManager = voteManager;
            _settingMenus = settingMenus;
            _nominateMenus = nominateMenus;
            _asyncVoteManager = asyncVoteManager;
            _timeLimitManager = timeLimitManager;
            _maxRoundsManager = maxRoundsManager;
            _logger = logger;
        }

        // Define class properties
        public IMenu? MainMenu;

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define load method
        public void Load()
        {
            // Create main menu
            MainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("commands.menu-title"));

            foreach (string _command in _pluginState.Game.PlayerCommands)
            {
                switch (_command)
                {
                    case "!changemap":
                        MainMenu?.AddMenuOption(_localizer.Localize("commands.menu-changemap"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);

                            if (_config.Votes.Enabled && _config.Votes.Maps)
                            {
                                _mapMenus.VoteMenu?.Open(player);
                            }
                        });
                        break;
                    case "!changemode":
                        MainMenu?.AddMenuOption(_localizer.Localize("commands.menu-changemode"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);

                            if (_config.Votes.Enabled && _config.Votes.GameModes)
                            {
                                _modeMenus.VoteMenu?.Open(player);
                            }
                        });
                        break;
                    case "!changesetting":
                        MainMenu?.AddMenuOption(_localizer.Localize("commands.menu-changesetting"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);

                            if (_config.Votes.Enabled && _config.Votes.GameSettings)
                            {
                                _settingMenus.MainMenu?.Open(player);
                            }
                        });
                        break;
                    case "!currentmode":
                        MainMenu?.AddMenuOption(_localizer.Localize("commands.menu-currentmode"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);

                            if (player != null)
                            {
                                player.PrintToChat(_localizer.Localize("currentmode.message", _pluginState.Game.CurrentMode.Name));
                            }
                        });
                        break;
                    case "!currentmap":
                        MainMenu?.AddMenuOption(_localizer.Localize("commands.menu-currentmap"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);
                            player.PrintToChat(_localizer.Localize("currentmap.message", _pluginState.Game.CurrentMap.Name));
                        });
                        break;
                    case "!nextmap":
                        MainMenu?.AddMenuOption(_localizer.Localize("commands.menu-nextmap"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);

                            if (_pluginState.RTV.NextMap != null && _pluginState.RTV.NextMode == null)
                            {
                                player.PrintToChat(_localizer.Localize("rtv.nextmap.message", _pluginState.RTV.NextMap.DisplayName));
                            }
                            else if (_pluginState.RTV.NextMap == null && _pluginState.RTV.NextMode != null)
                            {
                                player.PrintToChat(_localizer.Localize("rtv.nextmap.message", "Random"));
                            }
                            else
                            {
                                player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.no-vote"));
                            }
                        });
                        break;
                    case "!nextmode":
                        MainMenu?.AddMenuOption(_localizer.Localize("commands.menu-nextmode"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);
                            if (_pluginState.RTV.NextMode != null)
                            {
                                player.PrintToChat(_localizer.Localize("rtv.nextmode.message", _pluginState.RTV.NextMode.Name));
                            }
                            else if (_pluginState.RTV.NextMap != null && _pluginState.RTV.NextMode == null)
                            {
                                player.PrintToChat(_localizer.Localize("rtv.nextmode.message", _pluginState.Game.CurrentMode.Name));
                            }
                            else
                            {
                                player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.no-vote"));
                            }
                        });
                        break;
                    case "!rtv":
                        MainMenu?.AddMenuOption(_localizer.Localize("commands.menu-rtv"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);
                            _asyncVoteManager.RTVCounter(player);
                        });
                        break;
                    case "!nominate":
                        MainMenu?.AddMenuOption(_localizer.Localize("commands.menu-nominate"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);

                            if (_pluginState.RTV.EofVoteHappened)
                            {
                                if (!_timeLimitManager.UnlimitedTime())
                                {
                                    player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "rtv.schedule-change", _voteManager.GetTimeLeft()));
                                }
                                else if (!_maxRoundsManager.UnlimitedRounds)
                                {
                                    player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "rtv.schedule-change", _voteManager.GetRoundsLeft()));
                                }
                                return;
                            }

                            if (_pluginState.RTV.DisableCommands || !_pluginState.RTV.NominationEnabled)
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

                            if (PlayerExtensions.ValidPlayerCount() < _config!.RTV.MinPlayers)
                            {
                                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-players", _config!.RTV.MinPlayers));
                                return;
                            }

                            _nominateMenus.MainMenu?.Open(player);
                        });
                        break;
                    case "!timeleft":
                        MainMenu?.AddMenuOption(_localizer.Localize("commands.menu-timeleft"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);

                            if (player != null)
                            {
                                player.PrintToChat(_localizer.LocalizeWithPrefixInternal("timeleft.prefix", _timeLimitManager.GetTimeLeftMessage()));
                            }
                        });
                        break;
                }
            }
        }
    }
}