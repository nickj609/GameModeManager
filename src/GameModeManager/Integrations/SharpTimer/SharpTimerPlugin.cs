// Included libraries
using CounterStrikeSharp.API;

// Declare namespace
namespace GameModeManager.Integrations.SharpTimer
{
    // Define class
    // SharpTimer (Kus's surf, bhop, kz and course modes load it). It has its own !mode, for its movement modes, and a HUD it
    // redraws in the middle of the screen many times a second.
    public static class SharpTimerPlugin
    {
        // Define class properties
        public const string Name = "SharpTimer";
        public static bool Loaded => LoadedPlugins.IsLoaded(Name);

        // Its HUD parts, as its own ConVars (sharptimer_enable_*_hud)
        private static readonly string[] HudConVars =
        {
            "sharptimer_enable_timer_hud", "sharptimer_enable_velocity_hud", "sharptimer_enable_strafesync_hud",
            "sharptimer_enable_keys_hud", "sharptimer_enable_map_name_hud", "sharptimer_enable_map_tier_hud",
            "sharptimer_enable_map_type_hud", "sharptimer_enable_rankicons_hud"
        };

        // Define class methods
        // For the map change countdown, which would sit on top of the HUD. The map is about to change, and SharpTimer runs its
        // config again on the next map, so nothing needs turning back on.
        public static void HideHud()
        {
            if (Loaded)
                Server.ExecuteCommand(string.Join("; ", HudConVars.Select(name => $"{name} 0")));
        }
    }
}
