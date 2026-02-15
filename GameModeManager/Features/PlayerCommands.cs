// Included libraries
using GameModeManager.Menus;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class PlayerCommands : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private MapMenus _mapMenus;
        private ModeMenus _modeMenus;
        private Config _config = new();
        private PlayerMenu _playerMenu;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private SettingMenus _settingMenus;

        // Define class constructor
        public PlayerCommands(PluginState pluginState, IStringLocalizer iLocalizer, MapMenus mapMenus, ModeMenus modeMenus, SettingMenus settingMenus, PlayerMenu playerMenu)
        {
            _mapMenus = mapMenus;
            _modeMenus = modeMenus;
            _playerMenu = playerMenu;
            _pluginState = pluginState;
            _settingMenus = settingMenus;
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
            plugin.AddCommand("css_game", "Displays list of player commands", OnGameCommand);
            plugin.AddCommand("css_currentmap", "Displays current map.", OnCurrentMapCommand);
            plugin.AddCommand("css_currentmode", "Displays current map.", OnCurrentModeCommand);

            if(_config.Votes.Enabled)
            {
                if (_config.Votes.Maps)
                    plugin.AddCommand("css_changemap", "Displays the vote map menu.", OnChangeMapCommand);

                if (_config.Votes.GameModes)
                    plugin.AddCommand("css_changemode", "Displays the vote mode menu", OnChangeModeCommand);

                if (_config.Votes.GameSettings)
                    plugin.AddCommand("css_changesetting", "Displays the vote setting menu", OnChangeSettingCommand);
            }
        }

        // Define command handlers
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
                _mapMenus.VoteMenu?.Open(player);             
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
                _modeMenus.VoteMenu?.Open(player);
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeSettingCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
                _settingMenus.VoteMenu?.Open(player);
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnGameCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
                _playerMenu.MainMenu?.Open(player);
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnCurrentMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
                command.ReplyToCommand(_localizer.Localize("currentmap.message", _pluginState.Game.CurrentMap.DisplayName));
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnCurrentModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
                command.ReplyToCommand(_localizer.Localize("currentmode.message", _pluginState.Game.CurrentMode.Name));
        } 
    }
}