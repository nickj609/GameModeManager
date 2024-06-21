// Included libraries
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin
    {
        // Construct show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_showmaps", "Provides a list of maps from current mode.")]
        public void OnShowMapsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(Config.Votes.Enabled == true && Config.Votes.Map == true)
            {
                if(player != null && MenuFactory.ShowMapsMenu != null)
                {
                    // Open menu
                    MenuFactory.ShowMapsMenu.Title = Localizer["maps.menu-title"];
                    MenuFactory.OpenMenu(MenuFactory.ShowMapsMenu, Config.GameMode.Style, player);
                }
                else if (player == null)
                {
                    Console.Error.WriteLine("css_showmaps is a client only command.");
                }
            }
            
        }

        // Construct show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_showmodes", "Provides a list of game modes.")]
        public void OnShowModesCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(Config.Votes.Enabled == true && Config.Votes.GameMode == true)
            {

                if(player != null && MenuFactory.ShowModesMenu != null)
                {
                    // Open menu
                    MenuFactory.ShowModesMenu.Title = Localizer["modes.menu-title"];
                    MenuFactory.OpenMenu(MenuFactory.ShowModesMenu, Config.GameMode.Style, player);
                }
                else if (player == null)
                {
                    Console.Error.WriteLine("css_showmodes is a client only command.");
                }  
            }
        }

        // Construct show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_showsettings", "Provides a list of game settings.")]
        public void OnShowSettingsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(Config.Votes.Enabled == true && Config.Votes.GameSetting == true)
            {
                if(player != null && MenuFactory.ShowSettingsMenu != null)
                {
                    // Open menu
                    MenuFactory.ShowSettingsMenu.Title = Localizer["settings.menu-title"];
                    MenuFactory.OpenMenu(MenuFactory.ShowSettingsMenu, Config.Settings.Style, player);
                }
                else if (player == null)
                {
                    Console.Error.WriteLine("css_showsettings is a client only command.");
                }
            }
            
        }

        // Construct game menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_game", "Provides a list of game commands.")]
        public void OnGameCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && MenuFactory.GameMenu != null)
            {
                // Open menu
                MenuFactory.GameMenu.Title = Localizer["game.menu-title"];
                MenuFactory.OpenMenu(MenuFactory.GameMenu, Config.Settings.Style, player);
            }
            else if (player == null)
            {
                Console.Error.WriteLine("css_game is a client only command.");
            }
        }

        // Construct current map command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_currentmap", "Displays current map.")]
        public void OnCurrentMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null && PluginState.CurrentMap != null)
            {
                // Create message
                string _message = Localizer["currentmap.message", PluginState.CurrentMap.DisplayName];

                // Write to chat
                player.PrintToChat(_message);
            }
            else if (player != null && PluginState.CurrentMap == null)
            {
                player.PrintToChat("Current map is not set.");
            }
            else if (player == null)
            {
                Console.Error.WriteLine("css_game is a client only command.");
            }
        }

        // Construct current map command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_currentmapgroup", "Displays current map group.")]
        public void OnCurrentMapGroupCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null && PluginState.CurrentMapGroup != null)
            {
                // Create message
                string _message = Localizer["currentmode.message", PluginState.CurrentMapGroup.DisplayName];

                // Write to chat
                player.PrintToChat(_message);
            }
            else if (player != null && PluginState.CurrentMapGroup == null)
            {
                player.PrintToChat("Current map group not set.");   
            }
            else if (player == null)
            {
                Console.Error.WriteLine("css_game is a client only command.");
            }
        }

        // Construct current map command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_currentmode", "Displays current map.")]
        public void OnCurrentModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null && PluginState.CurrentMode != null)
            {
                // Create message
                string _message = Localizer["currentmode.message", PluginState.CurrentMode.Name];

                // Write to chat
                player.PrintToChat(_message);
            }
            else if (player != null && PluginState.CurrentMode == null)
            {
                player.PrintToChat("Current mode not set.");   
            }
            else if (player == null)
            {
                Console.Error.WriteLine("css_game is a client only command.");
            }
        }
    }
}
