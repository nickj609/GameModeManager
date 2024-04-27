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
        // Define settings list and folder
        public static List<string> Settings = new List<string>();

        // Create settings list
        private void ParseSettings()
        {
            // Check if the directory exists
            if (Directory.Exists(Config.Settings.Folder))
            {
                // Get all .cfg files
                string[] _cfgFiles = Directory.GetFiles($"{Config.Settings.Home}/{Config.Settings.Folder}/", "*.cfg");

                // Process each file
                foreach (string _file in _cfgFiles)
                {
                    string _fileName = Path.GetFileNameWithoutExtension(_file);  // Extract file name without .cfg
                    string _capitalizedName = char.ToUpper(_fileName[0]) + _fileName.Substring(1); // Capitalize the first letter
                    Settings.Add(_capitalizedName);
                }
            }
            else
            {
                Console.WriteLine("Settings folder not found.");
            }
        }
            
        // Create settings menu
        private static CenterHtmlMenu _settingsMenu = new CenterHtmlMenu("Settings List");

        // Setup settings menu
        private void SetupSettingsMenu()
        {
            // Define settings menu
            _settingsMenu = new CenterHtmlMenu("Settings List");

            // Add menu option for each game setting
            foreach (string _setting in Settings)
            {
                _settingsMenu.AddMenuOption(_setting, (player, option) =>
                {
                    // Write to chat
                    Server.PrintToChatAll(Localizer["changesetting.message", player.PlayerName, option.Text]);

                    // Change game setting
                    string _option = option.Text.ToLower();
                    Server.ExecuteCommand($"exec {_option}.cfg");

                    // Close menu
                    MenuManager.CloseActiveMenu(player);
                });
            }
        }

        // Construct change setting command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(minArgs: 1, usage: "[setting]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_setting", "Changes the game setting specified.")]
        public void OnSettingCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null)
            {
                // Write to chat
                Server.PrintToChatAll(Localizer["changesetting.message", player.PlayerName, command.ArgByIndex(1)]);

                // Change game setting
                string _option = $"{command.ArgByIndex(1)}".ToLower();
                Server.ExecuteCommand($"exec /settings/{_option}.cfg");
            }
        }

        // Construct admin setting menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_settings", "Provides a list of game settings.")]
        public void OnSettingsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null)
            {
                _modeMenu.Title = Localizer["settings.hud.menu-title"];
                MenuManager.OpenCenterHtmlMenu(_plugin, player, _settingsMenu);
            }
        }
    }
}
