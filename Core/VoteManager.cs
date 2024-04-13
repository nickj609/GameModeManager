// Included libraries
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CS2_CustomVotes.Shared.Models;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin
    {
         private void RegisterCustomVotes()
        {
            // Define mode options
            var _options = new Dictionary<string, VoteOption>();

            // Create mode options
            if (Config.GameMode.ListEnabled)
            {
                // Add menu option for each game mode in game mode list
                foreach (string mode in Config.GameMode.List)
                {
                    if(mode != null)
                    {
                        string _mode=mode.ToLower();
                        _options.Add(mode, new VoteOption(mode, new List<string> { $"exec {_mode}.cfg" }));
                    }
                }
            }
            else
            {
                _options.Add("Stay", new VoteOption("Keep current game mode", new List<string> { "" }));

                // Create mode options for each map group
                foreach (MapGroup _mapGroup in _mapGroups)
                {
                    // Split the string into parts by the underscore
                    string[] _nameParts = (_mapGroup.Name ?? _defaultMapGroup.Name).Split('_');

                    // Get the last part (the actual map group name)
                    string _tempName = _nameParts[nameParts.Length - 1]; 

                    // Combine the capitalized first letter with the rest
                    string _mapGroupName = _tempName.Substring(0, 1).ToUpper() + _tempName.Substring(1); 

                    if(_mapGroupName != null)
                    {  
                        string _mode=_mapGroupName.ToLower();
                        _options.Add(_mapGroupName, new VoteOption(_mapGroupName, new List<string> { $"exec {_mode}.cfg" }));
                    }
                }
            }

            // Add custom game modes vote
            Plugin.CustomVotesApi.Get()?.AddCustomVote(
                "gamemode", // Command to trigger the vote
                new List<string> {"gm", "changemode", "changegame"}, // aliases for the command (optional)
                "Vote to change game mode.", // Description
                "Stay", 
                30, // Time to vote
                _options,
                "center", // Menu style  - "center" or "chat"
                51); // Minimum percentage of votes required

            // Add extend map vote
            Plugin.CustomVotesApi.Get()?.AddCustomVote(
                "extend", // Command to trigger the vote
                new List<string>{"extendmap", "mapextend", "em"}, // aliases for the command (optional)
                "Vote to extend map.", // Description
                "No", 
                30, // Time to vote
                new Dictionary<string, VoteOption> // vote options
                {
                    { "5", new VoteOption("5 minutes", new List<string> { "sv_cheats 0" })},
                    { "10", new VoteOption("10 minutes", new List<string> { "sv_cheats 0" })},
                    { "15", new VoteOption("15 minutes", new List<string> { "sv_cheats 0" })},
                    { "20", new VoteOption("20 minutes", new List<string> { "sv_cheats 0" })},
                    { "No", new VoteOption("Don't extend", new List<string> {""})}
                },
                "center", // Menu style  - "center" or "chat"
                51); // Minimum percentage of votes required
        }
        private void DeregisterCustomVotes()
        {
            Plugin.CustomVotesApi.Get()?.RemoveCustomVote("gamemode");
        }
    }
}