// Declare namespace
namespace GameModeManager.Shared.Models
{
    // Define interface
    // A cfg in the actions folder: something that runs once, like adding a bot or swapping teams
    public interface IGameAction
    {
        // Define interface properties
        public string Name { get; }
        public string Group { get; } // "" = the actions menu's first page
        public string File { get; }  // As exec takes it, like "actions/swap_teams.cfg"
    }
}
