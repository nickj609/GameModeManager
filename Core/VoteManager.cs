// Included libraries
using CounterStrikeSharp.API.Core;
using System.Text.RegularExpressions;

// Copyright (c) 2024 imi-tat0r
// https://github.com/imi-tat0r/CS2-CustomVotes/
using CS2_CustomVotes.Shared.Models;
// ---------------------------------------------

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin
    {
        // Define vote flags for deregistration
        bool _gamemodeVote = false;
        bool _settingVote = false;

        // Construct reusable function to register custom votes
        private void RegisterCustomVotes()
        {
            if(Config.Votes.GameMode)
            {
                // Define mode options
                var _modeOptions = new Dictionary<string, VoteOption>();
                _modeOptions.Add("No", new VoteOption("No", new List<string>()));

                // Set mode options
                if (Config.GameMode.ListEnabled)
                {
                    // Add menu option for each game mode in game mode list
                    foreach (KeyValuePair<string, string> _entry in Config.GameMode.List)
                    {
                        // Add mode to all modes vote
                        string _option=_entry.Key.ToLower();
                        _modeOptions.Add(_entry.Value, new VoteOption(_entry.Value, new List<string> { $"exec {_option}.cfg" }));

                        // Create per mode vote
                        Plugin.CustomVotesApi.Get()?.AddCustomVote(
                            _option, // Command to trigger the vote
                            new List<string>(), // Aliases for the command (optional)
                            $"Change game mode to {_entry.Value}?", // Description
                            "No", // Default
                            30, // Time to vote
                            new Dictionary<string, VoteOption> // vote options
                            {
                                { "Yes", new VoteOption("{Green}Yes", new List<string> { $"exec {_option}.cfg" })},
                                { "No", new VoteOption("{Red}No", new List<string>())},
                            },
                            "center", // Menu style  - "center" or "chat"
                            -1 // Minimum percentage of votes required (-1 behaves like 50%)
                        ); 
                    }
                }
                else
                {
                    // Add menu option for each map group
                    foreach (MapGroup _mapGroup in MapGroups)
                    {
                        // Add game mode to all game modes vote
                        var _regex = new Regex(@"^(mg_)");
                        var _match = _regex.Match(_mapGroup.Name);

                        if (_match.Success) 
                        {
                            // Create new setting name
                            string _option = _mapGroup.Name.Substring(_match.Length);
                
                            _modeOptions.Add(_mapGroup.DisplayName, new VoteOption(_mapGroup.DisplayName, new List<string> { $"exec {_option}.cfg" }));

                            // Create per mode vote
                            Plugin.CustomVotesApi.Get()?.AddCustomVote(
                                _option, // Command to trigger the vote
                                new List<string>(), // Aliases for the command (optional)
                                $"Change game mode to {_option}?", // Description
                                "No", 
                                30, // Time to vote
                                new Dictionary<string, VoteOption> // vote options
                                {
                                    { "Yes", new VoteOption("{Green}Yes", new List<string> { $"exec {_option}.cfg" })},
                                    { "No", new VoteOption("{Red}No", new List<string>())},
                                },
                                "center", // Menu style  - "center" or "chat"
                                -1 // Minimum percentage of votes required (-1 behaves like 50%)
                            ); 
                        }
                    }
                }
                
                // Register game modes vote
                Plugin.CustomVotesApi.Get()?.AddCustomVote(
                    "changemode", // Command to trigger the vote
                    new List<string> {"cm"}, // aliases for the command (optional)
                    "Vote to change game mode.", // Description
                    "No", // Default option
                    30, // Time to vote
                    _modeOptions, // All options
                    "center", // Menu style  - "center" or "chat"
                    -1 // Minimum percentage of votes required (-1 behaves like 50%)
                ); 

                // Set game mode vote flag
                _gamemodeVote = true;
            }
        
            if(Config.Votes.GameSetting)
            {
                foreach (Setting _setting in Settings)
                {

                   // Register per setting vote
                    Plugin.CustomVotesApi.Get()?.AddCustomVote(
                        _setting.Name, // Command to trigger the vote
                        new List<string>(), // Aliases for the command (optional)
                        $"Change setting {_setting.DisplayName}?", // Description
                        "No", // Default option
                        30, // Time to vote
                        new Dictionary<string, VoteOption> // vote options
                        {
                            { "No", new VoteOption("No", new List<string>())},
                            { "Enable", new VoteOption("Enable", new List<string> { $"exec {Config.Settings.Folder}/{_setting.Enable}" })},
                            { "Disable", new VoteOption("Disable", new List<string>{ $"exec {Config.Settings.Folder}/{_setting.Disable}" })},
                        },
                        "center", // Menu style  - "center" or "chat"
                        -1 // Minimum percentage of votes required (-1 behaves like 50%)
                    ); 
                }

                // Set game setting vote flag
                _settingVote = true;
            }
        }

        // Construct reusable function to deregister custom votes
        private void DeregisterCustomVotes()
        {
            if (_gamemodeVote == true)
            {
                // Deregister all gamemodes vote
                Plugin.CustomVotesApi.Get()?.RemoveCustomVote("changemode");

                // Deregister per gamemode votes
                if (Config.GameMode.ListEnabled)
                {
                    foreach (KeyValuePair<string, string> _entry in Config.GameMode.List)
                    {
                        if(_entry.Key != null)
                        {
                            string _vote=_entry.Key.ToLower();
                            Plugin.CustomVotesApi.Get()?.RemoveCustomVote(_vote);    
                        }
                    }
                }
                else
                {    
                    foreach (MapGroup _mapGroup in MapGroups)
                    {

                        if(_mapGroup.Name != null)
                        {  
                            string _vote=_mapGroup.Name.ToLower();
                            Plugin.CustomVotesApi.Get()?.RemoveCustomVote(_vote);
                        }   
                    }
                }
            }

            if (_settingVote == true)
            {
                // Deregister per settings votes
                foreach (Setting _setting in Settings)
                {
                    Plugin.CustomVotesApi.Get()?.RemoveCustomVote(_setting.Name);
                }
            }
        }
    }
}