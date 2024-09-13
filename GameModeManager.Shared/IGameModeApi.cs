// Declare namespace
namespace GameModeManager.Shared;

// Dedine interface
public interface IGameModeApi
{
    // Define globals
    string WarmupTime { get; } 
    string WarmupMode { get; }
    string CurrentMap { get; }
    string CurrentMode { get; }  
    
    // Update map menus api handler
    public void UpdateMapMenus();

    // Trigger rotation api handler
    public void TriggerRotation();

    // Enable RTV compatibility api handler
    public void EnableRTV(bool enabled);

    // Change map api handlers
    public void ChangeMap(string mapName);
    public void ChangeMap(string mapName, float delay);

    // Schedule warmup api handlers
    public bool isWarmupScheduled();
    public bool ScheduleWarmup(string modeName);
    public bool ScheduleWarmup(string modeName, float delay);

    // Enforce time limit api handlers
    public void EnforceTimeLimit(bool enabled);
    public void EnforceCustomTimeLimit(bool enabled, float delay);

    // Change mode api handlers
    public void ChangeMode(string modeName);
    public void ChangeMode(string modeName, float delay);
    public void ChangeMode(string modeName, string mapName, float delay);

}