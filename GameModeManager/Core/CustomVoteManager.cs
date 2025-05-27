// Included libraries
using GameModeManager.Models;
using CS2_CustomVotes.Shared;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using CS2_CustomVotes.Shared.Models;
using CounterStrikeSharp.API.Core.Capabilities;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class CustomVoteManager : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Config _config = new();
        private PluginState _pluginState;
        private StringLocalizer _localizer;

        // Define class instance
        public CustomVoteManager(PluginState pluginState, StringLocalizer localizer)
        {
            _localizer = localizer;
            _pluginState = pluginState;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define class properties
        private bool MapVote = false;
        private bool SettingVote = false;
        private bool GameModeVote = false;
        public static PluginCapability<ICustomVoteApi> CustomVotesApi { get; } = new("custom_votes:api");

        // Define method to register custom votes
        public void RegisterCustomVotes()
        {
            if(_config.Votes.GameModes)
            {
                // Add votes to command list
                _pluginState.Game.PlayerCommands.Add("!changemode");

                // Define mode options
                var _modeOptions = new Dictionary<string, VoteOption>
                {
                    { "No", new VoteOption(_localizer.Localize("menu.no"), new List<string>()) }
                };

                // Add vote menu option for each game mode in game mode list
                foreach (Mode _mode in _pluginState.Game.Modes)
                {
                    // Add mode to all modes vote
                    string _modeCommand = PluginExtensions.RemoveCfgExtension(_mode.Config);
                    _modeOptions.Add(_mode.Name, new VoteOption(_mode.Name, new List<string> { $"css_mode {_mode.Name}"}));

                    // Create per mode vote
                    CustomVotesApi.Get()?.AddCustomVote(
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
                CustomVotesApi.Get()?.AddCustomVote(
                    "changemode", 
                    new List<string> {"cm"}, 
                    _localizer.Localize("modes.vote.menu-title"), 
                    "No", 
                    30, 
                    _modeOptions, 
                    "center", 
                    -1 
                ); 
                GameModeVote = true;

                // Register map votes
                if(_config.Votes.Maps)
                {
                    RegisterMapVotes();
                    MapVote = true;
                }
            }
        
            // Register game settings
            if(_config.Votes.GameSettings)
            {
                foreach (Setting _setting in _pluginState.Game.Settings)
                {
                    CustomVotesApi.Get()?.AddCustomVote(
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
                _pluginState.Game.PlayerCommands.Add("!changesetting");
                SettingVote = true;
            }
        }

        //Define method to register map votes
        public void RegisterMapVotes()
        {
            foreach (Map _map in _pluginState.Game.CurrentMode.Maps)
            {
                CustomVotesApi.Get()?.AddCustomVote(
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
            _pluginState.Game.PlayerCommands.Add("!changemap");
            MapVote = true;
        }

        // Define method to deregister map votes
        public void DeregisterMapVotes()
        {
            if (MapVote)
            {
                foreach (Map _map in _pluginState.Game.CurrentMode.Maps)
                {
                    CustomVotesApi.Get()?.RemoveCustomVote(_map.Name);
                }

                // Remove vote from command list
                _pluginState.Game.PlayerCommands.Remove("!changemap");
                MapVote = false;
            }
        }

        // Define method to deregister custom votes
        public void DeregisterCustomVotes()
        {
            // Deregister all gamemode votes
            if (GameModeVote)
            {
                CustomVotesApi.Get()?.RemoveCustomVote("changemode");

                foreach (Mode _mode in _pluginState.Game.Modes)
                {
                    string _modeCommand = PluginExtensions.RemoveCfgExtension(_mode.Config);
                    CustomVotesApi.Get()?.RemoveCustomVote(_modeCommand);    
                }
            }

            // Deregister per-setting votes
            if (SettingVote)
            {
                foreach (Setting _setting in _pluginState.Game.Settings)
                {
                    CustomVotesApi.Get()?.RemoveCustomVote(_setting.Name);
                }
            }
            DeregisterMapVotes();
        }
    }
}