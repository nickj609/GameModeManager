// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Core.Attributes.Registration;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin
    {
        // Create mode menu
        private static CenterHtmlMenu _modeMenu = new CenterHtmlMenu("Game Mode List");
        
        // Setup mode menu
        private void SetupModeMenu()
        {
            _modeMenu = new CenterHtmlMenu("Game Mode List");

            if (Config.GameMode.ListEnabled)
            {
                // Add menu option for each game mode in game mode list
                foreach (string _mode in Config.GameMode.List)
                {
                    _modeMenu.AddMenuOption(_mode, (player, option) =>
                    {
                        // Write to chat
                        Server.PrintToChatAll(Localizer["changemode.message", player.PlayerName, option.Text]);

                        // Change game mode
                        string _option = option.Text.ToLower();
                        AddTimer(Config.GameMode.Delay, () => Server.ExecuteCommand($"exec {_option}.cfg"));

                        // Close menu
                        MenuManager.CloseActiveMenu(player);
                    });
                }
            }
            else
            {
                // Create menu options for each map group parsed
                foreach (MapGroup _mapGroup in MapGroups)
                {
                    // Split the string into parts by the underscore
                    string[] _nameParts = (_mapGroup.Name ?? _defaultMapGroup.Name).Split('_');

                    // Get the last part (the actual map group name)
                    string _tempName = _nameParts[_nameParts.Length - 1]; 

                    // Combine the capitalized first letter with the rest
                    string _mapGroupName = _tempName.Substring(0, 1).ToUpper() + _tempName.Substring(1); 

                    if(_mapGroupName != null)
                    {
                        _modeMenu.AddMenuOption(_mapGroupName, (player, option) =>
                        {
                            // Write to chat
                            Server.PrintToChatAll(Localizer["changemode.message", player.PlayerName, option.Text]);

                            // Change game mode
                            string _option = option.Text.ToLower();
                            AddTimer(Config.GameMode.Delay, () => Server.ExecuteCommand($"exec {_option}.cfg"));

                            // Close menu
                            MenuManager.CloseActiveMenu(player);
                        });
                    }
                }
            }
        }

        // Construct change mode command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "[mode]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_mode", "Changes the game mode to the mode specified in the command argument.")]
        public void OnModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null)
            {
                // Write to chat
                Server.PrintToChatAll(Localizer["changemode.message", player.PlayerName, command.ArgByIndex(1)]);

                // Change game mode
                string _option = $"{command.ArgByIndex(1)}".ToLower();
                AddTimer(5.0f, () => Server.ExecuteCommand($"exec {_option}.cfg"));
            }
        }

        // Construct admin mode menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_modes", "Provides a list of game modes.")]
        public void OnModesCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null)
            {
                _modeMenu.Title = Localizer["mode.hud.menu-title"];
                MenuManager.OpenCenterHtmlMenu(_plugin, player, _modeMenu);
            }
        }
    }
}