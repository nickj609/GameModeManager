// Included libraries
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
}

public interface IGameModeControl
{
    void TriggerRotation();
    void ChangeMode(IMode mode);
    void ChangeMap(IMap map, int delay);
}

public interface IWarmupControl
{
    bool ScheduleWarmup(IMode mode);
}

public interface IGameModeApi
{
    IGameState State { get; }
    IGameModeControl Control { get; }
    IWarmupControl Warmup { get; }
}