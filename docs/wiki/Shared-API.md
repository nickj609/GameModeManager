GameModeManager provides a Shared API for other plugins to manage game modes. Add a reference to `GameModeManager.Shared` in your project using one of the methods below to get started!

1. Install the package using the .NET CLI: `dotnet add package GameModeManager.Shared`.
2. Download the latest release from the [releases](https://github.com/nickj609/GameModeManager/releases) page.
3. Download the source code and build it yourself.

> [!IMPORTANT]
> Version 2.0.0 targets .NET 10 and removes `IRTVApi` (`rtv:api`), because [GG1MapChooser](https://github.com/ssypchenko/GG1MapChooser) now handles RTV. Use its `ggmc:api` capability instead. `State.Maps`, `Modes`, `Settings` and `MapGroups` are now dictionaries. For .NET 8 plugins, stay on package version 1.0.63.
>
> Version 2.1.0 removes `ITimeLimitApi` (`time_limit:api`). If you need it, stay on package version 2.0.0, or [open an issue](https://github.com/nickj609/GameModeManager/issues/new/choose).

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
```
</details>

# Example Usage

```csharp
// Included libraries
using GameModeManager.Shared;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;

// Declare namespace
namespace MyPlugin;

public class MyPlugin : BasePlugin
{
    public override string ModuleName => "MyPlugin";
    public override string ModuleVersion => "1.0.0";

    private readonly PluginCapability<IGameModeApi> GameModeApi = new("game_mode:api");

    // Capabilities are ready once all plugins have loaded
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        IGameModeApi? gameModes = GameModeApi.Get();
        if (gameModes == null)
            return;

        if (gameModes.State.Maps.TryGetValue("de_dust2", out IMap? map))
            gameModes.Control.ChangeMap(map, 5);

        if (gameModes.State.Modes.TryGetValue("Casual", out IMode? mode))
            gameModes.Control.ChangeMode(mode);
    }
}
```
