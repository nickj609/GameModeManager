// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Cvars;

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

        // Define method to get a players team
        public static string GetTeamString(this CCSPlayerController? player, bool abbreviate = false)
        {
            var teamNum = player != null ? (int)player.Team : -1;
            var teamStr = teamNum switch
            {
                (int)CsTeam.None => "team.none",
                (int)CsTeam.CounterTerrorist => "team.ct",
                (int)CsTeam.Terrorist => "team.t",
                (int)CsTeam.Spectator => "team.spec",
                _ => ""
            };

            return abbreviate ? teamStr + ".short" : teamStr + ".long";
        }

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