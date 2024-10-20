// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Models;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class MapMenus : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;
        private Config _config = new Config();

        // Define class instance
        public MapMenus(MenuFactory menuFactory, PluginState pluginState, StringLocalizer localizer, ServerManager serverManager)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _serverManager = serverManager;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        public void LoadWASDMenus()
        {
            // Update map menus
            if (_config.Maps.Style.Equals("wasd", StringComparison.OrdinalIgnoreCase))
            {
                UpdateWASDMenus();
            }

            // Create all map(s) menu
            if (_config.Maps.Style.Equals("wasd", StringComparison.OrdinalIgnoreCase) && _config.Commands.AllMaps)
            {
                // Assign menu
                _pluginState.MapWASDMenu = _menuFactory.AssignWasdMenu("Map List");

                // Add menu options for each map in the new map list
                foreach (Map _map in _pluginState.Maps)
                {
                    _pluginState.MapWASDMenu?.Add(_map.DisplayName, (player, option) =>
                    {
                        _menuFactory.CloseWasdMenu(player);
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _map.Name));
                        _serverManager.ChangeMap(_map);
                    });
                }
            }

            // Create vote all map(s) menu
            if (_config.Maps.Style.Equals("wasd") && _config.Votes.Maps)
            {
                // Assign menu
                _pluginState.VoteMapsWASDMenu = _menuFactory.AssignWasdMenu("Map List");

                // Add menu options for each map in map list
                foreach (Map _map in _pluginState.Maps)
                {
                    _pluginState.VoteMapsWASDMenu?.Add(_map.DisplayName, (player, option) =>
                    {
                         _menuFactory.CloseWasdMenu(player);
                         _pluginState.CustomVotesApi.Get()?.StartCustomVote(player, _map.Name);
                    });
                }
            }
        }
        // Define load behavior
        public void Load()
        {
            // Create map menus (maps from current game mode)
            UpdateMenus();

            // Create all maps menu
            if (_config.Commands.AllMaps)
            {
                // Assign menu
                _pluginState.MapsMenu = _menuFactory.AssignMenu(_config.Maps.Style, "Select a game mode.");

                // Add menu option for each game mode in game mode list
                foreach (Mode _mode in _pluginState.Modes)
                {
                    _pluginState.MapsMenu.AddMenuOption(_mode.Name, (player, option) =>
                    {
                        // Create sub menu
                        BaseMenu subMenu;
                        subMenu = _menuFactory.AssignMenu(_config.Maps.Style, _localizer.Localize("maps.menu-title"));

                        // Add menu option for each map in map list
                        foreach (Map _map in _mode.Maps)
                        {
                            subMenu.AddMenuOption(_map.DisplayName, (player, option) =>
                            {
                                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _map.Name));
                                MenuManager.CloseActiveMenu(player);
                                _serverManager.ChangeMap(_map);
                            });
                        } 
                        // Open menu
                        _menuFactory.OpenMenu(subMenu, player);
                    });
                }
            }

            // Create vote all map(s) menu
            if (_config.Maps.Style.Equals("wasd") && _config.Votes.Maps)
            {
                // Assign menu
                _pluginState.VoteMapsMenu = _menuFactory.AssignMenu(_config.Maps.Style, "Select a game mode.");

                // Add menu options for each mode in mode list
                foreach (Mode _mode in _pluginState.Modes)
                {
                    _pluginState.VoteMapsMenu.AddMenuOption(_mode.Name, (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Create sub menu
                        BaseMenu subMenu;
                        subMenu = _menuFactory.AssignMenu(_config.Maps.Style, _localizer.Localize("maps.menu-title"));

                        // Add menu options for each map in map list
                        foreach (Map _map in _mode.Maps)
                        {
                            subMenu.AddMenuOption(_map.DisplayName, (player, option) =>
                            {
                                MenuManager.CloseActiveMenu(player);
                                _pluginState.CustomVotesApi.Get()?.StartCustomVote(player, _map.Name);
                            });
                        }
                        // Open sub menu
                        _menuFactory.OpenMenu(subMenu, player);
                    });
                }
            }
        }

        // Define reusable method to update the map menu
        public void UpdateMenus()
        {
            // Update map menu 
            _pluginState.MapMenu = _menuFactory.AssignMenu(_config.Maps.Style, "Map List");

            // Add menu options for each map in the new map list
            foreach (Map _map in _pluginState.CurrentMode.Maps)
            {
                _pluginState.MapMenu.AddMenuOption(_map.DisplayName, (player, option) =>
                {
                    MenuManager.CloseActiveMenu(player);
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _map.Name));
                    _serverManager.ChangeMap(_map);
                });
            }

            // Update vote map menu
            _pluginState.VoteMapMenu = _menuFactory.AssignMenu(_config.Maps.Style, "Map List");

            // Add menu options for each map in the current mode map list
            foreach (Map _map in _pluginState.CurrentMode.Maps)
            {
                // Add menu option
                _pluginState.VoteMapMenu.AddMenuOption(_map.DisplayName, (player, option) =>
                {
                    // Close menu
                    MenuManager.CloseActiveMenu(player);

                    // Start vote
                    _pluginState.CustomVotesApi.Get()?.StartCustomVote(player, _map.Name);
                });
            }  
        }

        // Define reusable method to update the map menu
        public void UpdateWASDMenus()
        {  
            // Update map menu
            _pluginState.MapWASDMenu = _menuFactory.AssignWasdMenu("Map List");

            // Add menu options for each map in the new map list
            foreach (Map _map in _pluginState.CurrentMode.Maps)
            {
                _pluginState.MapWASDMenu?.Add(_map.DisplayName, (player, option) =>
                {
                    _menuFactory.CloseWasdMenu(player);
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _map.Name));
                    _serverManager.ChangeMap(_map);
                });
            }

            // Update vote map menu
            _pluginState.VoteMapWASDMenu = _menuFactory.AssignWasdMenu("Map List");

            // Add menu options for each map in the current mode map list
            foreach (Map _map in _pluginState.CurrentMode.Maps)
            {
                // Add menu option
                _pluginState.VoteMapMenu.AddMenuOption(_map.DisplayName, (player, option) =>
                {
                    // Close menu
                    _menuFactory.CloseWasdMenu(player);

                    // Start vote
                    _pluginState.CustomVotesApi.Get()?.StartCustomVote(player, _map.Name);
                });
            }
        }
    }
}