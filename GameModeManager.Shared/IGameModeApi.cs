// Declare namespace
namespace GameModeManager.Shared;

// Dedine interface
public interface IGameModeApi
{
    // Define globals
    string NextMap { get; } 
    string NextMode { get; }
    string CurrentMap { get; }
    string CurrentMode { get; }  
    
    // Trigger rotation api handler
    public void TriggerRotation();

    // Change map api handlers
    public void ChangeMap(string mapName);
    public void ChangeMap(string mapName, float delay);

    // Schedule warmup api handlers
    public void ScheduleWarmup(string modeName);
    public void ScheduleWarmup(string modeName, float delay);

    // Enforce time limit api handlers
    public void EnforceTimeLimit(bool enabled);
    public void EnforceCustomTimeLimit(bool enabled, float delay);

    // Change mode api handlers
    public void ChangeMode(string modeName);
    public void ChangeMode(string modeName, float delay);
    public void ChangeMode(string modeName, string mapName, float delay);

}