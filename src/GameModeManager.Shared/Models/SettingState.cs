// Declare namespace
namespace GameModeManager.Shared.Models
{
    // Define enum
    // Whether a setting is on now, read from the server: its enable and disable cfg files compared with the ConVars and
    // plugins they change
    public enum SettingState
    {
        Unknown, // Nothing in its cfg files can be checked (only commands like say or echo)
        On,      // Everything its enable cfg sets is set
        Off,     // Everything its disable cfg sets is set, or, without a disable cfg, its enable cfg isn't
        Mixed    // Some of each, for example after one ConVar was changed by hand
    }
}
