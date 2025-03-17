// Declare namespace
namespace GameModeManager.Shared;

// Define interface
public interface IGameModeApi
{
    // Define interface properties
    string WarmupMode { get; }
    string CurrentMap { get; }
    string CurrentMode { get; }  

    // Define interface methods
    public void UpdateMapMenus();
    public void TriggerRotation();
    public void EnableTimeLimit();
    public bool isWarmupScheduled();
    public void EnableRTV(bool enabled);
    public void EnableTimeLimit(int delay);
    public void ChangeMode(string modeName);
    public bool ScheduleWarmup(string modeName);
    public void ChangeMap(string mapName, int delay);
}