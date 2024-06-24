// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

// Declare namespace
namespace GameModeManager
{
    // Define ServerManager class
    public static class ServerManager
    {
        // Define method calculate valid players (no bots)
        public static CCSPlayerController[] ValidPlayers(bool considerBots = false)
        {
            //considerBots = true;
            return Utilities.GetPlayers()
                .Where(x => x.ReallyValid(considerBots))
                .Where(x => !x.IsHLTV)
                .Where(x => considerBots || !x.IsBot)
                .ToArray();
        }

        // Define method get a count of valid players
        public static int ValidPlayerCount(bool considerBots = false)
        {
            return ValidPlayers(considerBots).Length;
        }

        public static bool IsHibernationEnabled()
        {
            // Check hibernation state
            var conVar = ConVar.Find("sv_hibernate_when_empty");

            if (conVar != null)
            {
                return conVar.GetPrimitiveValue<bool>(); // Returns true/false based on the ConVar value
            }
            else
            {
                return false;
            }
        }

        // Define reusable method to change map
        public static void ChangeMap(Map _nextMap)
        {
            // If map valid, change map based on map type
            if (Server.IsMapValid(_nextMap.Name))
            {
                Server.ExecuteCommand($"changelevel \"{_nextMap.Name}\"");
            }
            else if (_nextMap.WorkshopId != -1)
            {
                Server.ExecuteCommand($"host_workshop_map \"{_nextMap.WorkshopId}\"");
            }
            else
            {
                Server.ExecuteCommand($"ds_workshop_changelevel \"{_nextMap.Name}\"");
            }
        }

        // Define reusable method to change mode
        public static void ChangeMode(Mode mode, Plugin plugin, PluginState pluginState, float delay)
        {
            if (plugin != null)
            {
                // Change mode
                plugin.AddTimer(delay, () => 
                {
                    Server.ExecuteCommand($"exec {mode.Config}");
                }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

                // Set current mode
                pluginState.CurrentMode = mode; 
            }
        }
    }
}