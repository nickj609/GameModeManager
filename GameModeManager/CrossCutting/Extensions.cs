// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Entities;

// Declare namespace
namespace GameModeManager.CrossCutting
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

        // Define method to check if hibernation is enabled
        public static bool IsHibernationEnabled()
        {
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

        // Define method to check if server is empty
        public static bool IsServerEmpty()
        {
            return ValidPlayerCount(false) == 0;
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

        // Define methods to freeze and unfreeze a player
        public static void Freeze(this CBasePlayerPawn pawn)
        {
            pawn.MoveType = MoveType_t.MOVETYPE_OBSOLETE;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 1); // obsolete
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        }

        public static void Unfreeze(this CBasePlayerPawn pawn)
        {
            pawn.MoveType = MoveType_t.MOVETYPE_WALK;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 2); // walk
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        }  

        // Define method to freeze all players
        public static void FreezePlayers()
		{
            foreach (var player in ValidPlayers(true))
            {
			    player.Pawn.Value!.Freeze();
            }
		}

        // Define method to unfreeze all players
        public static void UnfreezePlayers()
		{
            foreach (var player in ValidPlayers(true))
            {
                player.Pawn.Value!.Unfreeze();
            }
		}

        // Define method to unfreeze all players
        public static void PrintCenterTextAll(string text, int duration)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player.IsValid)
                {
                    player.PrintToCenter(text);
                }
            }
        }

        // Define method to check if a player can target another player
        public static bool CanTarget(this CCSPlayerController? controller, CCSPlayerController? target)
        {
            if (controller is null || target is null) return true;
            if (target.IsBot) return true;

            return AdminManager.CanPlayerTarget(controller, target) ||
                                    AdminManager.CanPlayerTarget(new SteamID(controller.SteamID),
                                        new SteamID(target.SteamID));
        } 

        // Define method to shuffle vote options
        public static IList<T> Shuffle<T>(Random rng, IList<T> array)
        {
            int n = array.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
            return array;
        }
    }
}