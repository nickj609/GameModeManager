// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Core.Attributes.Registration;

// Declare namespace
namespace GameModeManager
{     
    public partial class Plugin : BasePlugin
    {    
        // Construct server map group command handler
        [ConsoleCommand("css_mapgroup", "Sets the mapgroup for the MapListUpdater plugin.")]
        [CommandHelper(minArgs: 1, usage: "mg_active", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnMapGroupCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) 
            {
                // Get map group
                MapGroup? newMapGroup = mapGroups.FirstOrDefault(g => g.Name == $"{command.ArgByIndex(1)}");

                if (newMapGroup == null || newMapGroup.Name == null || newMapGroup.Maps == null)
                {
                    Logger.LogWarning("New map group could not be found. Setting default map group.");
                    newMapGroup = defaultMapGroup;
                }
                Logger.LogInformation($"Current map group is {currentMapGroup.Name}.");
                Logger.LogInformation($"New map group is {newMapGroup.Name}.");

                // Update map list and map menu
                try
                {
                    UpdateMapList(newMapGroup);
                }
                catch(Exception ex)
                {
                    Logger.LogError($"{ex.Message}");
                }
            }
        }

        // Construct change map command
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "[map name] optional: [id]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_map", "Changes the map to the map specified in the command argument.")]
        public void OnMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null)
            {
                Map newMap = new Map($"{command.ArgByIndex(1)}",$"{command.ArgByIndex(2)}");
                Map? foundMap = allMaps.FirstOrDefault(g => g.Name == $"{command.ArgByIndex(1)}");

                if (foundMap != null)
                {
                    newMap = foundMap; 
                }
                // Write to chat
                Server.PrintToChatAll(Localizer["changemap.message", player.PlayerName, newMap.Name]);
                // Change map
                AddTimer(5.0f, () => ChangeMap(newMap));
            }
        }
        // Construct admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_maps", "Provides a list of maps for the current game mode.")]
        public void OnMapsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null)
            {
                mapMenu.Title = Localizer["maps.hud.menu-title"];
                MenuManager.OpenCenterHtmlMenu(_plugin, player, mapMenu);
            }
        }
        // Construct change mode command
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "[mode]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_mode", "Changes the game mode to the mode specified in the command argument.")]
        public void OnModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null)
            {
                // Write to chat
                Server.PrintToChatAll(Localizer["changemode.message", player.PlayerName, command.ArgByIndex(1)]);

                // Change game mode
                string newMode = $"{command.ArgByIndex(1)}".ToLower();
                AddTimer(5.0f, () => Server.ExecuteCommand($"exec {newMode}.cfg"));
            }
        }

        // Construct admin mode menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_modes", "Provides a list of game modes.")]
        public void OnModesCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null)
            {
                modeMenu.Title = Localizer["mode.hud.menu-title"];
                MenuManager.OpenCenterHtmlMenu(_plugin, player, modeMenu);
            }
        }
    }
}