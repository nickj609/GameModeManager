// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Models;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;


// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class RockTheVoteCommand : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private GameRules _gameRules;
        private RTVManager _rtvManager;
        private MenuFactory _menuFactory;
        private PluginState _pluginState;
        private VoteManager _voteManager;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private ILogger<ModeCommands> _logger;

        // Define class instance
        public RockTheVoteCommand(PluginState pluginState, StringLocalizer localizer, MenuFactory menuFactory, ILogger<ModeCommands> logger, RTVManager rtvManager, GameRules gameRules, VoteManager voteManager)
        {
            _logger = logger;
            _localizer = localizer;
            _gameRules = gameRules;
            _rtvManager = rtvManager;
            _voteManager = voteManager;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            plugin.AddCommand("css_rtv", "", CommandHandler);
            plugin.RegisterEventHandler<EventPlayerDisconnect>(PlayerDisconnected, HookMode.Pre);

        }

        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void CommandHandler(CCSPlayerController? player, CommandInfo command)
        {
            if (player is null)
                return;

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
            else if (_config.RTV.MinRounds > 0 && _config.RTV.MinRounds > _gameRules.TotalRoundsPlayed)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-rounds", _config.RTV.MinRounds));
                return;
            }

            if (Extensions.ValidPlayerCount() < _config!.RTV.MinPlayers)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-players", _config!.RTV.MinPlayers));
                return;
            }

            VoteResult result = _rtvManager!.AddVote(player.UserId!.Value);
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
                    Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.rocked-the-vote", player.PlayerName)} {_localizer.Localize("general.votes-needed", result.VoteCount, result.RequiredVotes)}");
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.votes-reached"));
                    _voteManager.StartVote();
                    break;
            }
        }
        public HookResult PlayerDisconnected(EventPlayerDisconnect @event, GameEventInfo @eventInfo)
        {
            var player = @event.Userid;
            if (player?.UserId != null)
                _rtvManager!.RemoveVote(player.UserId.Value);

            return HookResult.Continue;
        }
    }
}