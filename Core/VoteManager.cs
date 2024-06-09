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
        bool _mapVote = false;

        // Construct reusable function to register custom votes
        private void RegisterCustomVotes()
        {
            // Check if game mode votes are enable
            if(Config.Votes.GameMode)
            {
                // Define mode options
                var _modeOptions = new Dictionary<string, VoteOption>();
                _modeOptions.Add("No", new VoteOption(Localizer["menu.no"], new List<string>()));

                // Check if using game mode list or map group list
                if (Config.GameMode.ListEnabled)
                {
                    // Add vote menu option for each game mode in game mode list
                    foreach (KeyValuePair<string, string> _entry in Config.GameMode.List)
                    {
                        // Add mode to all modes vote
                        string _option=_entry.Key.ToLower();
                        _modeOptions.Add(_entry.Value, new VoteOption(_entry.Value, new List<string> { $"exec {_option}.cfg" }));

                        // Create per mode vote
                        Plugin.CustomVotesApi.Get()?.AddCustomVote(
                            _option, // Command to trigger the vote
                            new List<string>(), // Aliases for the command (optional)
                            Localizer["mode.vote.menu-title", _entry.Value], // Description
                            "No", // Default
                            30, // Time to vote
                            new Dictionary<string, VoteOption> // vote options
                            {
                                { "Yes", new VoteOption(Localizer["menu.yes"], new List<string> { $"exec {_option}.cfg" })},
                                { "No", new VoteOption(Localizer["menu.no"], new List<string>())},
                            },
                            "center", // Menu style  - "center" or "chat"
                            -1 // Minimum percentage of votes required (-1 behaves like 50%)
                        ); 
                    }
                }
                else
                {
                    // Add vote menu option for each map group
                    foreach (MapGroup _mapGroup in MapGroups)
                    {
                        // Define regex for map group prefix
                        var _regex = new Regex(@"^(mg_)");
                        var _match = _regex.Match(_mapGroup.Name);

                        // Add game mode to all game modes vote
                        if (_match.Success) 
                        {
                            // Remove mode prefix
                            string _option = _mapGroup.Name.Substring(_match.Length);
                
                            // Add mode as vote menu option for all modes vote
                            _modeOptions.Add(_mapGroup.DisplayName, new VoteOption(_mapGroup.DisplayName, new List<string> { $"exec {_option}.cfg" }));

                            // Create per mode vote
                            Plugin.CustomVotesApi.Get()?.AddCustomVote(
                                _option, // Command to trigger the vote
                                new List<string>(), // Aliases for the command (optional)
                                Localizer["mode.vote.menu-title", _mapGroup.DisplayName], // Description
                                "No", 
                                30, // Time to vote
                                new Dictionary<string, VoteOption> // vote options
                                {
                                    { "Yes", new VoteOption(Localizer["menu.yes"], new List<string> { $"exec {_option}.cfg" })},
                                    { "No", new VoteOption(Localizer["menu.no"], new List<string>())},
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
                    Localizer["modes.vote.menu-title"], // Description
                    "No", // Default option
                    30, // Time to vote
                    _modeOptions, // All options
                    "center", // Menu style  - "center" or "chat"
                    -1 // Minimum percentage of votes required (-1 behaves like 50%)
                ); 

                // Set game mode vote flag
                _gamemodeVote = true;

                // Register map votes
                if(Config.Votes.Map == true)
                {
                    RegisterMapVotes();
                    // Set map vote flag
                    _mapVote = true;
                }
            }
        
            if(Config.Votes.GameSetting)
            {
                foreach (Setting _setting in Settings)
                {

                   // Register per setting vote
                    Plugin.CustomVotesApi.Get()?.AddCustomVote(
                        _setting.Name, // Command to trigger the vote
                        new List<string>(), // Aliases for the command (optional)
                        Localizer["setting.vote.menu-title", _setting.DisplayName], // Description
                        "No", // Default option
                        30, // Time to vote
                        new Dictionary<string, VoteOption> // vote options
                        {
                            { "No", new VoteOption(Localizer["menu.no"], new List<string>())},
                            { "Enable", new VoteOption(Localizer["menu.enable"], new List<string> { $"exec {Config.Settings.Folder}/{_setting.Enable}" })},
                            { "Disable", new VoteOption(Localizer["menu.disable"], new List<string>{ $"exec {Config.Settings.Folder}/{_setting.Disable}" })},
                        },
                        "center", // Menu style  - "center" or "chat"
                        -1 // Minimum percentage of votes required (-1 behaves like 50%)
                    ); 
                }

                // Set game setting vote flag
                _settingVote = true;
            }
        }

        private void RegisterMapVotes()
        {
            // Get maps from current map group
            List<Map> _maps = CurrentMapGroup.Maps;

            // Register per map vote
            foreach (Map _map in _maps)
            {
                Plugin.CustomVotesApi.Get()?.AddCustomVote(
                    _map.Name, // Command to trigger the vote
                    new List<string>(), // Aliases for the command (optional)
                    Localizer["map.vote.menu-title", _map.Name], // Description
                    "No", 
                    30, // Time to vote
                    new Dictionary<string, VoteOption> // vote options
                    {
                        { "Yes", new VoteOption(Localizer["menu.yes"], new List<string> { $"css_map {_map.Name} {_map.WorkshopId}" })},
                        { "No", new VoteOption(Localizer["menu.no"], new List<string>())},
                    },
                    "center", // Menu style  - "center" or "chat"
                    -1 // Minimum percentage of votes required (-1 behaves like 50%)
                ); 
            }
            // Set map vote flag
            _mapVote = true;
        }

        private void DeregisterMapVotes()
        {
            if (_mapVote == true)
            {
                // Get maps from current map group
                List<Map> _maps = CurrentMapGroup.Maps;

                // Deregister per map vote
                foreach (Map _map in _maps)
                {
                    Plugin.CustomVotesApi.Get()?.RemoveCustomVote(_map.Name);
                }
                
                // Set map vote flag
                _mapVote = false;
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

            // Deregister map votes
            DeregisterMapVotes();
        }
    }
}