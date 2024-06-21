// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager
{
    // Define MenuFactory class
    public class MenuFactory : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private static Config? _config;
        private static Plugin? _plugin;
        private static ILogger? _logger;
        private static StringLocalizer? _localizer;

        // Load dependencies
        public void OnLoad(Plugin plugin)
        { 
            _plugin = plugin;
            _logger = plugin.Logger;
            _localizer = new StringLocalizer(plugin.Localizer);
        }
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Construct reusable method to assign menus
        public static BaseMenu AssignMenu(string _menuType, string _menuName)
        {
            // Create base menu
            BaseMenu _baseMenu;

            // Assign chat or hud menu based on config
            if (_menuType == "center")
            {
                _baseMenu = new CenterHtmlMenu(_menuName);
            }
            else
            {
                _baseMenu = new ChatMenu(_menuName);
            }

            // Return assigned menu
            return _baseMenu;
        }

        // Construct reusable method to open each type of menu
        public static void OpenMenu(BaseMenu _menu, string _menuType, CCSPlayerController _player)
        {
            if (_plugin != null)
            {
                // Check if menu type from config is hud or chat menu
                if (_menuType == "center")
                {
                    // Create tmp menu
                    CenterHtmlMenu? _hudMenu = _menu as CenterHtmlMenu;

                    // Open menu
                    if (_hudMenu != null)
                    {
                        MenuManager.OpenCenterHtmlMenu(_plugin, _player, _hudMenu);
                    }
                }
                else
                {
                    // Create tmp menu
                    ChatMenu? _chatMenu = _menu as ChatMenu;

                    // Open menu
                    if (_chatMenu != null)
                    {
                        MenuManager.OpenChatMenu(_player, _chatMenu);
                    }
                }
            }
        }

        public static void Load()
        {
            CreateModeMenus();
            CreateSettingsMenus();
        }

        // Define settings menus
        public static BaseMenu? SettingsMenu;
        public static BaseMenu? SettingsEnableMenu;
        public static BaseMenu? SettingsDisableMenu; 

        // Construct reusable method to setup settings menu
        public static void CreateSettingsMenus()
        {
            if(_logger != null && MapMenu != null && _config != null && _localizer != null)
            {
                // Assign menus
                SettingsMenu = AssignMenu(_config.Settings.Style, "Setting Actions");
                SettingsEnableMenu = AssignMenu(_config.Settings.Style, "Settings List");
                SettingsDisableMenu = AssignMenu(_config.Settings.Style, "Settings List");

                // Add enable menu options
                foreach (Setting _setting in SettingsManager.Settings)
                {
                    SettingsEnableMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
                    {
                        // Create message
                        string _message = _localizer.LocalizeWithPrefix("enable.changesetting.message", player.PlayerName, option.Text);
                        // Write to chat
                        Server.PrintToChatAll(_message);

                        // Change game setting
                        Server.ExecuteCommand($"exec {_config.Settings.Folder}/{_setting.Enable}");

                        // Close menu
                        MenuManager.CloseActiveMenu(player);
                    });
                }

                // Add disable menu options
                foreach (Setting _setting in SettingsManager.Settings)
                {
                    SettingsDisableMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
                    {
                        // Create message
                        string _message = _localizer.LocalizeWithPrefix("disable.changesetting.message", player.PlayerName, option.Text);

                        // Write to chat
                        Server.PrintToChatAll(_message);

                        // Change game setting
                        Server.ExecuteCommand($"exec {_config.Settings.Folder}/{_setting.Disable}");

                        // Close menu
                        MenuManager.CloseActiveMenu(player);
                    });
                }

                // Add settings menu options
                SettingsMenu.AddMenuOption(_localizer.Localize("menu.enable"), (player, option) =>
                {
                    SettingsEnableMenu.Title = _localizer.Localize("settings.menu-title");

                    if(player != null && _plugin != null)
                    {
                        OpenMenu(SettingsEnableMenu, _config.Settings.Style, player);
                    }
                });
                SettingsMenu.AddMenuOption(_localizer.Localize("menu.disable"), (player, option) =>
                {
                    SettingsDisableMenu.Title = _localizer.Localize("settings.menu-title");

                    if(player != null && _plugin != null)
                    {
                        // Open sub menu
                        OpenMenu(SettingsDisableMenu, _config.Settings.Style, player);
                        
                    }
                });

                // Setup show settings menu
                if(_config.Votes.GameSetting)
                {
                    CreateShowSettingsMenu();
                }
            }
        }
        
        // Define mode menu
        public static BaseMenu? ModeMenu;
        
        // Construct resuable function to set up mode menu
        public static void CreateModeMenus()
        {
            if(_logger != null && _config != null && _localizer != null)
            {
                // Assign menu
                ModeMenu = AssignMenu(_config.GameMode.Style, "Game Mode List");

                // Add menu option for each game mode in game mode list
                foreach (Mode _mode in PluginState.Modes)
                {
                    ModeMenu.AddMenuOption(_mode.Name, (player, option) =>
                    {
                        // Create message
                        string _message = _localizer.LocalizeWithPrefix("changemode.message", player.PlayerName, option.Text);

                        // Write to chat
                        Server.PrintToChatAll(_message);

                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Change game mode
                        if(_plugin != null)
                        {
                            _plugin.AddTimer(_config.GameMode.Delay, () => 
                            {
                                Server.ExecuteCommand($"exec {_mode.Config}");
                            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

                            // Set current mode
                            PluginState.CurrentMode = _mode;
                        }
                    });
                }

                // Setup show modes menu
                if(_config.Votes.GameMode)
                {
                    CreateShowModesMenu();
                }
            }
        }

        // Define map menu
        public static BaseMenu? MapMenu;

        // Construct reusable function to update the map menu
        public static void UpdateMapMenu(MapGroup _mapGroup)
        {
            if(_logger != null && _config != null && _localizer != null)
            {
                // Assign menu
                MapMenu = AssignMenu(_config.MapGroup.Style, "Map List");

                // Add menu options for each map in the new map list
                foreach (Map _map in _mapGroup.Maps)
                {
                    MapMenu.AddMenuOption(_map.DisplayName, (player, option) =>
                    {
                        Map? _nextMap = _map;

                        if (_nextMap == null)
                        {
                            _logger.LogWarning("Map not found when updating map menu. Using de_dust2 for next map."); 
                            _nextMap = new Map("de_dust2");
                        }

                        // Create message
                        string _message = _localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _nextMap.Name);

                        // Write to chat
                        Server.PrintToChatAll(_message);

                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Change map
                        if (_plugin != null)
                        {
                            _plugin.AddTimer(_config.MapGroup.Delay, () => 
                            {
                                MapManager.ChangeMap(_nextMap);
                            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                        }
                    });
                }

                // Update show maps menu with new map list
                if(_config.Votes.Map)
                {
                    UpdateShowMapsMenu(_mapGroup);
                }
            }
        }

        // Define map menu
        public static BaseMenu? GameMenu;

        // Construct reusable function to update the game command menu
        public static void UpdateGameMenu()
        {
            if(_logger != null && _config != null && _localizer != null)
            {
                // Assign menu
                GameMenu = AssignMenu(_config.Settings.Style, "Game Commands");

                // Add menu options for each map in the new map list
                foreach (string _command in PluginState.PlayerCommands)
                {
                    GameMenu.AddMenuOption(_command, (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        switch(option.Text)
                        {
                            case "!changemode":
                            if (player != null && ShowMapsMenu != null && _config.Votes.Enabled == true && _config.Votes.GameMode == true)
                            {
                                // Create message
                                string _message = _localizer.Localize("mode.show.menu-response") + _localizer.Localize("changemode");
                                player.PrintToChat(_message);
                            }
                            break;
                            case "!showmaps":
                            if (player != null && ShowMapsMenu != null && _config.Votes.Enabled == true && _config.Votes.Map == true)
                            {
                                OpenMenu(ShowMapsMenu, _config.MapGroup.Style, player);
                            }
                            break;
                            case "!showmodes":
                            if (player != null && ShowModesMenu != null && _config.Votes.Enabled == true && _config.Votes.GameMode == true)
                            {
                                OpenMenu(ShowModesMenu, _config.GameMode.Style, player);
                            }
                            break;
                            case "!showsettings":
                            if (player != null && ShowSettingsMenu != null && _config.Votes.Enabled == true && _config.Votes.GameSetting == true)
                            {
                                OpenMenu(ShowSettingsMenu, _config.Settings.Style, player);
                            }
                            break;
                            case "!currentmode":
                            if (player != null && PluginState.CurrentMapGroup != null)
                            {
                                // Create message
                                string _message = _localizer.Localize("currentmode.message", PluginState.CurrentMapGroup.DisplayName);

                                // Write to chat
                                player.PrintToChat(_message);
                            }
                            else if (player != null && PluginState.CurrentMap == null)
                            {
                                player.PrintToChat("Current map group not set.");   
                            }
                            break;
                            case "!currentmap":
                            if (player != null && PluginState.CurrentMap != null)
                            {
                                // Create message
                                string _message = _localizer.Localize("currentmap.message", PluginState.CurrentMap.Name);

                                // Write to chat
                                player.PrintToChat(_message);
                            }
                            else if (player != null && PluginState.CurrentMap == null)
                            {
                                player.PrintToChat("Current map is not set.");
                            }
                            break;
                        }
                    });
                }
            }
        }

        // Define show map menu
        public static BaseMenu? ShowMapsMenu;

        // Construct resuable function to set up show maps menu
        public static void UpdateShowMapsMenu(MapGroup _mapGroup)
        {
            if(_logger != null && _config != null && _localizer != null)
            {
                // Assign menu
                ShowMapsMenu = AssignMenu(_config.MapGroup.Style, "Map List");

                foreach (Map _map in _mapGroup.Maps)
                {
                    // Add menu option
                    ShowMapsMenu.AddMenuOption(_map.DisplayName, (player, option) =>
                    {
                        // Create message
                        string _message = _localizer.Localize("maps.show.menu-response", _map.Name);

                        // Write to chat
                        player.PrintToChat(_message);

                        // Close menu
                        MenuManager.CloseActiveMenu(player);
                    });
                }
            }
        }

        // Define show map menu
        public static BaseMenu? ShowModesMenu;

        // Construct resuable function to set up show maps menu
        public static void CreateShowModesMenu()
        {
            if(_logger != null && _config != null && _localizer != null)
            {
                // Assign menu
                ShowModesMenu = AssignMenu(_config.GameMode.Style, "Game Mode List");

                foreach (Mode _mode in PluginState.Modes)
                {
                    // Add menu option
                    ShowModesMenu.AddMenuOption(_mode.Name, (player, option) =>
                    {
                        // Create message
                        string _modeName = Extensions.RemoveCfgExtension(_mode.Config);
                        string _message = _localizer.Localize("mode.show.menu-response", _modeName);

                        // Write to chat
                        player.PrintToChat(_message);

                        // Close menu
                        MenuManager.CloseActiveMenu(player);
                    });
                }
            }
        }

        // Define show map menu
        public static BaseMenu? ShowSettingsMenu;

        // Construct resuable function to set up show maps menu
        public static void CreateShowSettingsMenu()
        {
            if(_logger != null && _config != null && _localizer != null)
            {
                // Assign menu
                ShowSettingsMenu = AssignMenu(_config.Settings.Style, "Setting List");
                
                foreach (Setting _setting in SettingsManager.Settings)
                {
                    // Add menu option
                    ShowSettingsMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
                    {
                        // Create message
                        string _message = _localizer.Localize("setting.show.menu-response", _setting.Name);

                        // Write to chat
                        player.PrintToChat(_message);

                        // Close menu
                        MenuManager.CloseActiveMenu(player);
                    });
                }
            }
        }
    }
}