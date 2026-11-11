// Included libraries
using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

// Declare namespace
namespace GameModeManager.Integrations.MatchZy
{
    // Define class
    // MatchZy (Kus's competitive and practice modes load it). While a MatchZy match is live, pauses and round restores go
    // through its own commands, so its pause tracking stays right: after a restore MatchZy pauses the match and its .unpause
    // works. MatchZy runs commands from the server console as an admin's. Outside a live match GameModeManager uses the
    // game's commands, as without MatchZy.
    public static class MatchZyMatch
    {
        // Define class properties
        // MatchZy's own flag for its admin commands, so the MatchZy page asks for the same
        public const string Permission = "@css/config";

        public static bool Loaded => LoadedPlugins.IsLoaded(LoadedPlugins.MatchZy);

        // A MatchZy match is live (its isMatchLive): not warmup, practice or waiting for players to ready up
        public static bool Live => Field("isMatchLive");
        public static bool Practice => Field("isPractice");
        public static bool Veto => Field("isVeto") || Field("isPreVeto");
        public static bool KnifeRound => Field("isKnifeRequired");
        public static bool Playout => Field("isPlayOutEnabled");
        public static bool Whitelist => Field("isWhitelistRequired");

        // A command for the match, from the server console, which MatchZy takes as an admin's
        public static void Run(string command) => Server.ExecuteCommand(command);

        // A practice command, as that player: MatchZy uses where they stand (.bot) or applies it to them (.god)
        public static void RunAs(CCSPlayerController player, string command)
        {
            if (player.IsValid)
                player.ExecuteClientCommandFromServer(command);
        }

        // Define class methods
        public static void Pause() => Server.ExecuteCommand("css_forcepause");
        public static void Unpause() => Server.ExecuteCommand("css_forceunpause");

        // MatchZy names a round's backup by the rounds played before it, so round 1 is backup 0
        public static void Restore(int round) => Server.ExecuteCommand($"css_restore {Math.Max(0, round - 1)}");

        // A bool field of the loaded MatchZy plugin. False when it isn't loaded, or a MatchZy update renamed the field.
        private static bool Field(string name)
        {
            IPlugin? plugin = LoadedPlugins.Find(LoadedPlugins.MatchZy);
            if (plugin == null)
                return false;
            try
            {
                const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                return plugin.GetType().GetField(name, Any)?.GetValue(plugin) is true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
