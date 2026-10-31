// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
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

        // Define class constructor
        public MapMenus(PluginState pluginState, StringLocalizer localizer, ServerManager serverManager)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _serverManager = serverManager;
        }

        // Define class properties
        public IMenu? MainMenu;

        // Define load method
        // Built once per mode: ModeCommands calls this again when the mode changes
        public void Load()
        {
            MainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("maps.menu-title"));

            foreach (IMap _map in _pluginState.Game.CurrentMode.Maps)
            {
                MainMenu?.AddMenuOption(_map.DisplayName, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _map.DisplayName));
                    _serverManager.ChangeMapInPool(_map);
                });
            }
        }
    }
}
