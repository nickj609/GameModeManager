// Included libraries
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Core
{
    // Defin records
    public record VoteResult(VoteResultEnum Result, int VoteCount, int RequiredVotes);

    // Define class
    public class RTVManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private GameRules _gameRules;
        private PluginState _pluginState;
        private Config _config = new();
        private StringLocalizer _localizer;
        private ILogger<WarmupManager> _logger;

        // Define class instance
        public RTVManager(PluginState pluginState, ILogger<WarmupManager> logger, GameRules gameRules, StringLocalizer localizer)
        {
            _logger = logger;
            _localizer = localizer;
            _gameRules = gameRules;
            _pluginState = pluginState;
        }
        
        // Define variables
        private List<int> votes = new();
        private float VotePercentage = 0F;
        public int VoteCount => votes.Count;
        public bool VotesAlreadyReached { get; set; } = false;
        public Dictionary<int, List<string>> Nominations = new();
        public int RequiredVotes { get => (int)Math.Round(Extensions.ValidPlayerCount() * VotePercentage); }

        // Define on map start behavior 
        public void OnMapStart(string _mapName)
        {
            votes.Clear();
            Nominations.Clear();
            VotesAlreadyReached = false;
        }

        // Define reusable method to enable custom RTV
        public void EnableCustomRTV()
        {
            _logger.LogInformation($"Enabling custom RTV...");
            Server.ExecuteCommand($"css_plugins load {_config.CustomRTV.Plugin}");
            _pluginState.CustomRTV = true;

            _logger.LogInformation($"Disabling rotations...");
            _pluginState.RotationsEnabled = false;
        }
        // Define reusable method to disable RTV
        public void DisableCustomRTV()
        {
            _logger.LogInformation($"Disabling RTV...");
            Server.ExecuteCommand($"css_plugins unload {_config.CustomRTV.Plugin}");
            _pluginState.CustomRTV = false;

            _logger.LogInformation($"Enabling rotations...");
            _pluginState.RotationsEnabled = true;
        }

        // Define reusable method to get vote result
        public bool CheckVotes(int numberOfVotes)
        {
            return numberOfVotes > 0 && numberOfVotes >= RequiredVotes;
        }

        // Define reusable method to add vote
        public VoteResult AddVote(int userId)
        {
            if (VotesAlreadyReached)
                return new VoteResult(VoteResultEnum.VotesAlreadyReached, VoteCount, RequiredVotes);

            VoteResultEnum? result = null;
            if (votes.IndexOf(userId) != -1)
                result = VoteResultEnum.AlreadyAddedBefore;
            else
            {
                votes.Add(userId);
                result = VoteResultEnum.Added;
            }

            if (CheckVotes(votes.Count))
            {
                VotesAlreadyReached = true;
                return new VoteResult(VoteResultEnum.VotesReached, VoteCount, RequiredVotes);
            }

            return new VoteResult(result.Value, VoteCount, RequiredVotes);
        }

        // Define reusable method to remove vote
        public void RemoveVote(int userId)
        {
            var index = votes.IndexOf(userId);
            if (index > -1)
                votes.RemoveAt(index);
        }

        // Define reusable method to check if map is in cooldown
        public bool IsMapInCooldown(string map)
        {
            return _pluginState.MapsOnCoolDown.IndexOf(map) > -1;
        }

        public void OnMapsLoaded(object? sender, Map[] maps)
        {
            _pluginState.NominationMenu = new ChatMenu("Nominations");
            foreach (var map in _pluginState.Maps!.Where(x => x.Name != Server.MapName))
            {
                _pluginState.NominationMenu.AddMenuOption(map.Name, (CCSPlayerController player, ChatMenuOption option) =>
                {
                    Map map = _pluginState.Maps!.FirstOrDefault(x => x.Name == option.Text) ?? new Map(option.Text);
                    Nominate(player, map.Name);
                }, IsMapInCooldown(map.Name));
            }
        }

        public List<string> NominationWinners()
        {
            if (Nominations.Count == 0)
                return new List<string>();

            var rawNominations = Nominations
                .Select(x => x.Value)
                .Aggregate((acc, x) => acc.Concat(x).ToList());

            return rawNominations
                .Distinct()
                .Select(map => new KeyValuePair<string, int>(map, rawNominations.Count(x => x == map)))
                .OrderByDescending(x => x.Value)
                .Select(x => x.Key)
                .ToList();
        }

        public void Nominate(CCSPlayerController player, string map)
        {
            if (map == Server.MapName)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.current-map"));
                return;
            }

            if (IsMapInCooldown(map))
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.map-played-recently"));
                return;
            }

            if (_pluginState.Maps!.Select(x => x.Name).FirstOrDefault(x => x == map) is null)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.invalid-map"));
                return;

            }

            var userId = player.UserId!.Value;
            if (!Nominations.ContainsKey(userId))
                Nominations[userId] = new();

            bool alreadyVoted = Nominations[userId].IndexOf(map) != -1;
            if (!alreadyVoted)
                Nominations[userId].Add(map);

            var totalVotes = Nominations.Select(x => x.Value.Where(y => y == map).Count())
                .Sum();

            if (!alreadyVoted)
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.nominated", player.PlayerName, map, totalVotes));
            else
                player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.already-nominated", map, totalVotes));
        }
    }
}