// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin
    {
        // Construct admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_maps", "Provides a list of maps from the current game mode.")]
        public void OnMapsCommand(CCSPlayerController? player, CommandInfo command)
        {

            if(player != null && MenuFactory.MapMenu != null)
            {
                MenuFactory.MapMenu.Title = Localizer["maps.menu-title"];
                MenuFactory.OpenMenu(MenuFactory.MapMenu, Config.GameMode.Style, player);
            }
            else
            {
                Console.Error.WriteLine("css_maps is a client only command.");
            }
        }

        // Construct admin change map command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "<map_name> optional: <workshop id>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_map", "Changes the map to the map specified in the command argument.")]
        public void OnMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && PluginState.Maps != null)
            {
                // Find map
                Map _newMap = new Map($"{command.ArgByIndex(1)}",$"{command.ArgByIndex(2)}");
                Map? _foundMap = PluginState.Maps.FirstOrDefault(g => g.Name == $"{command.ArgByIndex(1)}");

                if (_foundMap != null)
                {
                    // Assign map
                    _newMap = _foundMap; 
                }

                // Write to chat
                Server.PrintToChatAll(Localizer["plugin.prefix"] + " " + Localizer["changemap.message", player.PlayerName, _newMap.Name]);

                // Change map
                AddTimer(Config.MapGroup.Delay, () => 
                {
                    MapManager.ChangeMap(_newMap);
                }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            }
            else
            {
                Console.Error.WriteLine("css_map is a client only command.");
            }
        }
    }
}