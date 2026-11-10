// Included libraries
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Models
{
    // Define class
    // A cfg in the actions folder: something that runs once, like adding a bot or swapping teams, with no on/off state.
    // A file in a subfolder belongs to that folder's group ("bots/add_ct.cfg" is in "Bots").
    public class GameAction : IGameAction
    {
        // Define class properties
        public string File { get; }        // Relative to the cfg folder, as exec takes it: "actions/bots/add_ct.cfg"
        public string Name { get; }
        public string Group { get; }       // "" for a file directly in the actions folder
        public string Permission { get; }  // Who can run it; "" = @css/cvar
        public bool Vote { get; }          // Players without the permission can start a vote on it

        // Define class constructor
        public GameAction(string file, string name, string group, string permission, bool vote)
        {
            File = file;
            Name = name;
            Group = group;
            Permission = permission;
            Vote = vote;
        }
    }
}
