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
        private List<int> votes = new();
        private float VotePercentage = 0F;
        public int VoteCount => votes.Count;
        public bool VotesAlreadyReached { get; set; } = false;
        public int RequiredVotes { get => (int)Math.Round(Extensions.ValidPlayerCount() * VotePercentage); }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
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
            if(_pluginState.RTVEnabled)
            {
                votes.Clear();
                VotesAlreadyReached = false;
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
            if(_pluginState.NominationEnabled)
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
            if(_pluginState.NominationEnabled)
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
            {
                return new VoteResult(VoteResultEnum.VotesAlreadyReached, VoteCount, RequiredVotes);
            }

            VoteResultEnum? result = null;

            if (votes.IndexOf(userId) != -1)
            {
                result = VoteResultEnum.AlreadyAddedBefore;
            }
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

        // Define next map command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnNextMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                if (_pluginState.NextMap != null && _pluginState.NextMode == null)
                {
                    player.PrintToChat(_localizer.Localize("rtv.nextmap.message", _pluginState.NextMap.DisplayName));
                }
                else if (_pluginState.NextMap == null && _pluginState.NextMode != null)
                {
                    player.PrintToChat(_localizer.Localize("rtv.nextmap.message", "Random"));
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
                    player.PrintToChat(_localizer.Localize("rtv.nextmode.message", _pluginState.NextMode.Name));
                }
                else if (_pluginState.NextMap != null && _pluginState.NextMode == null)
                {
                    player.PrintToChat(_localizer.Localize("rtv.nextmode.message", _pluginState.CurrentMode.Name));
                }
                else
                {
                    player.PrintToChat(_localizer.Localize("general.validation.no-vote"));
                }
            }
        } 
    }
}