// Included libraries
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class MapCommands : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;
        private Config _config = new Config();

        // Define class instance
        public MapCommands(PluginState pluginState, MenuFactory menuFactory, StringLocalizer localizer, ServerManager serverManager)
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

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            // Enable commands if enabled in the config. 
            if (_config.Commands.Map)
            {
                plugin.AddCommand("css_map", "Changes the map to the map specified in the command argument.", OnMapCommand);
            }

            if (_config.Commands.Maps)
            {
                plugin.AddCommand("css_maps", "Displays a list of maps from the current mode.", OnMapsCommand);
            }

            if (_config.Commands.AllMaps)
            {
                plugin.AddCommand("css_allmaps", "Displays a list of modes and their maps.", OnAllMapsCommand);
            }
        }

        // Define admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnMapsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                if (_config.Maps.Style.Equals("wasd") && _pluginState.MapWASDMenu != null)
                {
                    _menuFactory.OpenWasdMenu(player, _pluginState.MapWASDMenu);
                }
                else
                {
                    _pluginState.MapMenu.Title = _localizer.Localize("maps.menu-title");
                    _menuFactory.OpenMenu(_pluginState.MapMenu, player);
                }
            }
        }

        // Define admin all maps menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnAllMapsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                if (_config.Maps.Style.Equals("wasd") && _pluginState.MapsWASDMenu != null)
                {
                    _menuFactory.OpenWasdMenu(player, _pluginState.MapsWASDMenu);
                }
                else
                {
                    _pluginState.MapsMenu.Title = _localizer.Localize("modes.menu-title");
                    _menuFactory.OpenMenu(_pluginState.MapsMenu, player);
                }
            }
        }

        // Define admin change map command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "[map name] optional: [workshop id]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void OnMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            // Find map
            Map _newMap = new Map($"{command.ArgByIndex(1)}",$"{command.ArgByIndex(2)}");
            Map? _foundMap = _pluginState.Maps.FirstOrDefault(g => g.Name.Equals($"{command.ArgByIndex(1)}", StringComparison.OrdinalIgnoreCase) || g.WorkshopId.ToString().Equals("{command.ArgByIndex(2)}", StringComparison.OrdinalIgnoreCase));

            if (_foundMap != null)
            {
                _newMap = _foundMap; 
            }

            // Print to chat
            if(player != null)
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _newMap.Name));
            }

            // Change map
            _serverManager.ChangeMap(_newMap, _config.Maps.Delay);
        }
    }
}