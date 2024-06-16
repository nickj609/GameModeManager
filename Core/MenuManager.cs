// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin
    {
        // Construct reusable function to assign menus
        private BaseMenu AssignMenu(string _menuType, string _menuName)
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

        // Construct reusable function to open each type of menu
        private void OpenMenu(BaseMenu _menu, string _menuType, CCSPlayerController _player)
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

        // Define settings menus
        public static BaseMenu? SettingsMenu;
        public static BaseMenu? SettingsEnableMenu;
        public static BaseMenu? SettingsDisableMenu; 

        // Construct reusable function to setup settings menu
        public void SetupSettingsMenu()
        {
            // Assign menus
            SettingsMenu = AssignMenu(Config.Settings.Style, "Setting Actions");
            SettingsEnableMenu = AssignMenu(Config.Settings.Style, "Settings List");
            SettingsDisableMenu = AssignMenu(Config.Settings.Style, "Settings List");

            // Add enable menu options
            foreach (Setting _setting in Settings)
            {
                SettingsEnableMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
                {
                    // Create message
                    string _message = Localizer["plugin.prefix"] + " " + Localizer["enable.changesetting.message", player.PlayerName, option.Text];
                    // Write to chat
                    Server.PrintToChatAll(_message);

                    // Change game setting
                    Server.ExecuteCommand($"exec {Config.Settings.Folder}/{_setting.Enable}");

                    // Close menu
                    MenuManager.CloseActiveMenu(player);
                });
            }

            // Add disable menu options
            foreach (Setting _setting in Settings)
            {
                SettingsDisableMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
                {
                    // Create message
                    string _message = Localizer["plugin.prefix"] + " " + Localizer["disable.changesetting.message", player.PlayerName, option.Text];

                    // Write to chat
                    Server.PrintToChatAll(_message);

                    // Change game setting
                    Server.ExecuteCommand($"exec {Config.Settings.Folder}/{_setting.Disable}");

                    // Close menu
                    MenuManager.CloseActiveMenu(player);
                });
            }

            // Add settings menu options
            SettingsMenu.AddMenuOption(Localizer["menu.enable"], (player, option) =>
            {
                SettingsEnableMenu.Title = Localizer["settings.menu-title"];

                if(player != null && _plugin != null)
                {
                    OpenMenu(SettingsEnableMenu, Config.Settings.Style, player);
                }
            });
            SettingsMenu.AddMenuOption(Localizer["menu.disable"], (player, option) =>
            {
                SettingsDisableMenu.Title = Localizer["settings.menu-title"];

                if(player != null && _plugin != null)
                {
                    // Open sub menu
                    OpenMenu(SettingsDisableMenu, Config.Settings.Style, player);
                    
                }
            });

            // Setup show settings menu
            if(Config.Votes.GameSetting)
            {
                SetupShowSettingsMenu();
            }
        }
        
        // Define mode menu
        public static BaseMenu? ModeMenu;
        
        // Construct resuable function to set up mode menu
        private void SetupModeMenu()
        {
            // Assign menu
            ModeMenu = AssignMenu(Config.GameMode.Style, "Game Mode List");

            if (Config.GameMode.ListEnabled)
            {
                // Add menu option for each game mode in game mode list
                foreach (KeyValuePair<string, string> _entry in Config.GameMode.List)
                {
                    ModeMenu.AddMenuOption(_entry.Value, (player, option) =>
                    {
                        // Create message
                        string _message = Localizer["plugin.prefix"] + " " + Localizer["changemode.message", player.PlayerName, option.Text];

                        // Write to chat
                        Server.PrintToChatAll(_message);

                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Change game mode
                        string _option = _entry.Key.ToLower();
                        AddTimer(Config.GameMode.Delay, () => 
                        {
                            Server.ExecuteCommand($"exec {_option}.cfg");
                        }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                    });
                }
            }
            else
            {
                // Create menu options for each map group parsed
                foreach (MapGroup _mapGroup in MapGroups)
                {

                    if(_mapGroup.DisplayName != null)
                    {
                        ModeMenu.AddMenuOption(_mapGroup.DisplayName, (player, option) =>
                        {
                             // Create message
                            string _message = Localizer["plugin.prefix"] + " " + Localizer["changemode.message", player.PlayerName, option.Text];

                            // Write to chat
                            Server.PrintToChatAll(_message);

                            // Close menu
                            MenuManager.CloseActiveMenu(player);

                            // Change game mode
                            string _option = option.Text.ToLower();
                            AddTimer(Config.GameMode.Delay, () => 
                            {
                                Server.ExecuteCommand($"exec {_option}.cfg");
                            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                        });
                    }
                    else
                    {
                        // Split the string into parts by the underscore
                        string[] _nameParts = (_mapGroup.Name ?? _defaultMapGroup.Name).Split('_');

                        // Get the last part (the actual map group name)
                        string _tempName = _nameParts[_nameParts.Length - 1]; 
                        
                        // Combine the capitalized first letter with the rest
                        string _mapGroupName = _tempName.Substring(0, 1).ToUpper() + _tempName.Substring(1); 

                        ModeMenu.AddMenuOption(_mapGroupName, (player, option) =>
                        {
                             // Create message
                            string _message = Localizer["plugin.prefix"] + "" + Localizer["changemode.message", player.PlayerName, option.Text];

                            // Write to chat
                            Server.PrintToChatAll(_message);

                            // Close menu
                            MenuManager.CloseActiveMenu(player);

                            // Change game mode
                            string _option = option.Text.ToLower();
                            AddTimer(Config.GameMode.Delay, () => 
                            {
                                Server.ExecuteCommand($"exec {_option}.cfg");
                            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                        });

                    }
                }
            }

            // Setup show modes menu
            if(Config.Votes.GameMode)
            {
                SetupShowModesMenu();
            }
        }

        // Define map menu
        public static BaseMenu? MapMenu;

        // Construct reusable function to update the map menu
        private void UpdateMapMenu(MapGroup _mapGroup)
        {
            // Assign menu
            MapMenu = AssignMenu(Config.MapGroup.Style, "Map List");

            // Add menu options for each map in the new map list
            foreach (Map _map in _mapGroup.Maps)
            {
                MapMenu.AddMenuOption(_map.DisplayName, (player, option) =>
                {
                    Map? _nextMap = _map;

                    if (_nextMap == null)
                    {
                        Logger.LogWarning("Map not found when updating map menu. Using de_dust2 for next map."); 
                        _nextMap = new Map("de_dust2");
                    }

                    // Create message
                    string _message = Localizer["plugin.prefix"] + " " + Localizer["changemap.message", player.PlayerName, _nextMap.Name];

                    // Write to chat
                    Server.PrintToChatAll(_message);

;                   // Close menu
                    MenuManager.CloseActiveMenu(player);

                    // Change map
                    AddTimer(Config.MapGroup.Delay, () => 
                    {
                        ChangeMap(_nextMap);
                    }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                });
            }

            // Update show maps menu with new map list
            if(Config.Votes.Map)
            {
                UpdateShowMapsMenu(_mapGroup);
            }
        }

        // Define map menu
        public static BaseMenu? GameMenu;

        // Construct reusable function to update the game command menu
        private void UpdateGameMenu()
        {
            // Assign menu
            GameMenu = AssignMenu(Config.Settings.Style, "Game Commands");

            // Add menu options for each map in the new map list
            foreach (string _command in Commands)
            {
                GameMenu.AddMenuOption(_command, (player, option) =>
                {
                    // Close menu
                    MenuManager.CloseActiveMenu(player);

                    switch(option.Text)
                    {
                        case "!changemode":
                        if (player != null && ShowMapsMenu != null && Config.Votes.Enabled == true && Config.Votes.GameMode == true)
                        {
                            // Create message
                            string _message = Localizer["mode.show.menu-response", "changemode"];
                            player.PrintToChat(_message);
                        }
                        break;
                        case "!showmaps":
                        if (player != null && ShowMapsMenu != null && Config.Votes.Enabled == true && Config.Votes.Map == true)
                        {
                            OpenMenu(ShowMapsMenu, Config.MapGroup.Style, player);
                        }
                        break;
                        case "!showmodes":
                        if (player != null && ShowModesMenu != null && Config.Votes.Enabled == true && Config.Votes.GameMode == true)
                        {
                            OpenMenu(ShowModesMenu, Config.GameMode.Style, player);
                        }
                        break;
                        case "!showsettings":
                        if (player != null && ShowSettingsMenu != null && Config.Votes.Enabled == true && Config.Votes.GameSetting == true)
                        {
                            OpenMenu(ShowSettingsMenu, Config.Settings.Style, player);
                        }
                        break;
                        case "!currentmode":
                        if (player != null && CurrentMapGroup != null)
                        {
                            // Create message
                            string _message = Localizer["currentmode.message", CurrentMapGroup.DisplayName];

                            // Write to chat
                            player.PrintToChat(_message);
                        }
                        else if (player != null && CurrentMap == null)
                        {
                            player.PrintToChat("Current map group not set.");   
                        }
                        break;
                        case "!currentmap":
                        if (player != null && CurrentMap != null)
                        {
                            // Create message
                            string _message = Localizer["currentmap.message", CurrentMap.Name];

                            // Write to chat
                            player.PrintToChat(_message);
                        }
                        else if (player != null && CurrentMap == null)
                        {
                            player.PrintToChat("Current map is not set.");
                        }
                        break;
                    }
                });
            }
        }

        // Define show map menu
        public static BaseMenu? ShowMapsMenu;

        // Construct resuable function to set up show maps menu
        private void UpdateShowMapsMenu(MapGroup _mapGroup)
        {
            // Assign menu
            ShowMapsMenu = AssignMenu(Config.MapGroup.Style, "Map List");

            foreach (Map _map in _mapGroup.Maps)
            {
                // Add menu option
                ShowMapsMenu.AddMenuOption(_map.DisplayName, (player, option) =>
                {
                    // Create message
                    string _message = Localizer["maps.show.menu-response", _map.Name];

                    // Write to chat
                    player.PrintToChat(_message);

                    // Close menu
                    MenuManager.CloseActiveMenu(player);
                });
            }
        }

        // Define show map menu
        public static BaseMenu? ShowModesMenu;

        // Construct resuable function to set up show maps menu
        private void SetupShowModesMenu()
        {
            // Assign menu
            ShowModesMenu = AssignMenu(Config.GameMode.Style, "Game Mode List");

            if (Config.GameMode.ListEnabled)
            {
                foreach (KeyValuePair<string, string> _entry in Config.GameMode.List)
                {
                    // Add menu option
                    ShowModesMenu.AddMenuOption(_entry.Value, (player, option) =>
                    {
                        // Create message
                        string _message = Localizer["mode.show.menu-response", _entry.Key];

                        // Write to chat
                        player.PrintToChat(_message);

                        // Close menu
                        MenuManager.CloseActiveMenu(player);
                    });
                }
            }
            else
            {
                foreach (MapGroup _mapGroup in MapGroups)
                {
                    if(_mapGroup.Name != null)
                    {
                        // Remove mode prefix
                        if (new Regex(@"^(mg_)").Match(_mapGroup.Name).Success) 
                        {
                            // Create new mode name
                            _mapGroup.Name = _mapGroup.Name.Substring(new Regex(@"^(mg_)").Match(_mapGroup.Name).Length);
                        }

                        // Add menu option
                        _ = ShowModesMenu.AddMenuOption(_mapGroup.DisplayName, (player, option) =>
                        {
                            // Write to player
                            player.PrintToChat((string)Localizer["mode.show.menu-response", _mapGroup.Name]);

                            // Close menu
                            MenuManager.CloseActiveMenu(player);
                        });
                    }
                    else
                    {
                        // Split the string into parts by the underscore
                        var _nameParts = (_mapGroup.Name ?? _defaultMapGroup.Name).Split('_');

                        // Get the last part (the actual map group name)
                        string _tempName = _nameParts[_nameParts.Length - 1]; 
                        
                        // Combine the capitalized first letter with the rest
                        string _mapGroupName = _tempName.Substring(0, 1).ToUpper() + _tempName.Substring(1); 

                        ShowModesMenu.AddMenuOption(_mapGroupName, (player, option) =>
                        {
                             // Create message
                            string _message = Localizer["plugin.prefix"] + " " + Localizer["changemode.message", player.PlayerName, option.Text];

                            // Write to chat
                            Server.PrintToChatAll(_message);

                            // Close menu
                            MenuManager.CloseActiveMenu(player);

                            // Change game mode
                            string _option = option.Text.ToLower();
                            AddTimer(Config.GameMode.Delay, () => 
                            {
                                Server.ExecuteCommand($"exec {_option}.cfg");
                            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                        });

                    }
                }
            }
        }

        // Define show map menu
        public static BaseMenu? ShowSettingsMenu;

        // Construct resuable function to set up show maps menu
        private void SetupShowSettingsMenu()
        {
            // Assign menu
            ShowSettingsMenu = AssignMenu(Config.Settings.Style, "Setting List");
            
            foreach (Setting _setting in Settings)
            {
                // Add menu option
                ShowSettingsMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
                {
                    // Create message
                    string _message = Localizer["setting.show.menu-response", _setting.Name];

                    // Write to chat
                    player.PrintToChat(_message);

                    // Close menu
                    MenuManager.CloseActiveMenu(player);
                });
            }
            
        }
    }
}