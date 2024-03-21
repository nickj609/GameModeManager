// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;

// Declare namespace
namespace GameModeManager
{     
    public partial class Plugin : BasePlugin
    {    
        // Construct server map group command handler
        [ConsoleCommand("css_mapgroup", "Sets the mapgroup for the MapListUpdater plugin.")]
        [CommandHelper(minArgs: 1, usage: "[mapgroup]", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnMapGroupCommand(CCSPlayerController player, CommandInfo command)
        {
            Logger.LogInformation("OnMapGroupCommand execution started");
            if (player == null) 
            {
                Logger.LogInformation($"Map group command detected!");

                // Check map group to make sure its not already set or set incorrectly
                MapGroup? newMapGroup = mapGroups.FirstOrDefault(g => g.Name == $"{command.ArgByIndex(1)}");

                Logger.LogInformation($"Current MapGroup is {currentMapGroup}.");
                Logger.LogInformation($"New MapGroup is {newMapGroup}.");

                if (currentMapGroup == newMapGroup)
                {
                    Logger.LogInformation("Mapgroup is the same. Not updating map list...");

                    return;
                }
                else if (newMapGroup == null)
                {
                    Logger.LogInformation("Mapgroup could not be found. Setting default map group");
                    newMapGroup = defaultMapGroup;
                }

                // UpdateMapList
                try
                {
                    Logger.LogInformation($"Finding new map list for new map group...");
                    UpdateMapList(newMapGroup);
                }
                catch(Exception ex)
                {
                    Logger.LogError($"{ex.Message}");
                }

                // Reset new map group
                currentMapGroup = newMapGroup;
                newMapGroup.Clear();
            }
        }

        // Construct admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_maps", "Provides a list of maps for the current game mode.")]
        public void OnMapsCommand(CCSPlayerController player, CommandInfo command)
        {
            Logger.LogInformation("OnMapsCommand execution started");
            if(player != null && _plugin != null)
            {
                MenuManager.OpenCenterHtmlMenu(_plugin, player, mapMenu);
            }
        }

        // Construct admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_modes", "Provides a list of game modes")]
        public void OnModesCommand(CCSPlayerController player, CommandInfo command)
        {
            Logger.LogInformation("OnModesCommand execution started");
            if(player != null && _plugin != null)
            {
                MenuManager.OpenCenterHtmlMenu(_plugin, player, modeMenu);
            }
        }
    }
}