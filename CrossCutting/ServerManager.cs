// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

// Declare namespace
namespace GameModeManager
{
    // Define ServerManager class
    public static class ServerManager
    {
        public static CCSPlayerController[] ValidPlayers(bool considerBots = false)
        {
            //considerBots = true;
            return Utilities.GetPlayers()
                .Where(x => x.ReallyValid(considerBots))
                .Where(x => !x.IsHLTV)
                .Where(x => considerBots || !x.IsBot)
                .ToArray();
        }

        public static int ValidPlayerCount(bool considerBots = false)
        {
            return ValidPlayers(considerBots).Length;
        }
    }
}