// Included libraries
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Shared;

// Define interfaces
public interface IGameState
{
    List<IMap> Maps { get; }
    List<IMode> Modes { get; }
    IMap CurrentMap { get; }
    IMode WarmupMode { get; }
    IMode CurrentMode { get; }
    List<ISetting> Settings { get; }
    List<IMapGroup> MapGroups { get; }
    bool WarmupScheduled { get; }
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