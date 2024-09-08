// Included libraries
using CounterStrikeSharp.API.Core;
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Shared;

// Dedine interface
public interface IGameModeApi
{
    // Define globals
    string CurrentMode {get;} 
    string CurrentMap {get;} 
    string NextMode {get;}
    string NextMap {get;}   

    // Change mode api handler
    public void ChangeMode(string name, float delay);

    public void ChangeMode(string modeName, string mapName, float delay);

    // Change map api handler
    public void ChangeMap(string name);

    // Trigger rotation api handler
    public void TriggerRotation();

    // Trigger rotation api handler
    public void SetWarmupMode(string name);

    // Trigger rotation api handler
    public void SetWarmupTime(float time);
}