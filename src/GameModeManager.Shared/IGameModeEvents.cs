// Included libraries
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Shared;

// Define interface
// What GameModeManager tells other plugins. Every event runs on the game thread. A handler that throws is logged and skipped,
// so it can't stop GameModeManager or the other handlers.
public interface IGameModeEvents
{
    // The mode changed: the old one, then the new one. Raised when the new mode's cfg runs (css_gamemode).
    event Action<IMode, IMode>? ModeChanged;

    // GameModeManager is about to change the map: the map's name, or its workshop ID
    event Action<string>? MapChanging;

    // The next map or mode was set (by an admin, a vote, or GameModeManager): its name as players see it
    event Action<string>? NextMapSet;

    // A setting was turned on (true) or off (false) by GameModeManager: a menu, a command, a vote or this API
    event Action<ISetting, bool>? SettingChanged;

    // A value setting was set: the setting, then the value's label
    event Action<IValueSetting, string>? ValueSettingChanged;

    // An action ran
    event Action<IGameAction>? ActionRun;

    // A yes/no vote ended: its title, whether it passed, then the yes and no counts
    event Action<string, bool, int, int>? VoteFinished;

    // The match went back to the start of this round (1 = the first round)
    event Action<int>? RoundRestored;

    // GameModeManager's warmup started (its warmup mode), or ended
    event Action<IMode>? WarmupStarted;
    event Action? WarmupEnded;
}
