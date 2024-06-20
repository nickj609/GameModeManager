// Included libraries
using System.Text.RegularExpressions;

// Copyright (c) 2024 imi-tat0r
// https://github.com/imi-tat0r/CS2-CustomVotes/
using CS2_CustomVotes.Shared.Models;
// ---------------------------------------------

// Declare namespace
namespace GameModeManager
{
    public class VoteManager
    {

        // Define vote flags for deregistration
        private static bool MapVote = false;
        private static bool SettingVote = false;
        private static bool GameModeVote = false;

        // Define dependencies
        private static Config? _config;
        private static StringLocalizer? _localizer;

        // Load dependencies
        public void OnLoad(Plugin plugin)
        { 
            _localizer = new StringLocalizer(plugin.Localizer);
        }
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Construct reusable function to register custom votes
        public static void RegisterCustomVotes()
        {
            if(_config != null && _localizer != null)
            {
                // Check if game mode votes are enable
                if(_config.Votes.GameMode)
                {
                    // Add votes to command list
                    PluginState.Commands.Add("!changemode");
                    PluginState.Commands.Add("!showmodes");

                    // Define mode options
                    var _modeOptions = new Dictionary<string, VoteOption>();
                    _modeOptions.Add("No", new VoteOption(_localizer.Localize("menu.no"), new List<string>()));

                    // Check if using game mode list or map group list
                    if (_config.GameMode.ListEnabled)
                    {
                        // Add vote menu option for each game mode in game mode list
                        foreach (KeyValuePair<string, string> _entry in _config.GameMode.List)
                        {
                            // Add mode to all modes vote
                            string _option=_entry.Key.ToLower();
                            _modeOptions.Add(_entry.Value, new VoteOption(_entry.Value, new List<string> { $"exec {_option}.cfg" }));

                            // Create per mode vote
                            Plugin.CustomVotesApi.Get()?.AddCustomVote(
                                _option, // Command to trigger the vote
                                new List<string>(), // Aliases for the command (optional)
                                _localizer.Localize("mode.vote.menu-title", _entry.Value), // Description
                                "No", // Default
                                30, // Time to vote
                                new Dictionary<string, VoteOption> // vote options
                                {
                                    { "Yes", new VoteOption(_localizer.Localize("menu.yes"), new List<string> { $"exec {_option}.cfg" })},
                                    { "No", new VoteOption(_localizer.Localize("menu.no"), new List<string>())},
                                },
                                "center", // Menu style  - "center" or "chat"
                                -1 // Minimum percentage of votes required (-1 behaves like 50%)
                            ); 
                        }
                    }
                    else
                    {
                        // Add vote menu option for each map group
                        foreach (MapGroup _mapGroup in PluginState.MapGroups)
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
                                    _localizer.Localize("mode.vote.menu-title", _mapGroup.DisplayName), // Description
                                    "No", 
                                    30, // Time to vote
                                    new Dictionary<string, VoteOption> // vote options
                                    {
                                        { "Yes", new VoteOption(_localizer.Localize("menu.yes"), new List<string> { $"exec {_option}.cfg" })},
                                        { "No", new VoteOption(_localizer.Localize("menu.no"), new List<string>())},
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
                        _localizer.Localize("modes.vote.menu-title"), // Description
                        "No", // Default option
                        30, // Time to vote
                        _modeOptions, // All options
                        "center", // Menu style  - "center" or "chat"
                        -1 // Minimum percentage of votes required (-1 behaves like 50%)
                    ); 

                    // Set game mode vote flag
                    GameModeVote = true;

                    // Register map votes
                    if(_config.Votes.Map == true)
                    {
                        RegisterMapVotes();
                        // Set map vote flag
                        MapVote = true;
                    }
                }
            
            
                if(_config.Votes.GameSetting)
                {
                    foreach (Setting _setting in SettingsManager.Settings)
                    {

                    // Register per setting vote
                        Plugin.CustomVotesApi.Get()?.AddCustomVote(
                            _setting.Name, // Command to trigger the vote
                            new List<string>(), // Aliases for the command (optional)
                            _localizer.Localize("setting.vote.menu-title", _setting.DisplayName), // Description
                            "No", // Default option
                            30, // Time to vote
                            new Dictionary<string, VoteOption> // vote options
                            {
                                { "No", new VoteOption(_localizer.Localize("menu.no"), new List<string>())},
                                { "Enable", new VoteOption(_localizer.Localize("menu.enable"), new List<string> { $"exec {_config.Settings.Folder}/{_setting.Enable}" })},
                                { "Disable", new VoteOption(_localizer.Localize("menu.disable"), new List<string>{ $"exec {_config.Settings.Folder}/{_setting.Disable}" })},
                            },
                            "center", // Menu style  - "center" or "chat"
                            -1 // Minimum percentage of votes required (-1 behaves like 50%)
                        ); 
                    }

                    // Add vote to command list
                    PluginState.Commands.Add("!showsettings");

                    // Set game setting vote flag
                    SettingVote = true;
                }
            }
        }

        public static void RegisterMapVotes()
        {
            if(_config != null && _localizer != null && PluginState.CurrentMapGroup != null)
            {
                // Get maps from current map group
                List<Map> _maps = PluginState.CurrentMapGroup.Maps;

                // Register per map vote
                foreach (Map _map in _maps)
                {
                    Plugin.CustomVotesApi.Get()?.AddCustomVote(
                        _map.Name, // Command to trigger the vote
                        new List<string>(), // Aliases for the command (optional)
                        _localizer.Localize("map.vote.menu-title", _map.Name), // Description
                        "No", 
                        30, // Time to vote
                        new Dictionary<string, VoteOption> // vote options
                        {
                            { "Yes", new VoteOption(_localizer.Localize("menu.yes"), new List<string> { $"css_map {_map.Name} {_map.WorkshopId}" })},
                            { "No", new VoteOption(_localizer.Localize("menu.no"), new List<string>())},
                        },
                        "center", // Menu style  - "center" or "chat"
                        -1 // Minimum percentage of votes required (-1 behaves like 50%)
                    ); 
                }

                // Add vote to command list
                PluginState.Commands.Add("!showmaps");

                // Update game menu
                MenuFactory.UpdateGameMenu();

                // Set map vote flag
                MapVote = true;
            }
        }

        public static void DeregisterMapVotes()
        {
            if (MapVote == true && PluginState.CurrentMapGroup != null)
            {
                // Get maps from current map group
                List<Map> _maps = PluginState.CurrentMapGroup.Maps;

                // Deregister per map vote
                foreach (Map _map in _maps)
                {
                    Plugin.CustomVotesApi.Get()?.RemoveCustomVote(_map.Name);
                }
                
                // Remove vote from command list
                PluginState.Commands.Remove("!showmaps");

                // Update game menu
                MenuFactory.UpdateGameMenu();

                // Set map vote flag
                MapVote = false;
            }
        }

        // Construct reusable function to deregister custom votes
        public static void DeregisterCustomVotes()
        {
            if (GameModeVote == true && _config != null)
            {
                // Deregister all gamemodes vote
                Plugin.CustomVotesApi.Get()?.RemoveCustomVote("changemode");

                // Deregister per gamemode votes
                if (_config.GameMode.ListEnabled)
                {
                    foreach (KeyValuePair<string, string> _entry in _config.GameMode.List)
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
                    foreach (MapGroup _mapGroup in PluginState.MapGroups)
                    {

                        if(_mapGroup.Name != null)
                        {  
                            string _vote=_mapGroup.Name.ToLower();
                            Plugin.CustomVotesApi.Get()?.RemoveCustomVote(_vote);
                        }   
                    }
                }
            }

            if (SettingVote == true)
            {
                // Deregister per settings votes
                foreach (Setting _setting in SettingsManager.Settings)
                {
                    Plugin.CustomVotesApi.Get()?.RemoveCustomVote(_setting.Name);
                }
            }

            // Deregister map votes
            DeregisterMapVotes();
        }
    }
}