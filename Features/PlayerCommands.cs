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
        private Config? _config;
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;

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
            plugin.AddCommand("css_showmaps", "Shows a list of maps.", OnShowMapsCommand);
            plugin.AddCommand("css_game", "Provides a list of game commands.", OnGameCommand);
            plugin.AddCommand("css_currentmap", "Displays current map.", OnCurrentMapCommand);
            plugin.AddCommand("css_currentmode", "Displays current map.", OnCurrentModeCommand);
            plugin.AddCommand("css_showmodes", "Shows a list of game modes.", OnShowModesCommand);
            plugin.AddCommand("css_showsettings", "Provides a list of game settings.", OnShowSettingsCommand);
        }

        // Define show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnShowMapsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(_config != null && _config.Votes.Enabled && _config.Votes.Map)
            {
                if(player != null && _pluginState.ShowMapsMenu != null)
                {
                    // Open menu
                    _pluginState.ShowMapsMenu.Title = _localizer.Localize ("maps.menu-title");
                    _menuFactory.OpenMenu(_pluginState.ShowMapsMenu, _config.GameModes.Style, player);
                }
                else if (player == null)
                {
                    Console.Error.WriteLine("css_showmaps is a client only command.");
                }
            }            
        }

        // Define show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnShowModesCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(_config != null && _config.Votes.Enabled && _config.Votes.GameMode)
            {

                if(player != null && _pluginState.ShowModesMenu != null)
                {
                    // Open menu
                    _pluginState.ShowModesMenu.Title = _localizer.Localize("modes.menu-title");
                    _menuFactory.OpenMenu(_pluginState.ShowModesMenu, _config.GameModes.Style, player);
                }
                else if (player == null)
                {
                    Console.Error.WriteLine("css_showmodes is a client only command.");
                }  
            }
        }

        // Define show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnShowSettingsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(_config != null && _config.Votes.Enabled && _config.Votes.GameSetting)
            {
                if(player != null && _pluginState.ShowSettingsMenu != null)
                {
                    // Open menu
                    _pluginState.ShowSettingsMenu.Title = _localizer.Localize("settings.menu-title");
                    _menuFactory.OpenMenu(_pluginState.ShowSettingsMenu, _config.Settings.Style, player);
                }
                else if (player == null)
                {
                    Console.Error.WriteLine("css_showsettings is a client only command.");
                }
            }
        }

        // Define game menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnGameCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _pluginState.GameMenu != null && _config != null)
            {
                // Open menu
                _pluginState.GameMenu.Title = _localizer.Localize("game.menu-title");
                _menuFactory.OpenMenu(_pluginState.GameMenu, _config.Settings.Style, player);
            }
            else if (player == null)
            {
                Console.Error.WriteLine("css_game is a client only command.");
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
                Console.Error.WriteLine("css_game is a client only command.");
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
                Console.Error.WriteLine("css_game is a client only command.");
            }
        }
    }
}
