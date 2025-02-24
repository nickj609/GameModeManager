// Included libraries
using GameModeManager.Menus;
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Core
{
    // Define records
    public record VoteResult(VoteResultEnum Result, int VoteCount, int RequiredVotes);

    // Define class
    public class RTVManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin? _plugin;
        private Config _config = new();
        private PlayerMenu _playerMenu;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ILogger<WarmupManager> _logger;

        // Define class instance
        public RTVManager(PluginState pluginState, ILogger<WarmupManager> logger, StringLocalizer localizer, PlayerMenu playerMenu)
        {
            _logger = logger;
            _localizer = localizer;
            _playerMenu = playerMenu;
            _pluginState = pluginState;
        }
        
        // Define variables
        private ushort InCoolDown = 0;
        private List<int> votes = new();
        private float VotePercentage = 0F;
        public int VoteCount => votes.Count;
        public bool VotesAlreadyReached { get; set; } = false;
        public Dictionary<int, List<string>> Nominations = new();
        public int RequiredVotes { get => (int)Math.Round(Extensions.ValidPlayerCount() * VotePercentage); }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
            InCoolDown = config.RTV.OptionsInCoolDown;
            VotePercentage = _config.RTV.VotePercentage / 100F;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            
            if (_pluginState.RTVEnabled)
            {
                EnableRTV();
            }
        }

        // Define on map start behavior 
        public void OnMapStart(string _mapName)
        {
            votes.Clear();
            Nominations.Clear();
            VotesAlreadyReached = false;

            var map = Server.MapName;
            if(map is not null)
            {
                if (InCoolDown == 0)
                {
                        _pluginState.OptionsOnCoolDown.Clear();
                    return;
                }

                if ( _pluginState.OptionsOnCoolDown.Count > InCoolDown)
                    _pluginState.OptionsOnCoolDown.RemoveAt(0);

                    _pluginState.OptionsOnCoolDown.Add(map.Trim().ToLower());
            }
        }

        // Define method to enable custom RTV
        public void EnableRTV()
        {
            // Enable RTV
            _pluginState.RTVEnabled = true;
            _logger.LogInformation($"Enabling RTV...");
            _plugin?.AddCommand("css_nextmap", "Displays current map.", OnNextMapCommand);
            _plugin?.AddCommand("css_nextmode", "Displays current map.", OnNextModeCommand);

            // Update player menu
            _pluginState.PlayerCommands.Add("!rtv");
            if(_config.RTV.NominationEnabled)
            {
                _pluginState.PlayerCommands.Add("!nominate");
            }
            _pluginState.PlayerCommands.Add("!nextmap");
            _pluginState.PlayerCommands.Add("!nextmode");
            _playerMenu.Load();

            // Disable rotations
            _logger.LogInformation($"Disabling rotations...");
            _pluginState.RotationsEnabled = false;
        }
        
        // Define method to disable RTV
        public void DisableRTV()
        {
            // Disable RTV
            _pluginState.RTVEnabled = false;
            _logger.LogInformation($"Disabling RTV...");
            _plugin?.RemoveCommand("css_nextmap", OnNextMapCommand);
            _plugin?.RemoveCommand("css_nextmode", OnNextModeCommand);

            // Update player menu
            _pluginState.PlayerCommands.Remove("!rtv");
            if(_config.RTV.NominationEnabled)
            {
                _pluginState.PlayerCommands.Remove("!nominate");
            }
            _pluginState.PlayerCommands.Remove("!nextmap");
            _pluginState.PlayerCommands.Remove("!nextmode");
            _playerMenu.Load();

            // Enable rotations
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
        public bool IsOptionInCooldown(string option)
        {
            return _pluginState.OptionsOnCoolDown.IndexOf(option) > -1;
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

        public void Nominate(CCSPlayerController player, string option)
        {
            if (option.Equals(Server.MapName, StringComparison.OrdinalIgnoreCase))
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.current-map"));
                return;
            }

            if (option.Equals(_pluginState.CurrentMode.Name, StringComparison.OrdinalIgnoreCase))
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.current-mode"));
                return;
            }

            if (IsOptionInCooldown(option))
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.map-played-recently"));
                return;
            }

            if (_pluginState.Maps!.Select(x => x.Name).FirstOrDefault(x => x == option) is null)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.invalid-map"));
                return;

            }

            var userId = player.UserId!.Value;
            if (!Nominations.ContainsKey(userId))
                Nominations[userId] = new();

            bool alreadyVoted = Nominations[userId].IndexOf(option) != -1;
            if (!alreadyVoted)
                Nominations[userId].Add(option);

            var totalVotes = Nominations.Select(x => x.Value.Where(y => y == option).Count()).Sum();

            if (!alreadyVoted)
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.nominated", player.PlayerName, option, totalVotes));
            else
                player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.already-nominated", option, totalVotes));
        }

        // Define next map command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnNextMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                if (_pluginState.NextMap != null && _pluginState.NextMode == null)
                {
                    player.PrintToChat(_localizer.Localize("nextmap.message", _pluginState.NextMap.DisplayName));
                }
                else if (_pluginState.NextMap == null && _pluginState.NextMode != null)
                {
                    player.PrintToChat(_localizer.Localize("nextmap.message", "Random"));
                }
                else
                {
                    player.PrintToChat(_localizer.Localize("general.validation.no-vote"));
                }
            }
        }

        // Define next mode command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnNextModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                if (_pluginState.NextMode != null)
                {
                    player.PrintToChat(_localizer.Localize("nextmode.message", _pluginState.NextMode.Name));
                }
                else if (_pluginState.NextMap != null && _pluginState.NextMode == null)
                {
                    player.PrintToChat(_localizer.Localize("nextmode.message", _pluginState.CurrentMode.Name));
                }
                else
                {
                    player.PrintToChat(_localizer.Localize("general.validation.no-vote"));
                }
            }
        } 
    }
}