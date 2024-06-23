// Included libraries
using CounterStrikeSharp.API.Core;

// Declare namespace
namespace GameModeManager
{
    // Define class
    public static class Extensions
    {
        // Define method to check if a player is a bot
        public static bool ReallyValid(this CCSPlayerController? player, bool considerBots = false)
        {
            return player is not null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected &&
                (considerBots || (!player.IsBot && !player.IsHLTV));
        }

        // Define method to remove cfg extension from strings
        public static string RemoveCfgExtension(string str)
        {
            if (str.EndsWith(".cfg"))
            {
                return str.Substring(0, str.Length - 4);
            }
            else
            {
                return str;
            }
        }
    }
}