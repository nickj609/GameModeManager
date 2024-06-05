// Included libraries
using CounterStrikeSharp.API.Core;

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
                _modeOptions.Add("No", new VoteOption("No", new List<string> { "clear;" }));

                // Set mode options
                if (Config.GameMode.ListEnabled)
                {
                    // Add menu option for each game mode in game mode list
                    foreach (string _mode in Config.GameMode.List)
                    {
                        if(_mode != null)
                        {
                            // Add mode to all modes vote
                            string _option=_mode.ToLower();
                            _modeOptions.Add(_mode, new VoteOption(_mode, new List<string> { $"exec {_option}.cfg" }));

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
                                    { "No", new VoteOption("{Red}No", new List<string> { "clear;" })},
                                },
                                "center", // Menu style  - "center" or "chat"
                                -1 // Minimum percentage of votes required (-1 behaves like 50%)
                            ); 
                        }
                    }
                }
                else
                {
                    // Add menu option for each map group
                    foreach (MapGroup _mapGroup in MapGroups)
                    {
                        // Capitalize game mode name
                        string[] _nameParts = (_mapGroup.Name ?? _defaultMapGroup.Name).Split('_');
                        string _tempName = _nameParts[_nameParts.Length - 1]; 
                        string _mapGroupName = _tempName.Substring(0, 1).ToUpper() + _tempName.Substring(1); 

                        if(_mapGroupName != null)
                        {  
                             // Add game mode to all game modes vote
                            string _option=_mapGroupName.ToLower();
                            _modeOptions.Add(_mapGroupName, new VoteOption(_mapGroupName, new List<string> { $"exec {_option}.cfg" }));

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
                                    { "No", new VoteOption("{Red}No", new List<string> { "clear;" })},
                                },
                                "center", // Menu style  - "center" or "chat"
                                -1 // Minimum percentage of votes required (-1 behaves like 50%)
                            ); 
                        }

                    }
                }
                
                // Register game modes vote
                Plugin.CustomVotesApi.Get()?.AddCustomVote(
                    "gamemode", // Command to trigger the vote
                    new List<string> {"gm", "changemode", "changegame"}, // aliases for the command (optional)
                    "Vote to change game mode.", // Description
                    "No", 
                    30, // Time to vote
                    _modeOptions,
                    "center", // Menu style  - "center" or "chat"
                    -1 // Minimum percentage of votes required (-1 behaves like 50%)
                ); 

                // Set game mode vote flag
                _gamemodeVote = true;
            }
            if(Config.Votes.GameSetting)
            {
                // Define setiing options
                var _settingOptions = new Dictionary<string, VoteOption>();
                _settingOptions.Add("No", new VoteOption("No", new List<string> { "clear;" }));

                // Set setting options
                foreach (Setting _setting in Settings)
                {
                    // Add options to all game settings vote
                    _settingOptions.Add($"Enable {_setting.Name}", new VoteOption($"Enable {_setting.Name}", new List<string> { $"exec {Config.Settings.Folder}/{_setting.ConfigEnable}" }));
                    _settingOptions.Add($"Disable {_setting.Name}", new VoteOption($"Disable {_setting.Name}", new List<string> { $"exec {Config.Settings.Folder}/{_setting.ConfigDisable}" }));

                   // Register per game setting vote
                   Plugin.CustomVotesApi.Get()?.AddCustomVote(
                    _setting.Name, // Command to trigger the vote
                    new List<string>(), // Aliases for the command (optional)
                    $"Change {_setting.Name}?", // Description
                    "No", 
                    30, // Time to vote
                    new Dictionary<string, VoteOption> // vote options
                    {
                        { "Enable", new VoteOption("{Green}Enable", new List<string> { $"exec {Config.Settings.Folder}/{_setting.ConfigEnable}" })},
                        { "Disable", new VoteOption("{Red}Disable", new List<string> { $"exec {Config.Settings.Folder}/{_setting.ConfigDisable}" })},
                    },
                    "center", // Menu style  - "center" or "chat"
                    -1 // Minimum percentage of votes required (-1 behaves like 50%)
                );
                   
                }

                // Register all game settings vote
                Plugin.CustomVotesApi.Get()?.AddCustomVote(
                    "gamesetting", // Command to trigger the vote
                    new List<string> {"gs", "changesetting", "settingchange"}, // aliases for the command (optional)
                    "Vote to change a game setting.", // Description
                    "No", 
                    30, // Time to vote
                    _settingOptions,
                    "center", // Menu style  - "center" or "chat"
                    -1 // Minimum percentage of votes required (-1 behaves like 50%)
                ); 

                // Set game setting vote flag
                _settingVote = true;
            }
        }
        
        // Construct reusable function to deregister custom votes
        private void DeregisterCustomVotes()
        {
            if (_gamemodeVote == true)
            {
                Plugin.CustomVotesApi.Get()?.RemoveCustomVote("gamemode");
            }
            if (_settingVote == true)
            {
                Plugin.CustomVotesApi.Get()?.RemoveCustomVote("gamesetting");
            }
        }
    }
}