// Included libraries
using WASDSharedAPI;
using GameModeManager.Core;
using GameModeManager.Menus;
using CounterStrikeSharp.API;
using GameModeManager.Models;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class AsyncVoteManager : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private RTVMenus _rtvMenus;
        private GameRules _gameRules;
        private PluginState _pluginState;
        private VoteManager _voteManager;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private ILogger<ModeCommands> _logger;
        private MaxRoundsManager _maxRoundsManager;
        private TimeLimitManager _timeLimitManager;
        private VoteOptionManager _voteOptionManager;

        // Define class instance
        public AsyncVoteManager(PluginState pluginState, IStringLocalizer iLocalizer, ILogger<ModeCommands> logger, GameRules gameRules, VoteManager voteManager, MaxRoundsManager maxRoundsManager, TimeLimitManager timeLimitManager, VoteOptionManager voteOptionManager, RTVMenus rtvMenus, MenuFactory menuFactory)
        {
            _logger = logger;
            _rtvMenus = rtvMenus;
            _gameRules = gameRules;
            _voteManager = voteManager;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _maxRoundsManager = maxRoundsManager;
            _timeLimitManager = timeLimitManager; 
            _voteOptionManager = voteOptionManager;
            _localizer = new StringLocalizer(iLocalizer, "rtv.prefix");
        }

        // Define class properties
        private List<int> Votes = new();
        private float VotePercentage = 0F;
        public int VoteCount => Votes.Count;
        public bool VotesAlreadyReached { get; set; } = false;
        public int RequiredVotes { get => (int)Math.Round(Extensions.ValidPlayerCount() * VotePercentage); }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
            VotePercentage = _config.RTV.VotePercentage / 100F;
        }

        // Define on map start behavior 
        public void OnMapStart(string _mapName)
        {
            if(_pluginState.RTVEnabled)
            {
                Votes.Clear();
                VotesAlreadyReached = false;
            }
        }

        // Define method to get vote result
        public bool CheckVotes(int numberOfVotes)
        {
            return numberOfVotes > 0 && numberOfVotes >= RequiredVotes;
        }

        // Define method to add vote
        public VoteResult AddVote(int userId)
        {
            if (VotesAlreadyReached)
            {
                return new VoteResult(VoteResultEnum.VotesAlreadyReached, VoteCount, RequiredVotes);
            }

            VoteResultEnum? result;

            if (Votes.IndexOf(userId) != -1)
            {
                result = VoteResultEnum.AlreadyAddedBefore;
            }
            else
            {
                Votes.Add(userId);
                result = VoteResultEnum.Added;
            }

            if (CheckVotes(Votes.Count))
            {
                VotesAlreadyReached = true;
                return new VoteResult(VoteResultEnum.VotesReached, VoteCount, RequiredVotes);
            }

            return new VoteResult(result.Value, VoteCount, RequiredVotes);
        }

        // Define method to remove vote
        public void RemoveVote(int userId)
        {
            var index = Votes.IndexOf(userId);
            if (index > -1)
                Votes.RemoveAt(index);
        }

        // Define method to start RTV vote
        public void StartVote(VoteResult? result, CCSPlayerController? player)
        {
            // Display to chat
            if(player?.PlayerName != null && result?.VoteCount != null)
            {
                Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.rocked-the-vote", player.PlayerName)} {_localizer.Localize("general.votes-needed", result.VoteCount, result.RequiredVotes)}");
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.votes-reached"));
            }

            // Load Options
            _voteOptionManager.LoadOptions();
            _rtvMenus.Load(_voteOptionManager.ScrambleOptions());

            // Start vote
            _voteManager.StartVote(_pluginState.RTVDuration);

            // Display vote menu
            if (_config.RTV.Style.Equals("wasd", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var validPlayer in Extensions.ValidPlayers())
                {
                    IWasdMenu? menu;
                    menu = _rtvMenus.GetWasdMenu();

                    if (menu != null)
                    {
                        _menuFactory.OpenWasdMenu(validPlayer, menu);
                    }
                }
            }
            else
            {
                foreach (var validPlayer in Extensions.ValidPlayers())
                {
                    BaseMenu menu;
                    menu = _rtvMenus.GetMenu();

                    if (menu != null)
                    {
                        _menuFactory.OpenMenu(menu, validPlayer);
                    }
                }
            }
        }

        // Define method to add and count vote
        public void RTVCounter(CCSPlayerController player)
        {
            if (_pluginState.EofVoteHappened)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.schedule-change"));
                return;
            }

            if (_pluginState.DisableCommands || !_config.RTV.Enabled)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.disabled"));
                return;
            }

            if (_gameRules.WarmupRunning)
            {
                if (!_config.RTV.EnabledInWarmup)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                    return;
                }
            }
            else if (_timeLimitManager.UnlimitedTime() && !_maxRoundsManager.UnlimitedRounds && _config.RTV.MinRounds > 0 && _config.RTV.MinRounds > _gameRules.TotalRoundsPlayed)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-rounds", _config.RTV.MinRounds));
                return;
            }

            if (Extensions.ValidPlayerCount() < _config!.RTV.MinPlayers)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-players", _config!.RTV.MinPlayers));
                return;
            }

            if (_pluginState.NextMap != null & _pluginState.NextMap != null)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.disabled"));
                return;
            }

            VoteResult result = AddVote(player.UserId!.Value);
            switch (result.Result)
            {
                case VoteResultEnum.Added:
                    Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.rocked-the-vote", player.PlayerName)} {_localizer.Localize("general.votes-needed", result.VoteCount, result.RequiredVotes)}");
                    break;
                case VoteResultEnum.AlreadyAddedBefore:
                    player.PrintToChat($"{_localizer.LocalizeWithPrefix("rtv.already-rocked-the-vote")} {_localizer.Localize("general.votes-needed", result.VoteCount, result.RequiredVotes)}");
                    break;
                case VoteResultEnum.VotesAlreadyReached:
                    player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.disabled"));
                    break;
                case VoteResultEnum.VotesReached:
                    StartVote(result, player);
                    break;
            }
        }
    }
}