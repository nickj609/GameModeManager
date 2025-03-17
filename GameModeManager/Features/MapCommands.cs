// Included libraries
using WASDSharedAPI;
using GameModeManager.Menus;
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;
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
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;
        private Config _config = new Config();

        // Define class instance
        public MapCommands(PluginState pluginState, MenuFactory menuFactory, StringLocalizer localizer, ServerManager serverManager, MapMenus mapMenus)
        {
            _mapMenus = mapMenus;
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
            if(player != null)
            {
                if (_config.Maps.Style.Equals("wasd"))
                {
                    if (_config.Maps.Mode == 0)
                    {
                        IWasdMenu? menu;
                        menu = _mapMenus.GetWasdMenu("CurrentMode");

                        if(menu != null)
                        {
                            _menuFactory.OpenWasdMenu(player, menu);
                        }
                    }
                    else if (_config.Maps.Mode == 1)
                    {
                        IWasdMenu? menu;
                        menu = _mapMenus.GetWasdMenu("All");

                        if(menu != null)
                        {
                            _menuFactory.OpenWasdMenu(player, menu);
                        }
                    }
                }
                else
                {
                    if (_config.Maps.Mode == 0)
                    {
                        BaseMenu menu;
                        menu = _mapMenus.GetMenu("CurrentMode");
                        _menuFactory.OpenMenu(menu, player);
                    }
                    else
                    {
                        BaseMenu menu;
                        menu = _mapMenus.GetMenu("All");
                        _menuFactory.OpenMenu(menu, player);
                    }
                }
            }
        }

        // Define admin change map command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "[map name] optional: [workshop id]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void OnMapCommand(CCSPlayerController? player, CommandInfo command)
        {
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