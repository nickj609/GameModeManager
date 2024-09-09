// Included libraries
using GameModeManager.Core;
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
        private Plugin? _plugin;
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;
        private Config _config = new Config();

        // Define class instance
        public MapCommands(PluginState pluginState, MenuFactory menuFactory, StringLocalizer localizer)
        {
            _localizer = localizer;
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

            // Enable commands if enabled in the config. 
            if (_config.Commands.Map)
            {
                _plugin.AddCommand("css_map", "Changes the map to the map specified in the command argument.", OnMapCommand);
            }

            if (_config.Commands.Maps)
            {
                _plugin.AddCommand("css_maps", "Displays a list of maps from the current mode.", OnMapsCommand);
            }

            if (_config.Commands.AllMaps)
            {
                _plugin.AddCommand("css_allmaps", "Displays a list of modes and their maps.", OnAllMapsCommand);
            }
        }

        // Define admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnMapsCommand(CCSPlayerController? player, CommandInfo command)
        {

            if(player != null)
            {
                _pluginState.MapMenu.Title = _localizer.Localize("maps.menu-title");
                _menuFactory.OpenMenu(_pluginState.MapMenu, _config.GameModes.Style, player);
            }
            else
            {
                command.ReplyToCommand("css_maps is a client only command.");
            }
        }

        // Define admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnAllMapsCommand(CCSPlayerController? player, CommandInfo command)
        {

            if(player != null)
            {
                _pluginState.MapsMenu.Title = _localizer.Localize("modes.menu-title");
                _menuFactory.OpenMenu(_pluginState.MapsMenu, _config.GameModes.Style, player);
            }
            else
            {
                command.ReplyToCommand("css_maps is a client only command.");
            }
        }

        // Define admin change map command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "<map_name> optional: <workshop id>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                // Find map
                Map _newMap = new Map($"{command.ArgByIndex(1)}",$"{command.ArgByIndex(2)}");
                Map? _foundMap = _pluginState.Maps.FirstOrDefault(g => g.Name.Equals($"{command.ArgByIndex(1)}", StringComparison.OrdinalIgnoreCase));

                if (_foundMap != null)
                {
                    // Assign map
                    _newMap = _foundMap; 
                }

                // Write to chat
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, _newMap.Name));

                if(_plugin != null)
                {
                    // Change map
                    _plugin.AddTimer(_config.Maps.Delay, () => 
                    {
                        ServerManager.ChangeMap(_newMap, _config, _plugin, _pluginState);
                    }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
                }
            }
            else
            {
                command.ReplyToCommand("css_map is a client only command.");
            }
        }
    }
}