// Included libraries
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Admin;
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
        [CommandHelper(minArgs: 1, usage: "[mapgroup]", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnMapGroupCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) 
            {
                Logger.LogInformation($"Map group command detected!");
                Logger.LogInformation($"Current MapGroup is {currentMapGroup.Name}.");

                // Check map group to make sure its not already set or set incorrectly
                MapGroup? newMapGroup = mapGroups.FirstOrDefault(g => g.Name == $"{command.ArgByIndex(1)}");

                if (currentMapGroup == newMapGroup)
                {
                    Logger.LogInformation("Mapgroup is the same. No updates needed.");
                    return;
                }

                if (newMapGroup == null || newMapGroup.Name == null || newMapGroup.Maps == null)
                {
                    Logger.LogInformation("New mapgroup could not be found. Setting default map group.");
                    newMapGroup = defaultMapGroup;
                }
               
                Logger.LogInformation($"New MapGroup is {newMapGroup.Name}.");

                // UpdateMapList
                try
                {
                    UpdateMapList(newMapGroup);
                }
                catch(Exception ex)
                {
                    Logger.LogError($"{ex.Message}");
                }

                // Set current map group
                currentMapGroup = newMapGroup;
                newMapGroup.Clear();
                
            }
        }
        // Construct admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_maps", "Provides a list of maps for the current game mode.")]
        public void OnMapsCommand(CCSPlayerController? player, CommandInfo command)
        {
            Logger.LogInformation("OnMapsCommand execution started.");
            if(player != null && _plugin != null)
            {
                mapMenu.Title = Localizer["maps.hud.menu-title"];
                MenuManager.OpenCenterHtmlMenu(_plugin, player, mapMenu);
            }
        }
        // Construct admin mode menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_modes", "Provides a list of game modes.")]
        public void OnModesCommand(CCSPlayerController? player, CommandInfo command)
        {
            Logger.LogInformation("OnModesCommand execution started.");
            if(player != null && _plugin != null)
            {
                modeMenu.Title = Localizer["mode.hud.menu-title"];
                MenuManager.OpenCenterHtmlMenu(_plugin, player, modeMenu);
            }
        }
    }
}