// Declare namespace
namespace GameModeManager.Settings
{
    // Define enum
    // When a setting's change takes effect, from the "// applies: ..." header of its cfg
    public enum SettingApplies
    {
        Now,       // No header: straight away
        NextRound, // "next round": the game reads it when a round starts (round time, freeze time)
        Restart,   // "restart": only after the match restarts (start money). Admins are asked to restart; votes restart.
        MapChange  // "map change": only on the next map
    }

    // Define class
    public static class SettingAppliesText
    {
        public static SettingApplies Parse(string? text)
        {
            string value = (text ?? "").Trim().ToLowerInvariant();
            if (value.Contains("round"))
                return SettingApplies.NextRound;
            if (value.Contains("restart") || value.Contains("match"))
                return SettingApplies.Restart;
            if (value.Contains("map"))
                return SettingApplies.MapChange;
            return SettingApplies.Now;
        }
    }
}
