// Included libraries
using CounterStrikeSharp.API.Core;

// Declare namespace
namespace GameModeManager
{
    // Define Extensions class
    public static class Extensions
    {
        public static bool ReallyValid(this CCSPlayerController? player, bool considerBots = false)
        {
            return player is not null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected &&
                (considerBots || (!player.IsBot && !player.IsHLTV));
        }
    }
}