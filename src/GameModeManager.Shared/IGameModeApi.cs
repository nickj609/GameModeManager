// Included libraries
using CounterStrikeSharp.API.Core;
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Shared;

// Define interfaces
public interface IGameState
{
    IMap CurrentMap { get; }
    IMode WarmupMode { get; }
    IMode CurrentMode { get; }
    bool WarmupScheduled { get; }
    Dictionary<string, IMap> Maps { get; }
    Dictionary<string, IMode> Modes { get; }
    Dictionary<string, ISetting> Settings { get; }
    Dictionary<string, IMapGroup> MapGroups { get; }

    // Whether a setting is on now, read from the server (its cfg files compared with the ConVars and plugins they change)
    SettingState GetSettingState(ISetting setting);

    // Settings with a choice of values, and the value in effect now (null when it can't be told)
    IReadOnlyList<IValueSetting> ValueSettings { get; }
    string? GetValue(IValueSetting setting);

    // The cfgs in the actions folder
    IReadOnlyList<IGameAction> Actions { get; }

    // The map or mode GG1MapChooser changes to next, as players see it. Null when there's none yet.
    string? NextMap { get; }

    // A GameModeManager vote, or a GG1MapChooser map vote, is running
    bool VoteInProgress { get; }

    // GameModeManager's warmup is running
    bool WarmupRunning { get; }
}

public interface IGameModeControl
{
    void TriggerRotation();
    void ChangeMode(IMode mode);
    void ChangeMap(IMap map, int delay);

    // The map or mode the current one changes to at its end. False when GG1MapChooser can't take it now (a vote is running).
    bool SetNextMap(IMap map);
    bool SetNextMode(IMode mode);

    // Turns a setting on or off, or sets a value setting to one of its values (by label). Players are told, like an admin's change.
    // A setting that applies after a restart is applied, not restarted: call RestartMatch when ready.
    bool SetSetting(ISetting setting, bool enable);
    bool SetValue(IValueSetting setting, string value);

    // Runs an action's cfg
    void RunAction(IGameAction action);

    // Goes back to the start of a round of this map (1 = the first round), from the game's round backups. False when there's
    // no backup for it, or in warmup.
    bool RestoreRound(int round);

    // Restarts the match (mp_restartgame 1)
    void RestartMatch();

    // A yes/no vote by GameModeManager's rules (Votes in its config: style, percentage, duration, cooldown), started by caller.
    // onPassed runs on the game thread if it passes. False when it can't start now (votes off, another vote, cooldown); the
    // caller has been told why.
    bool StartVote(CCSPlayerController caller, string question, Action onPassed);
}

public interface IWarmupControl
{
    bool ScheduleWarmup(IMode mode);
    void EndWarmup();
}

public interface IGameModeApi
{
    IGameState State { get; }
    IGameModeControl Control { get; }
    IWarmupControl Warmup { get; }
    IGameModeEvents Events { get; }
}
