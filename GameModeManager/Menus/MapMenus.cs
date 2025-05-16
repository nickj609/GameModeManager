// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using WASDMenuAPI.Shared.Models;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class MapMenus : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;
        private Config _config = new Config();
        private MenuFactory _menuFactory = new MenuFactory();

        // Define class instance
        public MapMenus(PluginState pluginState, StringLocalizer localizer, ServerManager serverManager)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _serverManager = serverManager;
        }

        // Define class properties
        private IWasdMenu? mapWasdMenu;
        private IWasdMenu? voteMapWasdMenu;
        private IWasdMenu? voteMapsWasdMenu;

        private BaseMenu mapMenu = new ChatMenu("Map List");
        private BaseMenu mapsMenu = new ChatMenu("Map List");
        private BaseMenu voteMapMenu = new ChatMenu("Map List");

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }
        
        // Define methods to get menus
        public BaseMenu GetMenu(string Name)
        {
            if (Name.Equals("All"))
            {
                return mapsMenu;
            }
            else if (Name.Equals("CurrentMode"))
            {
                return mapMenu;
            }
            else
            {
                return voteMapMenu;
            }
        }

        public IWasdMenu? GetWasdMenu(string Name)
        {
            if (Name.Equals("CurrentMode"))
            {
                return mapWasdMenu;
            }
            else if (Name.Equals("VoteAll"))
            {
                return voteMapWasdMenu;
            }
            else
            {
                return voteMapsWasdMenu;
            }
        }

        // Define load behavior
        public void Load()
        {
            // Create map menus (maps from current game mode)
            UpdateMenus();

            // Create all maps menu
            if (_config.Maps.Mode == 1)
            {
                mapsMenu = _menuFactory.BaseMenus.AssignMenu(_config.Maps.Style, _localizer.Localize("maps.menu-title"));

                // Add menu option for each game mode in game mode list
                foreach (IMode _mode in _pluginState.Modes)
                {
                    mapsMenu.AddMenuOption(_mode.Name, (player, option) =>
                    {
                        // Create sub menu
                        BaseMenu subMenu;
                        subMenu = _menuFactory.BaseMenus.AssignMenu(_config.Maps.Style, _localizer.Localize("maps.menu-title"));

                        // Add menu option for each map in map list
                        foreach (IMap _map in _mode.Maps)
                        {
                            subMenu.AddMenuOption(_map.DisplayName, (player, option) =>
                            {
                                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _map.Name));
                                _menuFactory.BaseMenus.CloseMenu(player);
                                _serverManager.ChangeMap(_map, _config.Maps.Delay);
                            });
                        } 
                        // Open menu
                        _menuFactory.BaseMenus.OpenMenu(subMenu, player);
                    });
                }
            }
        }

        // Define method to load WASD menus
        public void LoadWASDMenus()
        {
            // Update map menus
            if (_config.Maps.Style.Equals("wasd", StringComparison.OrdinalIgnoreCase))
            {
                UpdateWASDMenus();
            }

            // Create all map(s) menu
            if (_config.Maps.Style.Equals("wasd", StringComparison.OrdinalIgnoreCase) && _config.Maps.Mode == 1)
            {
                mapWasdMenu = _menuFactory.WasdMenus.AssignMenu("Map List");

                // Add menu options for each map in the new map list
                foreach (IMap _map in _pluginState.Maps)
                {
                    mapWasdMenu?.Add(_map.DisplayName, (player, option) =>
                    {
                        _menuFactory.WasdMenus.CloseMenu(player);
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _map.Name));
                        _serverManager.ChangeMap(_map, _config.Maps.Delay);
                    });
                }
            }

            // Create vote all map(s) menu
            if (_config.Maps.Style.Equals("wasd") && _config.Votes.Maps)
            {
                voteMapsWasdMenu = _menuFactory.WasdMenus.AssignMenu("Map List");

                // Add menu options for each map in map list
                foreach (IMap _map in _pluginState.Maps)
                {
                    voteMapsWasdMenu?.Add(_map.DisplayName, (player, option) =>
                    {
                         _menuFactory.WasdMenus.CloseMenu(player);
                         CustomVoteManager.CustomVotesApi.Get()?.StartCustomVote(player, _map.Name);
                    });
                }
            }
        }

        // Define method to update the map menu
        public void UpdateMenus()
        {
            // Update map menu 
            mapMenu = _menuFactory.BaseMenus.AssignMenu(_config.Maps.Style, "Map List");

            // Add menu options for each map in the new map list
            foreach (IMap _map in _pluginState.CurrentMode.Maps)
            {
                mapMenu.AddMenuOption(_map.DisplayName, (player, option) =>
                {
                    _menuFactory.BaseMenus.CloseMenu(player);
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _map.Name));
                    _serverManager.ChangeMap(_map, _config.Maps.Delay);
                });
            }

            // Update vote map menu
            voteMapMenu = _menuFactory.BaseMenus.AssignMenu(_config.Maps.Style, "Map List");

            // Add menu options for each map in the current mode map list
            foreach (IMap _map in _pluginState.CurrentMode.Maps)
            {
                voteMapMenu.AddMenuOption(_map.DisplayName, (player, option) =>
                {
                    // Close menu
                    _menuFactory.BaseMenus.CloseMenu(player);

                    // Start vote
                    CustomVoteManager.CustomVotesApi.Get()?.StartCustomVote(player, _map.Name);
                });
            }  
        }

        // Define method to update the map menu
        public void UpdateWASDMenus()
        {  
            // Update map menu
            mapWasdMenu = _menuFactory.WasdMenus.AssignMenu("Map List");

            // Add menu options for each map in the new map list
            foreach (IMap _map in _pluginState.CurrentMode.Maps)
            {
                mapWasdMenu?.Add(_map.DisplayName, (player, option) =>
                {
                    _menuFactory.WasdMenus.CloseMenu(player);
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _map.Name));
                    _serverManager.ChangeMap(_map, _config.Maps.Delay);
                });
            }

            // Update vote map menu
            voteMapWasdMenu = _menuFactory.WasdMenus.AssignMenu("Map List");

            // Add menu options for each map in the current mode map list
            foreach (IMap _map in _pluginState.CurrentMode.Maps)
            {
                // Add menu option
                voteMapWasdMenu?.Add(_map.DisplayName, (player, option) =>
                {
                    // Close menu
                    _menuFactory.WasdMenus.CloseMenu(player);

                    // Start vote
                    CustomVoteManager.CustomVotesApi.Get()?.StartCustomVote(player, _map.Name);
                });
            }
        }
    }
}