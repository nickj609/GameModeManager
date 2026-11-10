// Declare namespace
namespace GameModeManager.Shared.Models
{
    // Define enum
    // Whether a setting is on now, read from the server: its enable and disable cfg files compared with the ConVars and
    // plugins they change
    public enum SettingState
    {
        Unknown, // Nothing in its cfg files can be checked (only commands like say or echo)
        On,      // The server matches its enable cfg more closely than its disable cfg
        Off,     // It matches its disable cfg more closely, or, without a disable cfg, its enable cfg isn't fully in effect
        Mixed    // It matches both equally (menus show it as off)
    }
}
