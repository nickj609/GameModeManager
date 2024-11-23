// Declare namespace
namespace GameModeManager.Shared;

// Dedine interface
public interface IGameModeApi
{
    // Define globals
    string WarmupMode { get; }
    string CurrentMap { get; }
    string CurrentMode { get; }  

    // Update map menus api handler
    public void UpdateMapMenus();

    // Trigger rotation api handler
    public void TriggerRotation();

    // Enable RTV compatibility api handler
    public void EnableCustomRTV(bool enabled);

    // Change mode api handlers
    public void ChangeMode(string modeName);

    // Change map api handlers
    public void ChangeMap(string mapName);
    public void ChangeMap(string mapName, int delay);

    // Schedule warmup api handlers
    public bool isWarmupScheduled();
    public bool ScheduleWarmup(string modeName);

    // Enforce time limit api handlers
    public void EnableTimeLimit();
    public void EnableTimeLimit(int delay);

}