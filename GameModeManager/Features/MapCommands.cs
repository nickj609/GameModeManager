// Included libraries
using GameModeManager.Menus;
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class MapCommands : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private MapMenus _mapMenus;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;
        private Config _config = new Config();

        // Define class constructor
        public MapCommands(PluginState pluginState, StringLocalizer localizer, ServerManager serverManager, MapMenus mapMenus)
        {
            _mapMenus = mapMenus;
            _localizer = localizer;
            _pluginState = pluginState;
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
            if (_config.Commands.Map)
            {
                plugin.AddCommand("css_map", "Changes the map to the map specified in the command argument.", OnMapCommand);
            }
            if (_config.Commands.Maps)
            {
                plugin.AddCommand("css_maps", "Displays a list of maps from the current mode.", OnMapsCommand);
            }
        }

        // Define admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnMapsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                _mapMenus.MainMenu?.Open(player);
            }
        }

        // Define admin change map command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "<map name> optional: <workshop id>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void OnMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            IMap? newMap;
            string mapName = command.ArgByIndex(1);
            
            if (long.TryParse(command.ArgByIndex(2), out long workshopId) && _pluginState.Game.MapsByWorkshopId.TryGetValue(workshopId, out IMap? workshopMap))
            {
                newMap = workshopMap;
            }
            else if (_pluginState.Game.Maps.TryGetValue(mapName, out IMap? map))
            {
                newMap = map;
            }
            else
            {
                newMap = new Map(mapName, workshopId);
            }

            if (newMap == null)
            {
                command.ReplyToCommand($"Failed to find or create map for command: {mapName} (Workshop ID: {workshopId})");
                return; 
            }

            if (player != null)
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, newMap.DisplayName));
            }

            _serverManager.ChangeMap(newMap, _config.Maps.Delay);
        }
    }
}