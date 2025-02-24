// Included libraries
using GameModeManager.Menus;
using GameModeManager.Models;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;

// Copyright (c) 2024 imi-tat0r
// https://github.com/imi-tat0r/CS2-CustomVotes/
using CS2_CustomVotes.Shared.Models;
// ---------------------------------------------

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class CustomVoteManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Config _config = new();
        private PlayerMenu _playerMenu;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ILogger<VoteManager> _logger;

        // Define class instance
        public CustomVoteManager(PluginState pluginState, StringLocalizer localizer, RTVManager rtvManager, PlayerMenu playerMenu, ILogger<VoteManager> logger, MenuFactory menuFactory)
        {
            _logger = logger;
            _localizer = localizer;
            _playerMenu = playerMenu;
            _pluginState = pluginState;
        }

        // Define vote flags for deregistration
        private bool MapVote = false;
        private bool SettingVote = false;
        private bool GameModeVote = false;

        // Define method to register custom votes
        public void RegisterCustomVotes()
        {
            if(_config.Votes.GameModes)
            {
                // Add votes to command list
                _pluginState.PlayerCommands.Add("!changemode");

                // Define mode options
                var _modeOptions = new Dictionary<string, VoteOption>
                {
                    { "No", new VoteOption(_localizer.Localize("menu.no"), new List<string>()) }
                };

                // Add vote menu option for each game mode in game mode list
                foreach (Mode _mode in _pluginState.Modes)
                {
                    // Add mode to all modes vote
                    string _modeCommand = Extensions.RemoveCfgExtension(_mode.Config);
                    _modeOptions.Add(_mode.Name, new VoteOption(_mode.Name, new List<string> { $"css_mode {_mode.Name}"}));

                    // Create per mode vote
                    _pluginState.CustomVotesApi.Get()?.AddCustomVote(
                        _modeCommand, 
                        new List<string>(), 
                        _localizer.Localize("mode.vote.menu-title", _mode.Name), 
                        "No", 
                        30, 
                        new Dictionary<string, VoteOption> // vote options
                        {
                            { "Yes", new VoteOption(_localizer.Localize("menu.yes"), new List<string> { $"css_mode {_mode.Name}" })},
                            { "No", new VoteOption(_localizer.Localize("menu.no"), new List<string>())},
                        },
                        "center", 
                        -1 
                    ); 
                }
                
                // Register game modes vote
                _pluginState.CustomVotesApi.Get()?.AddCustomVote(
                    "changemode", 
                    new List<string> {"cm"}, 
                    _localizer.Localize("modes.vote.menu-title"), 
                    "No", 
                    30, 
                    _modeOptions, 
                    "center", 
                    -1 
                ); 

                // Set game mode vote flag
                GameModeVote = true;

                // Register map votes
                if(_config.Votes.Maps)
                {
                    RegisterMapVotes();
                    MapVote = true;
                }
            }
        
            if(_config.Votes.GameSettings)
            {
                foreach (Setting _setting in _pluginState.Settings)
                {
                    // Register per-setting vote
                    _pluginState.CustomVotesApi.Get()?.AddCustomVote(
                        _setting.Name, 
                        new List<string>(), 
                        _localizer.Localize("setting.vote.menu-title", _setting.DisplayName), 
                        "No", 
                        30, 
                        new Dictionary<string, VoteOption> 
                        {
                            { "No", new VoteOption(_localizer.Localize("menu.no"), new List<string>())},
                            { "Enable", new VoteOption(_localizer.Localize("menu.enable"), new List<string> { $"exec {_config.Settings.Folder}/{_setting.Enable}" })},
                            { "Disable", new VoteOption(_localizer.Localize("menu.disable"), new List<string>{ $"exec {_config.Settings.Folder}/{_setting.Disable}" })},
                        },
                        "center", 
                        -1 
                    ); 
                }

                // Add vote to command list
                _pluginState.PlayerCommands.Add("!changesetting");

                // Set game setting vote flag
                SettingVote = true;
            }
        }

        //Define method to register map votes
        public void RegisterMapVotes()
        {
            // Register per-map vote
            foreach (Map _map in _pluginState.CurrentMode.Maps)
            {
                _pluginState.CustomVotesApi.Get()?.AddCustomVote(
                    _map.Name, 
                    new List<string>(), 
                    _localizer.Localize("map.vote.menu-title", _map.Name), 
                    "No", 
                    30,
                    new Dictionary<string, VoteOption> 
                    {
                        { "Yes", new VoteOption(_localizer.Localize("menu.yes"), new List<string> { $"css_map {_map.Name} {_map.WorkshopId}" })},
                        { "No", new VoteOption(_localizer.Localize("menu.no"), new List<string>())},
                    },
                    "center", 
                    -1 
                ); 
            }
            _pluginState.PlayerCommands.Add("!changemap");
            _playerMenu.Load();
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
                _pluginState.PlayerCommands.Remove("!changemap");
                _playerMenu.Load();
                MapVote = false;
            }
        }

        // Define method to deregister custom votes
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