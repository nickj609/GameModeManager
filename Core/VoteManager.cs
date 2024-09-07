// Copyright (c) 2024 imi-tat0r
// https://github.com/imi-tat0r/CS2-CustomVotes/
using CS2_CustomVotes.Shared.Models;
// ---------------------------------------------

// Declare namespace
namespace GameModeManager
{
    public class VoteManager : IPluginDependency<Plugin, Config>
    {

        // Define vote flags for deregistration
        private bool MapVote = false;
        private bool SettingVote = false;
        private bool GameModeVote = false;

        // Define dependencies
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;
        private Config _config = new Config();

        // Define class instance
        public VoteManager(PluginState pluginState, MenuFactory menuFactory, StringLocalizer localizer)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Load dependencies
        public void OnLoad(Plugin plugin)
        { 
            _localizer = new StringLocalizer(plugin.Localizer);
        }

        // Define reusable method to register custom votes
        public void RegisterCustomVotes()
        {
            if(_config != null)
            {
                // Check if game mode votes are enable
                if(_config.Votes.GameModes)
                {
                    // Add votes to command list
                    _pluginState.PlayerCommands.Add("!changemode");
                    _pluginState.PlayerCommands.Add("!showmodes");

                    // Define mode options
                    var _modeOptions = new Dictionary<string, VoteOption>();
                    _modeOptions.Add("No", new VoteOption(_localizer.Localize("menu.no"), new List<string>()));

                    // Add vote menu option for each game mode in game mode list
                    foreach (Mode _mode in _pluginState.Modes)
                    {
                        // Add mode to all modes vote
                        string _modeCommand = Extensions.RemoveCfgExtension(_mode.Config);
                        _modeOptions.Add(_mode.Name, new VoteOption(_mode.Name, new List<string> { $"exec {_mode.Config}; css_gamemode {_mode.Name}" }));

                        // Create per mode vote
                        _pluginState.CustomVotesApi.Get()?.AddCustomVote(
                            _modeCommand, // Command to trigger the vote
                            new List<string>(), // Aliases for the command (optional)
                            _localizer.Localize("mode.vote.menu-title", _mode.Name), // Description
                            "No", // Default
                            30, // Time to vote
                            new Dictionary<string, VoteOption> // vote options
                            {
                                { "Yes", new VoteOption(_localizer.Localize("menu.yes"), new List<string> { $"exec {_mode.Config}; css_gamemode {_mode.Name}" })},
                                { "No", new VoteOption(_localizer.Localize("menu.no"), new List<string>())},
                            },
                            "center", // Menu style  - "center" or "chat"
                            -1 // Minimum percentage of votes required (-1 behaves like 50%)
                        ); 
                    }
                    
                    // Register game modes vote
                    _pluginState.CustomVotesApi.Get()?.AddCustomVote(
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
                    if(_config.Votes.Maps)
                    {
                        RegisterMapVotes();

                        // Set map vote flag
                        MapVote = true;
                    }
                }
            
                if(_config.Votes.GameSettings)
                {
                    foreach (Setting _setting in _pluginState.Settings)
                    {
                        // Register per-setting vote
                        _pluginState.CustomVotesApi.Get()?.AddCustomVote(
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
                    _pluginState.PlayerCommands.Add("!showsettings");

                    // Set game setting vote flag
                    SettingVote = true;
                }
            }
        }

        //Define method to register map votes
        public void RegisterMapVotes()
        {
            // Register per-map vote
            foreach (Map _map in _pluginState.CurrentMode.Maps)
            {
                _pluginState.CustomVotesApi.Get()?.AddCustomVote(
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
            _pluginState.PlayerCommands.Add("!showmaps");

            // Update game menu
            _menuFactory.UpdateGameMenu();

            // Set map vote flag
            MapVote = true;
        }

        // Define method to deregister map votes
        public void DeregisterMapVotes()
        {
            if (MapVote)
            {
                // Deregister per-map votes
                foreach (Map _map in _pluginState.CurrentMode.Maps)
                {
                    _pluginState.CustomVotesApi.Get()?.RemoveCustomVote(_map.Name);
                }
                
                // Remove vote from command list
                _pluginState.PlayerCommands.Remove("!showmaps");

                // Update game menu
                _menuFactory.UpdateGameMenu();

                // Set map vote flag
                MapVote = false;
            }
        }

        // Define reusable method to deregister custom votes
        public void DeregisterCustomVotes()
        {
            // Deregister all gamemode votes
            if (GameModeVote)
            {
                _pluginState.CustomVotesApi.Get()?.RemoveCustomVote("changemode");

                foreach (Mode _mode in _pluginState.Modes)
                {
                    string _modeCommand = Extensions.RemoveCfgExtension(_mode.Config);
                    _pluginState.CustomVotesApi.Get()?.RemoveCustomVote(_modeCommand);    
                }
            }

            // Deregister per-setting votes
            if (SettingVote)
            {
                foreach (Setting _setting in _pluginState.Settings)
                {
                    _pluginState.CustomVotesApi.Get()?.RemoveCustomVote(_setting.Name);
                }
            }

            // Deregister per-map votes
            DeregisterMapVotes();
        }
    }
}