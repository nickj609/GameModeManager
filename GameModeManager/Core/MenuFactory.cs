// Included libraries
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class MenuFactory : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin? _plugin;
        private GameRules _gameRules;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;
        private IStringLocalizer _iLocalizer;
        private Config _config = new Config();
        private TimeLimitManager _timeLimitManager;
        private MaxRoundsManager _maxRoundsManager;

        // Define class instance
        public MenuFactory(PluginState pluginState, StringLocalizer stringLocalizer, TimeLimitManager timeLimitManager, 
        MaxRoundsManager maxRoundsManager, GameRules gameRules, IStringLocalizer iLocalizer, ServerManager serverManager)
        {
            _gameRules = gameRules;
            _iLocalizer = iLocalizer;
            _pluginState = pluginState;
            _localizer = stringLocalizer;
            _serverManager = serverManager;
            _timeLimitManager = timeLimitManager;
            _maxRoundsManager = maxRoundsManager;
        }
        
        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
        }

        // Define reusable method to assign menus
        public BaseMenu AssignMenu(string menuType, string menuName)
        {
            // Create base menu
            BaseMenu _baseMenu;

            // Assign chat or hud menu based on config
            if (menuType.Equals("center", StringComparison.OrdinalIgnoreCase) && _plugin != null)
            {
                _baseMenu = new CenterHtmlMenu(menuName, _plugin);
            }
            else
            {
                _baseMenu = new ChatMenu(menuName);
            }

            // Return assigned menu
            return _baseMenu;
        }

        // Define reusable method to open each type of menu
        public void OpenMenu(BaseMenu menu, string menuType, CCSPlayerController _player)
        {
            // Check if menu type from config is hud or chat menu
            if (menuType.Equals("center", StringComparison.OrdinalIgnoreCase))
            {
                // Create tmp menu
                CenterHtmlMenu? _hudMenu = menu as CenterHtmlMenu;

                // Open menu
                if (_hudMenu != null && _plugin != null)
                {
                    MenuManager.OpenCenterHtmlMenu(_plugin, _player, _hudMenu);
                }
            }
            else
            {
                // Create tmp menu
                ChatMenu? _chatMenu = menu as ChatMenu;

                // Open menu
                if (_chatMenu != null)
                {
                    MenuManager.OpenChatMenu(_player, _chatMenu);
                }
            }
        }

        // Define reusable method to setup settings menu
        public void CreateSettingsMenus()
        {
            // Assign menus
            _pluginState.SettingsMenu = AssignMenu(_config.Settings.Style, "Setting Actions");
            _pluginState.SettingsEnableMenu = AssignMenu(_config.Settings.Style, "Settings List");
            _pluginState.SettingsDisableMenu = AssignMenu(_config.Settings.Style, "Settings List");

            // Add enable menu options
            foreach (Setting _setting in _pluginState.Settings)
            {
                _pluginState.SettingsEnableMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
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
            foreach (Setting _setting in _pluginState.Settings)
            {
                _pluginState.SettingsDisableMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
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

            // create enable settings sub menu option
            _pluginState.SettingsMenu.AddMenuOption(_localizer.Localize("menu.enable"), (player, option) =>
            {
                _pluginState.SettingsEnableMenu.Title = _localizer.Localize("settings.menu-title");

                if(player != null)
                {
                    // Open sub menu
                    OpenMenu(_pluginState.SettingsEnableMenu, _config.Settings.Style, player);
                }
            });

            // Create disable settings menu sub menu option
            _pluginState.SettingsMenu.AddMenuOption(_localizer.Localize("menu.disable"), (player, option) =>
            {
                _pluginState.SettingsDisableMenu.Title = _localizer.Localize("settings.menu-title");

                if(player != null)
                {
                    // Open sub menu
                    OpenMenu(_pluginState.SettingsDisableMenu, _config.Settings.Style, player);
                    
                }
            });
            
            // Create user settings menu
            if(_config.Votes.GameSettings)
            {
                CreateShowSettingsMenu();
            }
        }
        
        // Define resuable method to set up mode menu
        public void CreateModeMenus()
        {
            // Assign menu
            _pluginState.ModeMenu = AssignMenu(_config.GameModes.Style, "Game Mode List");

            // Add menu option for each game mode in game mode list
            foreach (Mode _mode in _pluginState.Modes)
            {
                _pluginState.ModeMenu.AddMenuOption(_mode.Name, (player, option) =>
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
                        _serverManager.ChangeMode(_mode);
                    }
                });
            }
            // Setup user mode menu
            if(_config.Votes.GameModes)
            {
                CreateShowModesMenu();
            }
        }

        // Define resuable method to create map menus
        public void CreateMapMenus()
        {
            CreateAllMapsMenus();
            UpdateMapMenus();
        }

        // Define resuable method to create all maps menu
        public void CreateAllMapsMenus()
        {
            // Create admin all map(s) menu
            _pluginState.MapsMenu = AssignMenu(_config.Maps.Style, "Select a game mode.");

            foreach (Mode _mode in _pluginState.Modes)
            {
                _pluginState.MapsMenu.AddMenuOption(_mode.Name, (player, option) =>
                {
                    // Create sub menu
                    BaseMenu subMenu;
                    subMenu = AssignMenu(_config.Maps.Style, _localizer.Localize("maps.menu-title"));

                    foreach (Map _map in _mode.Maps)
                    {
                        subMenu.AddMenuOption(_map.DisplayName, (player, option) =>
                        {
                            Map _nextMap = _map;

                            // Create message
                            string _message = _localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _nextMap.Name);

                            // Write to chat
                            Server.PrintToChatAll(_message);

                            // Close menu
                            MenuManager.CloseActiveMenu(player);

                            // Change map
                            _serverManager.ChangeMap(_nextMap);
                        });
                    } 
                    // Open menu
                    OpenMenu(subMenu, _config.Maps.Style, player);
                });
            }
            // Create user all map(s) menu
            if(_config.Votes.Maps)
            {
                CreateShowAllMapsMenus();
            }
        }

        // Define resuable method to show all maps menu
        public void CreateShowAllMapsMenus()
        {
            _pluginState.ShowMapsMenu = AssignMenu(_config.Maps.Style, "Select a game mode.");

            foreach (Mode _mode in _pluginState.Modes)
            {
                _pluginState.ShowMapsMenu.AddMenuOption(_mode.Name, (player, option) =>
                {
                    // Close menu
                    MenuManager.CloseActiveMenu(player);

                    // Create sub menu
                    BaseMenu subMenu;
                    subMenu = AssignMenu(_config.Maps.Style, _localizer.Localize("maps.menu-title"));

                    foreach (Map _map in _mode.Maps)
                    {
                        subMenu.AddMenuOption(_map.DisplayName, (player, option) =>
                        {
                            // Close menu
                            MenuManager.CloseActiveMenu(player);

                            // Start vote
                            _pluginState.CustomVotesApi.Get()?.StartCustomVote(player, _map.Name);
                        });
                    }
                    // Open sub menu
                    OpenMenu(subMenu, _config.Maps.Style, player);
                });
            }
        }

        // Define reusable method to update the map menu
        public void UpdateMapMenus()
        {
            // Assign menu
            _pluginState.MapMenu = AssignMenu(_config.Maps.Style, "Map List");

            // Add menu options for each map in the new map list
            foreach (Map _map in _pluginState.CurrentMode.Maps)
            {
                _pluginState.MapMenu.AddMenuOption(_map.DisplayName, (player, option) =>
                {
                    Map _nextMap = _map;

                    // Create message
                    string _message = _localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _nextMap.Name);

                    // Write to chat
                    Server.PrintToChatAll(_message);

                    // Close menu
                    MenuManager.CloseActiveMenu(player);

                    // Change map
                    _serverManager.ChangeMap(_nextMap);

                });
            }

            // Update player vote map menu
            if(_config.Votes.Maps)
            {
                UpdateShowMapMenu();
            }
        }

        // Define reusable method to update the game command menu
        public void UpdateGameMenu()
        {
            // Assign menu
            _pluginState.GameMenu = AssignMenu(_config.Settings.Style, "Game Commands");

            // Add menu options for each map in the new map list
            foreach (string _command in _pluginState.PlayerCommands)
            {
                _pluginState.GameMenu.AddMenuOption(_command, (player, option) =>
                {
                    // Close menu
                    MenuManager.CloseActiveMenu(player);

                    switch(option.Text)
                    {
                        case "!changemap":
                        if (player != null && _config.Votes.Enabled && _config.Votes.Maps && !_config.Votes.AllMaps)
                        {
                            OpenMenu(_pluginState.ShowMapMenu, _config.Maps.Style, player);
                        }
                        else if(player != null && _config.Votes.Enabled && _config.Votes.Maps && _config.Votes.AllMaps)
                        {
                             OpenMenu(_pluginState.ShowMapsMenu, _config.Maps.Style, player);
                        }
                        break;
                        case "!changemode":
                        if (player != null && _config.Votes.Enabled && _config.Votes.GameModes)
                        {
                            OpenMenu(_pluginState.ShowModesMenu, _config.GameModes.Style, player);
                        }
                        break;
                        case "!changesetting":
                        if (player != null && _config.Votes.Enabled && _config.Votes.GameSettings)
                        {
                            OpenMenu(_pluginState.ShowSettingsMenu, _config.Settings.Style, player);
                        }
                        break;
                        case "!currentmode":
                        if (player != null)
                        {
                            // Create message
                            string _message = _localizer.Localize("currentmode.message", _pluginState.CurrentMode.Name);

                            // Write to chat
                            player.PrintToChat(_message);
                        }
                        break;
                        case "!currentmap":
                        if (player != null)
                        {
                            // Create message
                            string _message = _localizer.Localize("currentmap.message", _pluginState.CurrentMap.Name);

                            // Write to chat
                            player.PrintToChat(_message);
                        }
                        break;
                        case "!timeleft":
                        if (player != null)
                        {
                            // Define message
                            string _message;

                            // Define prefix
                            StringLocalizer _timeLocalizer = new StringLocalizer(_iLocalizer, "timeleft.prefix");

                            // If warmup, send general message
                            if (_gameRules.WarmupRunning)
                            {
                                if (player != null)
                                {
                                    player.PrintToChat(_timeLocalizer.LocalizeWithPrefix("timeleft.warmup"));
                                }
                                else
                                {
                                    Server.PrintToConsole(_timeLocalizer.LocalizeWithPrefix("timeleft.warmup"));
                                }
                                return;
                            }

                            // Create message based on map conditions
                            if (!_timeLimitManager.UnlimitedTime) // If time not over
                            {
                                if (_timeLimitManager.TimeRemaining > 1)
                                {
                                    // Get remaining time
                                    TimeSpan remaining = TimeSpan.FromSeconds((double)_timeLimitManager.TimeRemaining);

                                    // If hours left
                                    if (remaining.Hours > 0)
                                    {
                                        _message = _timeLocalizer.LocalizeWithPrefix("timeleft.remaining-time-hour", remaining.Hours.ToString("00"), remaining.Minutes.ToString("00"), remaining.Seconds.ToString("00"));
                                    }
                                    else if (remaining.Minutes > 0) // If minutes left
                                    {
                                        _message = _timeLocalizer.LocalizeWithPrefix("timeleft.remaining-time-minute", remaining.Minutes, remaining.Seconds);
                                    }
                                    else // If seconds left
                                    {
                                        _message = _timeLocalizer.LocalizeWithPrefix("timeleft.remaining-time-second", remaining.Seconds);
                                    }
                                }
                                else // If time over
                                {
                                    _message = _timeLocalizer.LocalizeWithPrefix("timeleft.remaining-time-over");
                                }
                            }
                            else if (!_maxRoundsManager.UnlimitedRounds) // If round limit not reached
                            {
                                if (_maxRoundsManager.RemainingRounds > 1) // If remaining rounds more than 1
                                {
                                    _message = _timeLocalizer.LocalizeWithPrefix("timeleft.remaining-rounds", _maxRoundsManager.RemainingRounds);
                                }
                                else // If last round
                                {
                                    _message = _timeLocalizer.LocalizeWithPrefix("timeleft.last-round");
                                }
                            }
                            else // If no time or round limit
                            {
                                _message = _timeLocalizer.LocalizeWithPrefix("timeleft.no-time-limit");
                            }

                            // Send message    
                            if (player != null)
                            {
                                player.PrintToChat(_message);
                            }
                            else
                            {
                                Server.PrintToConsole(_message);
                            }
                        }
                        break;
                    }
                });
            }
        }

        // Define resuable method to set up user map menu (maps from current game mode)
        public void UpdateShowMapMenu()
        {
            // Assign menu
            _pluginState.ShowMapMenu = AssignMenu(_config.Maps.Style, "Map List");

            foreach (Map _map in _pluginState.CurrentMode.Maps)
            {
                // Add menu option
                _pluginState.ShowMapMenu.AddMenuOption(_map.DisplayName, (player, option) =>
                {
                    // Close menu
                    MenuManager.CloseActiveMenu(player);

                    // Start vote
                    _pluginState.CustomVotesApi.Get()?.StartCustomVote(player, _map.Name);
                });
            }
        }

        // Define resuable method to set up show maps menu
        public void CreateShowModesMenu()
        {
            // Assign menu
            _pluginState.ShowModesMenu = AssignMenu(_config.GameModes.Style, "Game Mode List");

            foreach (Mode _mode in _pluginState.Modes)
            {
                // Add menu option
                _pluginState.ShowModesMenu.AddMenuOption(_mode.Name, (player, option) =>
                {
                    // Close menu
                    MenuManager.CloseActiveMenu(player);

                    // Start vote
                    _pluginState.CustomVotesApi.Get()?.StartCustomVote(player, Extensions.RemoveCfgExtension(_mode.Config));
                });
            }
        }

        // Define resuable method to set up show maps menu
        public void CreateShowSettingsMenu()
        {
            // Assign menu
            _pluginState.ShowSettingsMenu = AssignMenu(_config.Settings.Style, "Setting List");
            
            foreach (Setting _setting in _pluginState.Settings)
            {
                // Add menu option
                _pluginState.ShowSettingsMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
                {
                    // Close menu
                    MenuManager.CloseActiveMenu(player);

                    // Start vote
                    _pluginState.CustomVotesApi.Get()?.StartCustomVote(player, _setting.Name); 
                });
            }
        }
    }
}