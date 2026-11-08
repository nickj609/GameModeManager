// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory;

// Declare namespace
namespace GameModeManager.CrossCutting
{
    // Define player extensions
    public static class PlayerExtensions
    {
        // A player without a living pawn (dead, spectating, still joining) is skipped, not an error that stops the loop
        public static void FreezePlayers()
		{
            foreach (var pawn in LivingPawns())
            {
                if (pawn.MoveType != MoveType_t.MOVETYPE_OBSOLETE)
                    pawn.Freeze();
            }
		}

        public static void UnfreezePlayers()
		{
            foreach (var pawn in LivingPawns())
            {
                if (pawn.MoveType == MoveType_t.MOVETYPE_OBSOLETE)
                    pawn.Unfreeze();
            }
		}

        private static IEnumerable<CBasePlayerPawn> LivingPawns()
        {
            return ValidPlayers(true)
                .Select(player => player.Pawn.Value)
                .Where(pawn => pawn != null && pawn.IsValid && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                .Select(pawn => pawn!);
        }

        public static int ValidPlayerCount(bool considerBots = false)
        {
            return ValidPlayers(considerBots).Length;
        }

        public static void Freeze(this CBasePlayerPawn pawn)
        {
            pawn.MoveType = MoveType_t.MOVETYPE_OBSOLETE;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 1);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        }

        public static void Unfreeze(this CBasePlayerPawn pawn)
        {
            pawn.MoveType = MoveType_t.MOVETYPE_WALK;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 2);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        }

        public static CCSPlayerController[] ValidPlayers(bool considerBots = false)
        {
            return Utilities.GetPlayers()
                .Where(x => x.ReallyValid(considerBots))
                .Where(x => !x.IsHLTV)
                .Where(x => considerBots || !x.IsBot)
                .ToArray();
        }

        public static bool ReallyValid(this CCSPlayerController? player, bool considerBots = false)
        {
            return player is not null && player.IsValid && player.Connected == PlayerConnectedState.Connected &&
                (considerBots || (!player.IsBot && !player.IsHLTV));
        }
    }
        
    // Define server extensions
    public static class ServerExtensions
    {
        public static bool IsServerEmpty()
        {
            return PlayerExtensions.ValidPlayerCount(false) == 0;
        }

        public static bool IsHibernationEnabled()
        {
            var conVar = ConVar.Find("sv_hibernate_when_empty");

            if (conVar != null)
                return conVar.GetPrimitiveValue<bool>();
            else
                return false;
        }
    }
    
    // Define plugin extensions
    public static class PluginExtensions
    {
        public static string RemoveCfgExtension(string str)
        {
            if (str.EndsWith(".cfg"))
                return str.Substring(0, str.Length - 4);
            else
                return str;
        }

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