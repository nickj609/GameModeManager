// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Logging;

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

        // Define class constructor
        public MapMenus(PluginState pluginState, StringLocalizer localizer, ServerManager serverManager)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _serverManager = serverManager;
        }
        
        // Define class properties
        public IMenu? MainMenu;
        public IMenu? VoteMenu;

        // Define load method  
        public void Load()
        {
            _pluginState.Game.Maps.TryGetValue(_pluginState.Game.CurrentMap.Name, out var currentMap);
            _pluginState.Game.Modes.TryGetValue(_pluginState.Game.CurrentMode.Name, out var currentMode);

            if (_config.Maps.Mode == 1)
            {
                // Create main menu
                MainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("maps.menu-title"));

                foreach (IMap? _map in _pluginState.Game.Maps.Values)
                {
                    MainMenu?.AddMenuOption(_map.DisplayName, (player, option) =>
                    {
                        MenuFactory.Api?.CloseMenu(player);
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _map.Name));
                        _serverManager.ChangeMap(_map, _config.Maps.Delay);
                    });
                }

                // Create vote menu
                VoteMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("maps.menu-title"));

                foreach (IMap? _map in _pluginState.Game.Maps.Values)
                {
                    VoteMenu?.AddMenuOption(_map.DisplayName, (player, option) =>
                    {
                        MenuFactory.Api?.CloseMenu(player);
                        CustomVoteManager.CustomVotesApi.Get()?.StartCustomVote(player, _map.Name);
                    });
                }
            }
            else if (_config.Maps.Mode == 0)
            {
                // Create main menu
                MainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("maps.menu-title"));

                foreach (IMap _map in _pluginState.Game.CurrentMode.Maps)
                {
                    MainMenu?.AddMenuOption(_map.DisplayName, (player, option) =>
                    {
                        MenuFactory.Api?.CloseMenu(player);
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _map.Name));
                        _serverManager.ChangeMap(_map, _config.Maps.Delay);
                    });
                }

                // Create vote menu
                VoteMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("maps.menu-title"));

                foreach (IMap _map in _pluginState.Game.CurrentMode.Maps.ToList())
                {
                    VoteMenu?.AddMenuOption(_map.DisplayName, (player, option) =>
                    {
                        MenuFactory.Api?.CloseMenu(player);
                        CustomVoteManager.CustomVotesApi.Get()?.StartCustomVote(player, _map.Name);
                    });
                }
            }
        }
    }
}