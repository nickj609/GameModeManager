// Included libraries
using CounterStrikeSharp.API;
using System.Collections.Generic;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Core.Translations;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin
    {
        // Construct EventGameEnd Handler to automatically change map
        private HookResult EventGameEnd(EventCsIntermission @event, GameEventInfo info)
        {
            Logger.LogInformation("Game has ended. Picking random map from current map group...");
            Server.PrintToChatAll(Localizer["plugin.prefix"] + " Game has ended. Changing map...");

            if(currentMapGroup == null)
            {
                currentMapGroup = defaultMapGroup;
            }         
            // Get a random map
            Random rnd = new Random();
            int randomIndex = rnd.Next(0, currentMapGroup.Maps.Count); 
            Map randomMap = currentMapGroup.Maps[randomIndex];

            // Use the random map ID in the server command
            ChangeMap(randomMap);

            return HookResult.Continue;
        }
    }
}
