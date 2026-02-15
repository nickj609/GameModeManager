// Included libraries
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;
using Microsoft.Extensions.Localization;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class NominateManager : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Config _config = new();
        private PluginState _pluginState;
        private StringLocalizer _localizer;

        // Define class constructor
        public NominateManager(PluginState pluginState, IStringLocalizer iLocalizer)
        {
            _pluginState = pluginState;
            _localizer = new StringLocalizer(iLocalizer, "rtv.prefix");
        }

        // Define class properties
        private Dictionary<VoteOption, int> _mapVoteCounts = new();
        private Dictionary<VoteOption, int> _modeVoteCounts = new();
        public Dictionary<int, List<VoteOption>> MapNominations = new();
        public Dictionary<int, List<VoteOption>> ModeNominations = new();
        
        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
            _pluginState.RTV.InCoolDown = config.RTV.OptionsInCoolDown; 
            _pluginState.RTV.NominationEnabled = _config.RTV.NominationEnabled;
            _pluginState.RTV.MaxNominationWinners = _config.RTV.MaxNominationWinners;
        }

        // Define on map start behavior 
        public void OnMapStart(string _mapName)
        {
            if(_pluginState.RTV.Enabled)
            {
                if(_pluginState.RTV.NominationEnabled)
                {
                    MapNominations.Clear();
                    ModeNominations.Clear();
                    _mapVoteCounts.Clear(); 
                    _modeVoteCounts.Clear(); 

                    var map = Server.MapName;
                    if(map is not null)
                    {
                        if (_pluginState.RTV.InCoolDown == 0)
                        {
                            _pluginState.RTV.OptionsOnCoolDown.Clear();
                            _pluginState.RTV.OptionsOnCoolDownSet.Clear();
                            return;
                        }
                        
                        // Use Queue.Count and Dequeue
                        if (_pluginState.RTV.OptionsOnCoolDown.Count >= _pluginState.RTV.InCoolDown) 
                        {
                            var removedOption = _pluginState.RTV.OptionsOnCoolDown.Dequeue();
                            _pluginState.RTV.OptionsOnCoolDownSet.Remove(removedOption);
                        }

                        if (_pluginState.Game.Maps.TryGetValue(map.Trim(), out IMap? mapOption))
                        {
                            var newOption = new VoteOption(mapOption.Name, mapOption.DisplayName, VoteOptionType.Map);
                            _pluginState.RTV.OptionsOnCoolDown.Enqueue(newOption);
                            _pluginState.RTV.OptionsOnCoolDownSet.Add(newOption);
                        }
                    }
                }
            }
        }

        // Function to nominate a map or mode
        public void Nominate(CCSPlayerController player, VoteOption option)
        {
            var userId = player.UserId!.Value;

            // Check if option is in cooldown
            if (IsOptionInCooldown(option))
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.played-recently"));
                return;
            }

            // Nominate map or mode
            if(option.Type is VoteOptionType.Mode)
            {
                if (_pluginState.Game.CurrentMode.Name.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
                {
                    player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.current"));
                    return;
                }

                if (!ModeNominations.ContainsKey(userId))
                    ModeNominations[userId] = new();

                bool alreadyVoted = ModeNominations[userId].Contains(option);
                if (!alreadyVoted)
                {
                    ModeNominations[userId].Add(option);
                    _modeVoteCounts[option] = _modeVoteCounts.GetValueOrDefault(option, 0) + 1;
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.nominated", player.PlayerName, option.Name, _modeVoteCounts[option]));
                }
                else
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.already-nominated", option.Name, _modeVoteCounts.GetValueOrDefault(option, 0)));
                }
            }
            else if (option.Type is VoteOptionType.Map)
            {
                if (Server.MapName.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
                {
                    player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.current"));
                    return;
                }

                if (!MapNominations.ContainsKey(userId))
                    MapNominations[userId] = new();

                // Use Contains instead of IndexOf
                bool alreadyVoted = MapNominations[userId].Contains(option);

                if (!alreadyVoted)
                {
                    MapNominations[userId].Add(option);
                    // Update direct vote count for O(1) totalVotes
                    _mapVoteCounts[option] = _mapVoteCounts.GetValueOrDefault(option, 0) + 1;
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.nominated", player.PlayerName, option.DisplayName, _mapVoteCounts[option]));
                }
                else
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.already-nominated", option.DisplayName, _mapVoteCounts.GetValueOrDefault(option, 0)));
                }
            }
        }

        // Define method to check if option is in cooldown
        public bool IsOptionInCooldown(VoteOption option)
        {
            return _pluginState.RTV.OptionsOnCoolDownSet.Contains(option);
        }

        // List of map nomination winners
        public List<VoteOption> MapNominationWinners()
        {
            if (MapNominations.Count == 0)
                return new List<VoteOption>();

            // Use SelectMany for more efficient flattening
            var rawNominations = MapNominations
                .SelectMany(x => x.Value); 

            // Use GroupBy to count votes efficiently in one pass (O(N))
            List<VoteOption> winners = rawNominations
                .GroupBy(option => option) // Group by the VoteOption itself
                .Select(group => new { Option = group.Key, Count = group.Count() }) // Create anonymous type with option and its total count
                .OrderByDescending(x => x.Count) // Order by the count
                .Select(x => x.Option) // Select only the VoteOption
                .ToList(); // Convert to List

            // Take only the top nomination(s)
            if (winners.Count > _pluginState.RTV.MaxNominationWinners)
                winners = winners.Take(_pluginState.RTV.MaxNominationWinners).ToList();

            return winners;
        }

        // List of mode nomination winners
        public List<VoteOption> ModeNominationWinners()
        {
            if (ModeNominations.Count == 0)
                return new List<VoteOption>();

            // Use SelectMany for more efficient flattening
            var rawNominations = ModeNominations
                .SelectMany(x => x.Value); // This is now IEnumerable<VoteOption>

            // Use GroupBy to count votes efficiently in one pass (O(N))
            List<VoteOption> winners = rawNominations
                .GroupBy(option => option) // Group by the VoteOption itself
                .Select(group => new { Option = group.Key, Count = group.Count() }) // Create anonymous type with option and its total count
                .OrderByDescending(x => x.Count) // Order by the count
                .Select(x => x.Option) // Select only the VoteOption
                .ToList(); // Convert to List

            // Take only the top nomination(s)
            if (winners.Count > _pluginState.RTV.MaxNominationWinners)
                winners = winners.Take(_pluginState.RTV.MaxNominationWinners).ToList();

            return winners;
        }
    }
}