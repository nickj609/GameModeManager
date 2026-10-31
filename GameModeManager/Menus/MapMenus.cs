// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
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
        private MapChooserBridge _mapChooser;

        // Define class constructor
        public MapMenus(PluginState pluginState, StringLocalizer localizer, ServerManager serverManager, MapChooserBridge mapChooser)
        {
            _localizer = localizer;
            _mapChooser = mapChooser;
            _pluginState = pluginState;
            _serverManager = serverManager;
        }

        // Define class properties
        public IMenu? MainMenu; // Admin menu (!maps)

        // Define load method
        // Built once per mode: ModeCommands calls this again when the mode changes
        public void Load()
        {
            IMenu? listMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("maps.menu-title"), player => MainMenu?.Open(player));

            foreach (IMap _map in _pluginState.Game.CurrentMode.Maps)
            {
                listMenu?.AddMenuOption(_map.DisplayName, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    ChangeMap(player, _map);
                });
            }

            MainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.maps"));
            MainMenu?.AddMenuOption(_localizer.Localize("admin.change-map"), (player, option) => listMenu?.Open(player));
            MainMenu?.AddMenuOption(_localizer.Localize("admin.start-vote"), (player, option) =>
            {
                MenuFactory.Api?.CloseMenu(player);
                StartVote(player);
            });
        }

        // Define class methods
        public void ChangeMap(CCSPlayerController player, IMap map)
        {
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, map.DisplayName));
            _serverManager.ChangeMapInPool(map);
        }

        // GG1MapChooser's vote across several maps
        public void StartVote(CCSPlayerController player)
        {
            if (!_mapChooser.StartMapVote())
                player.PrintToChat(_localizer.LocalizeWithPrefix("admin.vote-unavailable"));
        }
    }
}
