GameModeManager provides a Shared API for other plugins to manage game modes. Add a reference to `GameModeManager.Shared` in your project using one of the methods below to get started!

1. Install the package using the .NET CLI: `dotnet add package GameModeManager.Shared`.
2. Download the latest release from the [releases](https://github.com/nickj609/GameModeManager/releases) page.
3. Download the source code and build it yourself.

> [!IMPORTANT]
> Version 2.0.0 targets .NET 10 and removes `IRTVApi` (`rtv:api`), because [GG1MapChooser](https://github.com/ssypchenko/GG1MapChooser) now handles RTV. Use its `ggmc:api` capability instead. `State.Maps`, `Modes`, `Settings` and `MapGroups` are now dictionaries. For .NET 8 plugins, stay on package version 1.0.63.
>
> Version 2.1.0 removes `ITimeLimitApi` (`time_limit:api`). If you need it, stay on package version 2.0.0, or [open an issue](https://github.com/nickj609/GameModeManager/issues/new/choose).
>
> Version 2.4.0 adds events (`Events`), value settings, actions, round restore, the next map, votes and setting state. It only adds to the API, so plugins built for 2.1.0 keep working.

# Interfaces
<details>
<summary>GameModeApi (capability <code>game_mode:api</code>)</summary>

```csharp
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

    // Whether a setting is on now: On, Off, Mixed or Unknown (see the FAQ, "How do I add custom settings?")
    SettingState GetSettingState(ISetting setting);

    // Settings with a choice of values, and the value in effect now (null when it can't be told)
    IReadOnlyList<IValueSetting> ValueSettings { get; }
    string? GetValue(IValueSetting setting);

    // The cfgs in the actions folder
    IReadOnlyList<IGameAction> Actions { get; }

    string? NextMap { get; }        // The map or mode GG1MapChooser changes to next, as players see it
    bool VoteInProgress { get; }    // A GameModeManager vote, or a GG1MapChooser map vote
    bool WarmupRunning { get; }     // GameModeManager's warmup
}

public interface IGameModeControl
{
    void TriggerRotation();
    void ChangeMode(IMode mode);
    void ChangeMap(IMap map, int delay);

    bool SetNextMap(IMap map);      // False while GG1MapChooser's vote is running
    bool SetNextMode(IMode mode);

    // Players are told when a change applies (next round, after a restart, next map). One that needs a restart
    // isn't restarted: call RestartMatch when ready.
    bool SetSetting(ISetting setting, bool enable);
    bool SetValue(IValueSetting setting, string value);   // By the value's label, like "5 minutes"

    void RunAction(IGameAction action);
    bool RestoreRound(int round);   // 1 = the first round of this map; false without a backup, or in warmup
    void RestartMatch();

    // A yes/no vote by GameModeManager's rules (its Votes settings). False when it can't start now; the caller is told why.
    bool StartVote(CCSPlayerController caller, string question, Action onPassed);
}

public interface IWarmupControl
{
    bool ScheduleWarmup(IMode mode);
    void EndWarmup();
}

// Every event runs on the game thread. A handler that throws is logged and skipped.
public interface IGameModeEvents
{
    event Action<IMode, IMode>? ModeChanged;                 // Old mode, new mode
    event Action<string>? MapChanging;                       // The map's name or workshop ID
    event Action<string>? NextMapSet;                        // The next map or mode, as players see it
    event Action<ISetting, bool>? SettingChanged;            // The setting, on or off
    event Action<IValueSetting, string>? ValueSettingChanged; // The setting, the value's label
    event Action<IGameAction>? ActionRun;
    event Action<string, bool, int, int>? VoteFinished;      // Title, passed, yes, no
    event Action<int>? RoundRestored;                        // The round it went back to
    event Action<IMode>? WarmupStarted;                      // The warmup mode
    event Action? WarmupEnded;
}

public interface IGameModeApi
{
    IGameState State { get; }
    IGameModeControl Control { get; }
    IWarmupControl Warmup { get; }
    IGameModeEvents Events { get; }
}
```
</details>

# Example Usage

```csharp
// Included libraries
using GameModeManager.Shared;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;

// Declare namespace
namespace MyPlugin;

public class MyPlugin : BasePlugin
{
    public override string ModuleName => "MyPlugin";
    public override string ModuleVersion => "1.0.0";

    private readonly PluginCapability<IGameModeApi> GameModeApi = new("game_mode:api");
    private IGameModeApi? _gameModes;

    // Capabilities are ready once all plugins have loaded
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _gameModes = GameModeApi.Get();
        if (_gameModes == null)
            return;

        // React to changes instead of checking for them
        _gameModes.Events.ModeChanged += OnModeChanged;
        _gameModes.Events.SettingChanged += (setting, on) => Server.PrintToChatAll($"{setting.DisplayName} is now {(on ? "on" : "off")}");

        // Read and change things
        if (_gameModes.State.Modes.TryGetValue("Casual", out IMode? mode))
            _gameModes.Control.SetNextMode(mode);

        IValueSetting? roundTime = _gameModes.State.ValueSettings.FirstOrDefault(setting => setting.Name == "Round time");
        if (roundTime != null)
            _gameModes.Control.SetValue(roundTime, "5 minutes");
    }

    // Stop listening when this plugin unloads, so GameModeManager doesn't call into it
    public override void Unload(bool hotReload)
    {
        if (_gameModes != null)
            _gameModes.Events.ModeChanged -= OnModeChanged;
    }

    private void OnModeChanged(IMode old, IMode current)
    {
        Server.PrintToChatAll($"Mode changed from {old.Name} to {current.Name}");
    }
}
```
