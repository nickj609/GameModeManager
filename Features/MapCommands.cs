// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager
{
    public class MapCommands : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Config? _config;
        private Plugin? _plugin;
        private MapManager _mapManager;
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;

        // Define class instance
        public MapCommands(PluginState pluginState, MenuFactory menuFactory, MapManager mapManager, StringLocalizer localizer)
        {
            _localizer = localizer;
            _mapManager = mapManager;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
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
            _plugin.AddCommand("css_maps", "Displays a list of maps from the current mode.", OnMapsCommand);
            _plugin.AddCommand("css_allmaps", "Displays a list of modes and their maps.", OnAllMapsCommand);

            if (_config != null && _config.Commands.Map)
            {
                _plugin.AddCommand("css_map", "Changes the map to the map specified in the command argument.", OnMapCommand);
            }
        }

        // Define admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnMapsCommand(CCSPlayerController? player, CommandInfo command)
        {

            if(player != null && _pluginState.MapMenu != null && _config != null)
            {
                _pluginState.MapMenu.Title = _localizer.Localize("maps.menu-title");
                _menuFactory.OpenMenu(_pluginState.MapMenu, _config.GameModes.Style, player);
            }
            else
            {
                Console.Error.WriteLine("css_maps is a client only command.");
            }
        }

        // Define admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnAllMapsCommand(CCSPlayerController? player, CommandInfo command)
        {

            if(player != null && _pluginState.MapsMenu != null && _config != null)
            {
                _pluginState.MapsMenu.Title = _localizer.Localize("modes.menu-title");
                _menuFactory.OpenMenu(_pluginState.MapsMenu, _config.GameModes.Style, player);
            }
            else
            {
                Console.Error.WriteLine("css_maps is a client only command.");
            }
        }

        // Define admin change map command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "<map_name> optional: <workshop id>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _config != null && _plugin != null)
            {
                // Find map
                Map _newMap = new Map($"{command.ArgByIndex(1)}",$"{command.ArgByIndex(2)}");
                Map? _foundMap = _pluginState.Maps.FirstOrDefault(g => g.Name == $"{command.ArgByIndex(1)}");

                if (_foundMap != null)
                {
                    // Assign map
                    _newMap = _foundMap; 
                }

                // Write to chat
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _newMap.Name));

                // Change map
                _plugin.AddTimer(_config.MapGroups.Delay, () => 
                {
                    _mapManager.ChangeMap(_newMap);
                }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            }
            else
            {
                Console.Error.WriteLine("css_map is a client only command.");
            }
        }
    }
}