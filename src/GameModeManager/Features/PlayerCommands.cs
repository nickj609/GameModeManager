// Included libraries
using MenuManager;
using GameModeManager.Menus;
using GameModeManager.Contracts;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Admin;
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

        // Define class properties
        private const string RandomVotePermission = "@css/changemap"; // The same flag as !maps and !modes

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

            // Player votes (VoteService); the entries also appear in the !game menu
            if(_config.Votes.Enabled)
            {
                plugin.AddCommand("css_changemap", "Starts a map vote. Usage: css_changemap [map]", OnChangeMapCommand);
                _pluginState.Game.PlayerCommands.Add("!changemap");

                if (_config.Votes.GameModes)
                {
                    plugin.AddCommand("css_changemode", "Starts a mode vote. Usage: css_changemode [mode]", OnChangeModeCommand);
                    _pluginState.Game.PlayerCommands.Add("!changemode");
                }

                if (_config.Votes.GameSettings)
                {
                    plugin.AddCommand("css_changesetting", "Displays the vote setting menu", OnChangeSettingCommand);
                    _pluginState.Game.PlayerCommands.Add("!changesetting");
                }
            }
        }

        // Define command handlers
        // No argument: a random vote on GG1's panel for admins, the list to pick from for everyone else. A map's name: a yes/no vote on it.
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
                return;

            string name = ArgumentOf(command);
            if (name.Length == 0)
            {
                if (AdminManager.PlayerHasPermissions(player, RandomVotePermission))
                    _mapMenus.StartPlayerVote(player);
                else
                    _mapMenus.VoteMenu?.Open(player);
                return;
            }

            IMap? map = _pluginState.Game.CurrentMode.Maps.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || m.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (map != null)
                _mapMenus.StartVote(player, map);
            else
                PlayerFeedback.Send(player, _localizer, "notify.vote", MenuNotice.Warning, "vote.no-match", name);
        }

        // No argument: a random vote on GG1's panel for admins, the list to pick from for everyone else. A mode's name: a yes/no vote on it.
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
                return;

            string name = ArgumentOf(command);
            if (name.Length == 0)
            {
                if (AdminManager.PlayerHasPermissions(player, RandomVotePermission))
                    _modeMenus.StartVote(player);
                else
                    _modeMenus.OpenVoteMenu(player);
                return;
            }

            IMode? mode = _pluginState.Game.Modes.Values.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            bool admin = AdminManager.PlayerHasPermissions(player, RandomVotePermission);
            if (mode != null && (_config.GameModes.InVotes(mode.Name) || admin))
            {
                // Admins can start a vote on any mode; players only on one for this many players
                if (admin || _modeMenus.FitsOrTell(player, mode))
                    _modeMenus.StartVote(player, mode);
            }
            else
                PlayerFeedback.Send(player, _localizer, "notify.vote", MenuNotice.Warning, "vote.no-match", name);
        }

        private static string ArgumentOf(CommandInfo command)
        {
            return command.ArgCount > 1 ? command.ArgString.Trim().Trim('"').Trim() : "";
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeSettingCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
                _settingMenus.OpenVoteMenu(player);
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
            {
                PlayerFeedback.NotifyText(player, _localizer, "commands.menu-currentmap", MenuNotice.Success, _pluginState.Game.CurrentMap.DisplayName);
                if (PlayerFeedback.ChatLine)
                    command.ReplyToCommand(_localizer.Localize("currentmap.message", _pluginState.Game.CurrentMap.DisplayName));
            }
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnCurrentModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                PlayerFeedback.NotifyText(player, _localizer, "commands.menu-currentmode", MenuNotice.Success, _pluginState.Game.CurrentMode.Name);
                if (PlayerFeedback.ChatLine)
                    command.ReplyToCommand(_localizer.Localize("currentmode.message", _pluginState.Game.CurrentMode.Name));
            }
        } 
    }
}