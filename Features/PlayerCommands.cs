// Included libraries
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager
{
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
            plugin.AddCommand("css_showmaps", "Shows a list of maps.", OnShowMapCommand);
            plugin.AddCommand("css_game", "Provides a list of game commands.", OnGameCommand);
            plugin.AddCommand("css_currentmap", "Displays current map.", OnCurrentMapCommand);
            plugin.AddCommand("css_currentmode", "Displays current map.", OnCurrentModeCommand);
            plugin.AddCommand("css_showmodes", "Shows a list of game modes.", OnShowModesCommand);
            plugin.AddCommand("css_showallmaps", "Shows a list of all maps.", OnShowAllMapsCommand);
            plugin.AddCommand("css_showsettings", "Provides a list of game settings.", OnShowSettingsCommand);
        }

        // Define show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnShowMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(_config.Votes.Enabled && _config.Votes.Maps)
            {
                if(player != null)
                {
                    // Open menu
                    _pluginState.ShowMapMenu.Title = _localizer.Localize ("maps.menu-title");
                    _menuFactory.OpenMenu(_pluginState.ShowMapMenu, _config.GameModes.Style, player);
                }
                else if (player == null)
                {
                    command.ReplyToCommand("css_showmaps is a client only command.");
                }
            }            
        }

        // Define show all maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnShowAllMapsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(_config.Votes.Enabled && _config.Votes.AllMaps)
            {
                if(player != null)
                {
                    // Open menu
                    _pluginState.ShowMapsMenu.Title = _localizer.Localize ("modes.menu-title");
                    _menuFactory.OpenMenu(_pluginState.ShowMapsMenu, _config.GameModes.Style, player);
                }
                else if (player == null)
                {
                    command.ReplyToCommand("css_showmaps is a client only command.");
                }
            }            
        }

        // Define show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnShowModesCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(_config.Votes.Enabled && _config.Votes.GameModes)
            {

                if(player != null)
                {
                    // Open menu
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
        public void OnShowSettingsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _config.Votes.Enabled && _config.Votes.GameSettings)
            {
                // Open menu
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
                // Open menu
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
                // Create message
                string _message = _localizer.Localize("currentmap.message", _pluginState.CurrentMap.DisplayName);

                // Write to chat
                player.PrintToChat(_message);
            }
            else
            {
                command.ReplyToCommand("css_game is a client only command.");
            }
        }

        // Define current map command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnCurrentModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                // Create message
                string _message = _localizer.Localize("currentmode.message", _pluginState.CurrentMode.Name);

                // Write to chat
                player.PrintToChat(_message);
            }
            else
            {
                command.ReplyToCommand("css_game is a client only command.");
            }
        }
    }
}
