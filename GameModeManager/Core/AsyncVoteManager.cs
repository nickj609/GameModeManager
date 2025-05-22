// Included libraries
using GameModeManager.Core;
using GameModeManager.Menus;
using CounterStrikeSharp.API;
using GameModeManager.Models;
using WASDMenuAPI.Shared.Models;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
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
        private Plugin? _plugin;
        private GameRules _gameRules;
        private PluginState _pluginState;
        private VoteManager _voteManager;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private MaxRoundsManager _maxRoundsManager;
        private TimeLimitManager _timeLimitManager;
        private VoteOptionManager _voteOptionManager;

        // Define class instance
        public AsyncVoteManager(PluginState pluginState, IStringLocalizer iLocalizer, GameRules gameRules, VoteManager voteManager, MaxRoundsManager maxRoundsManager, TimeLimitManager timeLimitManager, VoteOptionManager voteOptionManager)
        {
            _gameRules = gameRules;
            _voteManager = voteManager;
            _pluginState = pluginState;
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
        public record VoteResult(VoteResultEnum Result, int VoteCount, int RequiredVotes);
        public int RequiredVotes { get => (int)Math.Round(PlayerExtensions.ValidPlayerCount() * VotePercentage); }

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
        }

        // Define on map start behavior 
        public void OnMapStart(string _mapName)
        {
            if (_pluginState.RTV.Enabled)
            {
                Votes.Clear();
                VotesAlreadyReached = false;
            }
        }

        // Define class methods
        public bool CheckVotes(int numberOfVotes)
        {
            return numberOfVotes > 0 && numberOfVotes >= RequiredVotes;
        }

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

        public void RemoveVote(int userId)
        {
            var index = Votes.IndexOf(userId);
            if (index > -1)
                Votes.RemoveAt(index);
        }

        public void StartVote(VoteResult? result, CCSPlayerController? player)
        {
            MenuFactory menuFactory = new MenuFactory(_plugin);
            RTVMenus rtvMenus = new RTVMenus(_plugin, _pluginState, _localizer, _voteManager, _config);

            if (player?.PlayerName != null && result?.VoteCount != null)
            {
                Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.rocked-the-vote", player.PlayerName)} {_localizer.Localize("general.votes-needed", result.VoteCount, result.RequiredVotes)}");
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.votes-reached"));
            }

            // Start vote
            _voteOptionManager.LoadOptions();
            _voteManager.StartVote(_pluginState.RTV.Duration);

            // Display vote menu
            if (_config.RTV.Style.Equals("wasd", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var validPlayer in PlayerExtensions.ValidPlayers())
                {
                    rtvMenus.WasdMenus.Load(_voteOptionManager.ScrambleOptions());
                    IWasdMenu? menu = rtvMenus.WasdMenus.MainMenu;

                    if (menu != null)
                    {
                        menuFactory.WasdMenus.OpenMenu(validPlayer, menu);
                    }
                }
            }
            else
            {
                foreach (var validPlayer in PlayerExtensions.ValidPlayers())
                {
                    rtvMenus.BaseMenus.Load(_voteOptionManager.ScrambleOptions());
                    BaseMenu menu = rtvMenus.BaseMenus.MainMenu;

                    if (menu != null)
                    {
                        menuFactory.BaseMenus.OpenMenu(menu, validPlayer);
                    }
                }
            }
        }

        public void RTVCounter(CCSPlayerController player)
        {
            if (_pluginState.RTV.EofVoteHappened)
            {
                if (!_timeLimitManager.UnlimitedTime())
                {
                    string timeleft = _voteManager.GetTimeLeft();
                    player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "rtv.schedule-change", timeleft));
                }
                else if (!_maxRoundsManager.UnlimitedRounds)
                {
                    string roundsleft = _voteManager.GetRoundsLeft();
                    player.PrintToChat(_localizer.LocalizeWithPrefixInternal("rtv.prefix", "rtv.schedule-change", roundsleft));
                }
                return;
            }

            if (_pluginState.RTV.DisableCommands || !_config.RTV.Enabled)
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

            if (PlayerExtensions.ValidPlayerCount() < _config!.RTV.MinPlayers)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-players", _config!.RTV.MinPlayers));
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