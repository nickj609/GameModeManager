// Included libraries
using GameModeManager.Contracts;
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
        // Define dependencies
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;
        private Config _config = new Config();

        // Define class instance
        public PlayerCommands(PluginState pluginState, IStringLocalizer iLocalizer, MenuFactory menuFactory)
        {
            _pluginState = pluginState;
            _menuFactory = menuFactory;
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
            plugin.AddCommand("css_changemap", "Displays the vote map menu.", OnChangeMapCommand);
            plugin.AddCommand("css_changemode", "Displays the vote mode menu", OnChangeModeCommand);
            plugin.AddCommand("css_changesetting", "Displays the vote setting menu", OnChangeSettingCommand);
        }

        // Define show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _config.Votes.Enabled && _config.Votes.Maps && !_config.Votes.AllMaps)
            {
                if (_config.Votes.Style.Equals("wasd") && _pluginState.VoteMapWASDMenu != null)
                {
                    _menuFactory.OpenWasdMenu(player, _pluginState.VoteMapWASDMenu);
                }
                else
                {
                    _pluginState.VoteMapMenu.Title = _localizer.Localize("maps.menu-title");
                    _menuFactory.OpenMenu(_pluginState.VoteMapMenu, player);
                }

            }   
            else if(player != null && _config.Votes.Enabled && _config.Votes.AllMaps)
            {     
                if (_config.Votes.Style.Equals("wasd") && _pluginState.VoteMapsWASDMenu != null)
                {
                    _menuFactory.OpenWasdMenu(player, _pluginState.VoteMapsWASDMenu);
                }
                else
                { 
                    _pluginState.VoteMapsMenu.Title = _localizer.Localize ("modes.menu-title");
                    _menuFactory.OpenMenu(_pluginState.VoteMapsMenu, player);
                }
                
            }              
        }

        // Define show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(_config.Votes.Enabled && _config.Votes.GameModes)
            {
                if(player != null)
                {
                    if (_config.Votes.Style.Equals("wasd") && _pluginState.VoteModesWASDMenu != null)
                    {
                        _menuFactory.OpenWasdMenu(player, _pluginState.VoteModesWASDMenu);
                    }
                    else
                    {
                        _pluginState.VoteModesMenu.Title = _localizer.Localize("modes.menu-title");
                        _menuFactory.OpenMenu(_pluginState.VoteModesMenu, player);
                        
                    }
                } 
            }
        }

        // Define show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeSettingCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _config.Votes.Enabled && _config.Votes.GameSettings)
            {
                if (_config.Votes.Style.Equals("wasd") && _pluginState.VoteSettingsWASDMenu != null)
                {
                    _menuFactory.OpenWasdMenu(player, _pluginState.VoteSettingsWASDMenu);
                }
                else
                {
                    _pluginState.VoteSettingsMenu.Title = _localizer.Localize("settings.menu-title");
                    _menuFactory.OpenMenu(_pluginState.VoteSettingsMenu, player);
                }
            }
        }

        // Define game menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnGameCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                if (_config.Commands.Style.Equals("wasd") && _pluginState.GameWASDMenu != null)
                {
                    _menuFactory.OpenWasdMenu(player, _pluginState.GameWASDMenu);
                }
                else
                {
                    _pluginState.GameMenu.Title = _localizer.Localize("game.menu-title");
                    _menuFactory.OpenMenu(_pluginState.GameMenu, player);
                }
            }
        }

        // Define current map command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnCurrentMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                player.PrintToChat(_localizer.Localize("currentmap.message", _pluginState.CurrentMap.DisplayName));
            }
        }

        // Define current mode command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnCurrentModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                player.PrintToChat(_localizer.Localize("currentmode.message", _pluginState.CurrentMode.Name));
            }
        } 
    }
}
