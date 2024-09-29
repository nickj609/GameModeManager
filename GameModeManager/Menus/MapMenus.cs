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

        // Define load behavior
        public void Load()
        {
            // Create all map(s) menu
            _pluginState.MapsMenu = _menuFactory.AssignMenu(_config.Maps.Style, "Select a game mode.");

            foreach (Mode _mode in _pluginState.Modes)
            {
                _pluginState.MapsMenu.AddMenuOption(_mode.Name, (player, option) =>
                {
                    // Create sub menu
                    BaseMenu subMenu;
                    subMenu = _menuFactory.AssignMenu(_config.Maps.Style, _localizer.Localize("maps.menu-title"));

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

            // Create map menu (maps from current game mode)
            UpdateMapMenus();

            // Create user all map(s) menu
            if(_config.Votes.Maps)
            {
                CreateVoteAllMapsMenus();
            }
        }

        // Define resuable method to show all maps menu
        public void CreateVoteAllMapsMenus()
        {
            _pluginState.VoteMapsMenu = _menuFactory.AssignMenu(_config.Maps.Style, "Select a game mode.");

            foreach (Mode _mode in _pluginState.Modes)
            {
                _pluginState.VoteMapsMenu.AddMenuOption(_mode.Name, (player, option) =>
                {
                    // Close menu
                    MenuManager.CloseActiveMenu(player);

                    // Create sub menu
                    BaseMenu subMenu;
                    subMenu = _menuFactory.AssignMenu(_config.Maps.Style, _localizer.Localize("maps.menu-title"));

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

        // Define reusable method to update the map menu
        public void UpdateMapMenus()
        {
            // Assign menu
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

            // Update player vote map menu
            if(_config.Votes.Maps)
            {
                UpdateVoteMapMenu();
            }
        }

         // Define resuable method to set up user map menu (maps from current game mode)
        public void UpdateVoteMapMenu()
        {
            // Assign menu
            _pluginState.VoteMapMenu = _menuFactory.AssignMenu(_config.Maps.Style, "Map List");

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
        
    }
}