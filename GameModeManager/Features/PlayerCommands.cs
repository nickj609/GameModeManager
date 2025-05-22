// Included libraries
using GameModeManager.Core;
using GameModeManager.Menus;
using GameModeManager.Contracts;
using WASDMenuAPI.Shared.Models;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class PlayerCommands : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Plugin? _plugin;
        private GameRules _gameRules;
        private PluginState _pluginState;
        private VoteManager _voteManager;
        private MenuFactory? _menuFactory;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;
        private Config _config = new Config();
        private NominateManager _nominateManager;
        private TimeLimitManager _timeLimitManager;
        private MaxRoundsManager _maxRoundsManager;
        private AsyncVoteManager _asyncVoteManager;
        private VoteOptionManager _voteOptionManager;

        // Define class instance
        public PlayerCommands(PluginState pluginState, IStringLocalizer iLocalizer, ServerManager serverManager, VoteManager voteManager, GameRules gameRules,
        MaxRoundsManager maxRoundsManager, TimeLimitManager timeLimitManager, VoteOptionManager voteOptionManagerManager, NominateManager nominateManager, AsyncVoteManager asyncVoteManager)
        {
            _gameRules = gameRules;
            _pluginState = pluginState;
            _voteManager = voteManager;
            _serverManager = serverManager;
            _nominateManager = nominateManager;
            _timeLimitManager = timeLimitManager;
            _maxRoundsManager = maxRoundsManager;
            _asyncVoteManager = asyncVoteManager;
            _voteOptionManager = voteOptionManagerManager;
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
            _plugin = plugin;
            _menuFactory = new MenuFactory(_plugin);

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
            MapMenus? _mapMenus = new MapMenus(_plugin, _pluginState, _localizer, _serverManager, _config);

            if (player != null)
            {
                if (_config.Votes.Style.Equals("wasd"))
                {
                    _mapMenus?.WasdMenus.Load();
                    IWasdMenu? menu = _mapMenus?.WasdMenus.VoteMenu;

                    if (menu != null)
                    {
                        _menuFactory?.WasdMenus.OpenMenu(player, menu);
                    }
                }
                else
                {
                    _mapMenus.BaseMenus.Load();
                    BaseMenu menu = _mapMenus.BaseMenus.VoteMenu;
                    menu.Title = _localizer.Localize("modes.menu-title");
                    _menuFactory?.BaseMenus.OpenMenu(menu, player);
                }
            }              
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            ModeMenus? _modeMenus = new ModeMenus(_plugin, _pluginState, _localizer, _serverManager, _config);

            if (player != null)
            {
                if (_config.Votes.Style.Equals("wasd"))
                {
                    _modeMenus.WasdMenus.Load();
                    IWasdMenu? menu = _modeMenus.WasdMenus.VoteMenu;

                    if (menu != null)
                    {
                        _menuFactory?.WasdMenus.OpenMenu(player, menu);
                    }
                }
                else
                {
                    _modeMenus.BaseMenus.Load();
                    BaseMenu menu = _modeMenus.BaseMenus.VoteMenu;
                    menu.Title = _localizer.Localize("modes.menu-title");
                    _menuFactory?.BaseMenus.OpenMenu(menu, player);
                }
            } 
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeSettingCommand(CCSPlayerController? player, CommandInfo command)
        {
            SettingMenus? _settingMenus = new SettingMenus(_plugin, _pluginState, _localizer, _config);

            if (player != null)
            {
                if (_config.Settings.Style.Equals("wasd"))
                {
                    _settingMenus.WasdMenus.Load();
                    IWasdMenu? menu = _settingMenus.WasdMenus.VoteMenu;

                    if (menu != null)
                    {
                        menu.Title = _localizer.Localize("settings.menu-actions");
                        _menuFactory?.WasdMenus.OpenMenu(player, menu);
                    }
                }
                else
                {
                    _settingMenus.BaseMenus.Load();
                    BaseMenu menu = _settingMenus.BaseMenus.VoteMenu;

                    if (menu != null)
                    {
                        menu.Title = _localizer.Localize("settings.menu-actions");
                        _menuFactory?.BaseMenus.OpenMenu(menu, player);
                    }
                }
            }
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnGameCommand(CCSPlayerController? player, CommandInfo command)
        {
            MapMenus? _mapMenus = new MapMenus(_plugin, _pluginState, _localizer, _serverManager, _config);
            ModeMenus? _modeMenus = new ModeMenus(_plugin, _pluginState, _localizer, _serverManager, _config);
            SettingMenus? _settingMenus = new SettingMenus(_plugin, _pluginState, _localizer, _config);
            NominateMenus? _nominateMenus = new NominateMenus(_plugin, _pluginState, _localizer, _voteOptionManager, _nominateManager, _config);
            PlayerMenu? _playerMenu = new PlayerMenu(_plugin, _pluginState, _localizer, _timeLimitManager, _gameRules, _voteManager, _maxRoundsManager, _asyncVoteManager, _mapMenus, _modeMenus, _settingMenus, _nominateMenus, _config);

            if (player != null)
            {
                if (_config.Commands.Style.Equals("wasd"))
                {
                    _playerMenu.WasdMenus.Load();
                    IWasdMenu? menu = _playerMenu.WasdMenus.MainMenu;

                    if (menu != null)
                    {
                        _menuFactory?.WasdMenus.OpenMenu(player, menu);
                    }
                }
                else
                {
                    _playerMenu.BaseMenus.Load();
                    BaseMenu menu = _playerMenu.BaseMenus.MainMenu;

                    if (menu != null)
                    {
                        menu.Title = _localizer.Localize("game.menu-title");
                        _menuFactory?.BaseMenus.OpenMenu(menu, player);
                    }
                }
            }
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnCurrentMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                player.PrintToChat(_localizer.Localize("currentmap.message", _pluginState.Game.CurrentMap.DisplayName));
            }
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnCurrentModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                player.PrintToChat(_localizer.Localize("currentmode.message", _pluginState.Game.CurrentMode.Name));
            }
        } 
    }
}