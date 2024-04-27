// Included libraries
using System.Text;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

// Copyright (c) 2024 imi-tat0r
// https://github.com/imi-tat0r/CS2-CustomVotes/
using CS2_CustomVotes.Shared.Models;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin
    {
         private void RegisterCustomVotes()
        {
            // Define mode options
            var _modeOptions = new Dictionary<string, VoteOption>();

            // Create mode options
            if (Config.GameMode.ListEnabled)
            {
                // Add menu option for each game mode in game mode list
                foreach (string _mode in Config.GameMode.List)
                {
                    if(_mode != null)
                    {
                        string _option=_mode.ToLower();
                        _modeOptions.Add(_mode, new VoteOption(_mode, new List<string> { $"exec {_option}.cfg" }));
                    }
                }
            }
            else
            {
                _modeOptions.Add("Stay", new VoteOption("Keep current game mode", new List<string> { "" }));

                // Create mode options for each map group
                foreach (MapGroup _mapGroup in MapGroups)
                {
                    // Capitalize game mode name
                    string[] _nameParts = (_mapGroup.Name ?? _defaultMapGroup.Name).Split('_');
                    string _tempName = _nameParts[_nameParts.Length - 1]; 
                    string _mapGroupName = _tempName.Substring(0, 1).ToUpper() + _tempName.Substring(1); 

                    // Add game mode to vote
                    if(_mapGroupName != null)
                    {  
                        string _option=_mapGroupName.ToLower();
                        _modeOptions.Add(_mapGroupName, new VoteOption(_mapGroupName, new List<string> { $"exec {_option}.cfg" }));
                    }
                }
            }

            // Add game modes vote
            Plugin.CustomVotesApi.Get()?.AddCustomVote(
                "gamemode", // Command to trigger the vote
                new List<string> {"gm", "changemode", "changegame"}, // aliases for the command (optional)
                "Vote to change game mode.", // Description
                "Stay", 
                30, // Time to vote
                _modeOptions,
                "center", // Menu style  - "center" or "chat"
                51 // Minimum percentage of votes required
            ); 

            // Define setiing options
            var _settingOptions = new Dictionary<string, VoteOption>();

            // Create setting options
            foreach (string _setting in Settings)
            {
                string _option = _setting.ToLower();
                _settingOptions.Add(_setting, new VoteOption(_setting, new List<string> { $"exec {Config.Settings.Folder}/{_option}.cfg" }));
            }

            // Add game settings vote
            Plugin.CustomVotesApi.Get()?.AddCustomVote(
                "gamesetting", // Command to trigger the vote
                new List<string> {"gs", "changesetting", "settingchange"}, // aliases for the command (optional)
                "Vote to change a game setting.", // Description
                "Stay", 
                30, // Time to vote
                _settingOptions,
                "center", // Menu style  - "center" or "chat"
                51 // Minimum percentage of votes required
            ); 

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
                51 // Minimum percentage of votes required
            ); 
        }
        private void DeregisterCustomVotes()
        {
            Plugin.CustomVotesApi.Get()?.RemoveCustomVote("gamemode");
            Plugin.CustomVotesApi.Get()?.RemoveCustomVote("extendmap");
            Plugin.CustomVotesApi.Get()?.RemoveCustomVote("gamesettings");
        }
    }
}