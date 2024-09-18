// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
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
        public PlayerCommands(PluginState pluginState, StringLocalizer localizer, MenuFactory menuFactory)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
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
                _pluginState.ShowMapMenu.Title = _localizer.Localize("maps.menu-title");
                _menuFactory.OpenMenu(_pluginState.ShowMapMenu, _config.GameModes.Style, player);

            }   
            else if(player != null && _config.Votes.Enabled && _config.Votes.AllMaps)
            {      
                _pluginState.ShowMapsMenu.Title = _localizer.Localize ("modes.menu-title");
                _menuFactory.OpenMenu(_pluginState.ShowMapsMenu, _config.GameModes.Style, player);
                
            } 
            else
            {
                command.ReplyToCommand("css_showmaps is a client only command.");
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
                    _pluginState.ShowModesMenu.Title = _localizer.Localize("modes.menu-title");
                    _menuFactory.OpenMenu(_pluginState.ShowModesMenu, _config.GameModes.Style, player);
                }
                else if (player == null)
                {
                    command.ReplyToCommand("css_showmodes is a client only command.");
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
                _pluginState.ShowSettingsMenu.Title = _localizer.Localize("settings.menu-title");
                _menuFactory.OpenMenu(_pluginState.ShowSettingsMenu, _config.Settings.Style, player);
            }
            else if (player == null)
            {
                command.ReplyToCommand("css_showsettings is a client only command.");
            }
        }

        // Define game menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnGameCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                _pluginState.GameMenu.Title = _localizer.Localize("game.menu-title");
                _menuFactory.OpenMenu(_pluginState.GameMenu, _config.Settings.Style, player);
            }
            else
            {
                command.ReplyToCommand("css_game is a client only command.");
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
            else
            {
                command.ReplyToCommand("css_game is a client only command.");
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
            else
            {
                command.ReplyToCommand("css_game is a client only command.");
            }
        } 
    }
}
