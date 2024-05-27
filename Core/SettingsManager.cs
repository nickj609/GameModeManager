// Included libraries
using System.Globalization; 
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Core.Attributes.Registration;

// Declare namespace
namespace GameModeManager
{
    public class Setting : IEquatable<Setting>
    {
        public string Name { get; set; }
        public string ConfigEnable { get; set; }
        public string ConfigDisable { get; set; }
        
        public Setting(string name)
        {
            Name = name;
            ConfigEnable = "";
            ConfigDisable = "";
        }
        public Setting(string name, string configEnable, string configDisable)
        {
            Name = name;
            ConfigEnable = configEnable;
            ConfigDisable = configDisable;
        }

        public bool Equals(Setting? other) 
        {
            if (other == null) return false;  // Handle null 
            return Name == other.Name && ConfigEnable == other.ConfigEnable && ConfigDisable == other.ConfigDisable;
        }

        public void Clear()
        {
            Name = "";
            ConfigEnable = "";
            ConfigDisable = "";
        }
    }

    public class SettingsMenuFactory
    {
        public BaseMenu CreateMenu(string style)
        {
            if (style == "center") {
                return new CenterHtmlMenu("Settings List");
            } else {
                return new ChatMenu("Settings List");
            }
        }
    }
    public partial class Plugin : BasePlugin
    {
        // Define settings list
        public static List<Setting> Settings = new List<Setting>();
        private string FormatSettingName(string settingName)
        {

            var _name = Path.GetFileNameWithoutExtension(settingName);
            var _regex = new Regex(@"^(enable_|disable_)(.*)");
            var _match = _regex.Match(_name);

            if (_match.Success) 
            {
                _name = _match.Groups[2].Value;
                _name = _name.Replace("_", " ");
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_name); 
            } 
            else
            {
                return null!; 
            }
        }

        // Create settings list
        private void ParseSettings()
        {
            SettingsDirectory =  $"{ConfigDirectory}{Config.Settings.Folder}";

            // Check if the directory exists
            if (Directory.Exists(SettingsDirectory))
            {
                // Get all .cfg files
                string[] _cfgFiles = Directory.GetFiles(SettingsDirectory, "*.cfg");

                if (_cfgFiles.Length != 0)
                {
                    // Process each file
                    foreach (string _file in _cfgFiles)
                    {
                        string _fileName = FormatSettingName(_file);

                        if (_fileName != null)
                        {
                            // Find existing setting if it's already in the list
                            var setting = Settings.FirstOrDefault(s => s.Name == _fileName);

                            if (setting == null)
                            {
                                // Create a new setting if not found
                                setting = new Setting(_fileName);
                                Settings.Add(setting);
                            }

                            // Assign config path based on prefix
                            if (_file.StartsWith("enable_")) 
                            {
                                setting.ConfigEnable = _file;
                            } 
                            else 
                            {
                                setting.ConfigDisable = _file;
                            }
                        }
                        else
                        {
                            Logger.LogWarning($"Skipping {_file} because its missing the correct prefix.");
                        }
                    }
                }
                else
                {
                    Logger.LogError("Setting config files not found.");
                }
            }
            else
            {
                Logger.LogError("Settings folder not found.");
            }
        }
            
        // Setup settings menu
        public void SetupSettingsMenu()
        {
            // Create menus
            var menuFactory = new SettingsMenuFactory();
            var _settingsMenu = menuFactory.CreateMenu(Config.Settings.Style); 
            var _settingsEnableMenu = menuFactory.CreateMenu(Config.Settings.Style);
            var _settingsDisableMenu = menuFactory.CreateMenu(Config.Settings.Style);

            // Create enable menu options
            foreach (Setting _setting in Settings)
            {
                _settingsEnableMenu.AddMenuOption(_setting.Name, (player, option) =>
                {
                    // Write to chat
                    Server.PrintToChatAll(Localizer["enable.changesetting.message", player.PlayerName, option.Text]);

                    // Change game setting
                    Server.ExecuteCommand($"exec {SettingsDirectory}{_setting.ConfigEnable}");

                    // Close menu
                    MenuManager.CloseActiveMenu(player);
                });
            }

            // Create disable menu options
            foreach (Setting _setting in Settings)
            {
                _settingsDisableMenu.AddMenuOption(_setting.Name, (player, option) =>
                {
                    // Write to chat
                    Server.PrintToChatAll(Localizer["disable.changesetting.message", player.PlayerName, option.Text]);

                    // Change game setting
                    Server.ExecuteCommand($"exec {SettingsDirectory}{_setting.ConfigDisable}");

                    // Close menu
                    MenuManager.CloseActiveMenu(player);
                });
            }

            // Create main menu options
            _settingsMenu.AddMenuOption("Enable settings", (player, option) =>
            {
                _settingsEnableMenu.Title = Localizer["settings.enable.hud.menu-title"];

                if(player != null && _plugin != null)
                {
                    if(_settingsEnableMenu is CenterHtmlMenu)
                    {
                        MenuManager.OpenCenterHtmlMenu(_plugin, player, _settingsEnableMenu);
                    }
                    else if (_settingsEnableMenu is ChatMenu)
                    {
                        MenuManager.OpenChatMenu(player, _settingsEnableMenu);
                    }
                }
            });
            _settingsMenu.AddMenuOption("Disable settings", (player, option) =>
            {
                _settingsDisableMenu.Title = Localizer["settings.disable.hud.menu-title"];

                if(player != null && _plugin != null)
                {
                    // Open sub menu
                    if(_settingsDisableMenu is CenterHtmlMenu)
                    {
                        MenuManager.OpenCenterHtmlMenu(_plugin, player, _settingsDisableMenu);
                    }
                    else if (_settingsEnableMenu is ChatMenu)
                    {
                        MenuManager.OpenChatMenu(player, _settingsDisableMenu);
                    }
                }
            });
        }
        // Construct change setting command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(minArgs: 2, usage: "[enable/disable] [setting name]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_setting", "Changes the game setting specified.")]
        public void OnSettingCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null)
            {
                // Get args
                string _status = $"{command.ArgByIndex(1).ToLower()}";
                string _settingName = $"{command.ArgByIndex(2)}";

                // Find game setting
                Setting? _option = Settings.FirstOrDefault(s => s.Name == _settingName);

                if(_option != null) 
                {
                    if (_status == "enable")
                    {
                        // Write to chat
                        Server.PrintToChatAll(Localizer["enable.changesetting.message", player.PlayerName, command.ArgByIndex(2)]);

                        // Change game setting
                        Server.ExecuteCommand($"exec settings/{_option.ConfigDisable}");
                    }
                    else if (_status == "disable")
                    {
                        // Write to chat
                        Server.PrintToChatAll(Localizer["disable.changesetting.message", player.PlayerName, command.ArgByIndex(2)]);

                        // Change game setting
                        Server.ExecuteCommand($"exec settings/{_option.ConfigDisable}");
                    }
                    else
                    {
                        
                        command.ReplyToCommand($"Unexpected argument: {command.ArgByIndex(1)}");
                    }  
                }
                else
                {
                    command.ReplyToCommand($"Can't find setting: {command.ArgByIndex(2)}");
                }
            }
        }
        // Construct admin setting menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_settings", "Provides a list of game settings.")]
        public void OnSettingsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null && _settingsMenu != null)
            {
                // Open menu
                _settingsMenu.Title = Localizer["settings.hud.menu-title"];

                if(_settingsMenu is CenterHtmlMenu)
                {
                    MenuManager.OpenCenterHtmlMenu(_plugin, player, _settingsMenu);
                }
                else if (_settingsMenu is ChatMenu)
                {
                    MenuManager.OpenChatMenu(player, _settingsMenu);
                }
            }
        }
    }
}
