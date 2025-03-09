// Included libraries
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class NominateManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies

        private Config _config = new();
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ILogger<WarmupManager> _logger;

        // Define class instance
        public NominateManager(PluginState pluginState, ILogger<WarmupManager> logger, StringLocalizer localizer)
        {
            _logger = logger;
            _localizer = localizer;
            _pluginState = pluginState;
        }
        
        // Define variables
        public Dictionary<int, List<string>> MapNominations = new();
        public Dictionary<int, List<string>> ModeNominations = new();
    
        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
            _pluginState.InCoolDown = config.RTV.OptionsInCoolDown;
            _pluginState.NominationEnabled = _config.RTV.NominationEnabled;
            _pluginState.MaxNominationWinners = _config.RTV.MaxNominationWinners;
        }

        // Define on map start behavior 
        public void OnMapStart(string _mapName)
        {
            if(_pluginState.RTVEnabled)
            {
                if(_pluginState.NominationEnabled)
                {
                    MapNominations.Clear();
                    ModeNominations.Clear();

                    var map = Server.MapName;
                    if(map is not null)
                    {
                        if (_pluginState.InCoolDown == 0)
                        {
                            _pluginState.OptionsOnCoolDown.Clear();
                            return;
                        }

                        if (_pluginState.OptionsOnCoolDown.Count > _pluginState.InCoolDown)
                        {
                            _pluginState.OptionsOnCoolDown.RemoveAt(0);
                        }

                        _pluginState.OptionsOnCoolDown.Add(map.Trim().ToLower());
                    }
                }
            }
        }

         // Function to nominate a map or mode
        public void Nominate(CCSPlayerController player, string option)
        {
            // Define parameters
            Map? map = null;
            Mode? mode = null;
            var userId = player.UserId!.Value;

            // Find map or mode nominated
            if (_config.RTV.IncludeModes)
            {
                mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase));
            }

            if(_config.RTV.MapMode == 1)
            {
                map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase) || m.DisplayName.Equals(option, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                map = _pluginState.CurrentMode.Maps.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase) || m.DisplayName.Equals(option, StringComparison.OrdinalIgnoreCase));
            }

            if (map == null & mode == null)
            {
                 player.PrintToChat("Can't find map or mode.");
                 return;
            }

            // Check if option is in cooldown
            if (IsOptionInCooldown(option))
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.played-recently"));
                return;
            }

            // If mode
            if(mode != null)
            {
                // Check if mode is current mode
                if (_pluginState.CurrentMode.Name.Equals(mode.Name, StringComparison.OrdinalIgnoreCase))
                {
                    player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.current"));
                    return;
                }

                // Add nomination vote
                if (!ModeNominations.ContainsKey(userId))
                {
                    ModeNominations[userId] = new();
                }

                bool alreadyVoted = ModeNominations[userId].IndexOf(mode.Name) != -1;

                if (!alreadyVoted)
                {
                    ModeNominations[userId].Add(mode.Name);
                    var totalVotes = ModeNominations.Select(x => x.Value.Where(y => y == mode.Name).Count()).Sum();
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.nominated", player.PlayerName, mode.Name, totalVotes));
                }
                else
                {
                    var totalVotes = ModeNominations.Select(x => x.Value.Where(y => y == mode.Name).Count()).Sum();
                    player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.already-nominated", mode.Name, totalVotes));
                }
            }
            else if (map != null)
            {
                // Check if map is the current map
                if (Server.MapName.Equals(map.Name, StringComparison.OrdinalIgnoreCase))
                {
                    player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.current"));
                    return;
                }

                // Add nomination vote
                if (!MapNominations.ContainsKey(userId))
                {
                    MapNominations[userId] = new();
                }

                bool alreadyVoted = MapNominations[userId].IndexOf(map.DisplayName) != -1;

                if (!alreadyVoted)
                {
                    MapNominations[userId].Add(map.DisplayName);
                    var totalVotes = MapNominations.Select(x => x.Value.Where(y => y == map.DisplayName).Count()).Sum();
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.nominated", player.PlayerName, map.DisplayName, totalVotes));
                }
                else
                {
                    var totalVotes = MapNominations.Select(x => x.Value.Where(y => y == map.DisplayName).Count()).Sum();
                    player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.already-nominated", map.DisplayName, totalVotes));
                }
            }
        }

        // Define reusable method to check if map is in cooldown
        public bool IsOptionInCooldown(string option)
        {
            return _pluginState.OptionsOnCoolDown.IndexOf(option) > -1;
        }

        // List of map nomination winners
        public List<string> MapNominationWinners()
        {
            if (MapNominations.Count == 0)
                return new List<string>();

            var rawNominations = MapNominations
                .Select(x => x.Value)
                .Aggregate((acc, x) => acc.Concat(x).ToList());

            List<string> winners = rawNominations
                .Distinct()
                .Select(map => new KeyValuePair<string, int>(map, rawNominations.Count(x => x == map)))
                .OrderByDescending(x => x.Value)
                .Select(x => x.Key)
                .ToList();

            // Take only the top nomination(s)
            if (winners.Count > _pluginState.MaxNominationWinners)
            {
                winners = winners.Take(_pluginState.MaxNominationWinners).ToList();
            }

            return winners;
        }

        // List of mode nomination winners
        public List<string> ModeNominationWinners()
        {
            if (ModeNominations.Count == 0)
                return new List<string>();

            var rawNominations = ModeNominations
                .Select(x => x.Value)
                .Aggregate((acc, x) => acc.Concat(x).ToList());

            List<string> winners = rawNominations
                .Distinct()
                .Select(mode => new KeyValuePair<string, int>(mode, rawNominations.Count(x => x == mode)))
                .OrderByDescending(x => x.Value)
                .Select(x => x.Key)
                .ToList();

            // Take only the top nomination(s)
            if (winners.Count > _pluginState.MaxNominationWinners)
            {
                winners = winners.Take(_pluginState.MaxNominationWinners).ToList();
            }

            return winners;
        }
    }
}