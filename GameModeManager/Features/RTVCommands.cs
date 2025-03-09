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
    public class RTVCommands : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin? _plugin;
        private GameRules _gameRules;
        private RTVManager _rtvManager;
        private PluginState _pluginState;
        private VoteManager _voteManager;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private ILogger<ModeCommands> _logger;
        private MaxRoundsManager _maxRoundsManager;
        private TimeLimitManager _timeLimitManager;

        // Define class instance
        public RTVCommands(PluginState pluginState, StringLocalizer localizer, ILogger<ModeCommands> logger, RTVManager rtvManager, GameRules gameRules, VoteManager voteManager, MaxRoundsManager maxRoundsManager, TimeLimitManager timeLimitManager)
        {
            _logger = logger;
            _localizer = localizer;
            _gameRules = gameRules;
            _rtvManager = rtvManager;
            _voteManager = voteManager;
            _pluginState = pluginState;
            _maxRoundsManager = maxRoundsManager;
            _timeLimitManager = timeLimitManager; 
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
            _pluginState.RTVDuration = _config.RTV.VoteDuration;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;

            _plugin.AddCommand("css_rtv_enabled", "Enables or disables RTV.", OnRTVEnabledCommand);

            if (_pluginState.RTVEnabled)
            {
                _plugin.AddCommand("css_rtv", "", OnRTVCommand);
                _plugin.AddCommand("css_rtv_duration", "Sets the duration of the RTV vote", OnRTVDurationCommand);
                _plugin.AddCommand("css_rtv_roundsbeforeend", "Sets the rounds before end that the vote starts", OnRTVRoundsBeforeEndCommand);
                _plugin.AddCommand("css_rtv_secondsbeforeend", "Sets the seconds before end that the vote starts", OnRTVSecondsBeforeEndCommand);
                _plugin.RegisterEventHandler<EventPlayerDisconnect>(PlayerDisconnected, HookMode.Pre);
            }
        }
        // Define client rtv command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnRTVDurationCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                return;
            }  

            if (int.TryParse(command.ArgByIndex(1), out var duration))
            {
                _pluginState.RTVDuration = duration;
            }
        }

        // Define client rtv command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnRTVSecondsBeforeEndCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                return;
            }  

            if (int.TryParse(command.ArgByIndex(1), out var seconds))
            {
                _pluginState.RTVSecondsBeforeEnd = seconds;
            }
        }

        // Define client rtv command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnRTVRoundsBeforeEndCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                return;
            }  

            if (int.TryParse(command.ArgByIndex(1), out var rounds))
            {
                _pluginState.RTVRoundsBeforeEnd = rounds;
            }
        }

        // Define client rtv command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnRTVCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player is null)
            {
                return;
            }

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
            else if (_timeLimitManager.UnlimitedTime && !_maxRoundsManager.UnlimitedRounds && _config.RTV.MinRounds > 0 && _config.RTV.MinRounds > _gameRules.TotalRoundsPlayed)
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

            VoteResult result = _rtvManager.AddVote(player.UserId!.Value);
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
                    _voteManager.StartVote(_pluginState.RTVDuration);
                    break;
            }
        }

        // Define server rtv command handler
        [CommandHelper(minArgs: 1, usage: "<true|false>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnRTVEnabledCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null && _plugin != null) 
            {
               if (command.ArgByIndex(1).Equals("true", StringComparison.OrdinalIgnoreCase) && !_pluginState.RTVEnabled)
               {
                    _plugin.AddCommand("css_rtv", "", OnRTVCommand);
                    _plugin.AddCommand("css_rtv_enabled", "Enables or disables custom RTV.", OnRTVEnabledCommand);
                    _plugin.AddCommand("css_rtv_duration", "Sets the duration of the RTV vote", OnRTVDurationCommand);
                    _plugin.AddCommand("css_rtv_roundsbeforeend", "Sets the rounds before end that the vote starts", OnRTVRoundsBeforeEndCommand);
                    _plugin.AddCommand("css_rtv_secondsbeforeend", "Sets the seconds before end that the vote starts", OnRTVSecondsBeforeEndCommand);
                    _plugin.RegisterEventHandler<EventPlayerDisconnect>(PlayerDisconnected, HookMode.Pre);
                    _rtvManager.EnableRTV();
               }
               else if (command.ArgByIndex(1).Equals("false", StringComparison.OrdinalIgnoreCase) && _pluginState.RTVEnabled)
               {
                    _plugin.RemoveCommand("css_rtv", OnRTVCommand);
                    _plugin.RemoveCommand("css_rtv_enabled", OnRTVEnabledCommand);
                    _plugin.RemoveCommand("css_rtv_duration", OnRTVDurationCommand);
                    _plugin.RemoveCommand("css_rtv_roundsbeforeend", OnRTVRoundsBeforeEndCommand);
                    _plugin.RemoveCommand("css_rtv_secondsbeforeend", OnRTVSecondsBeforeEndCommand);
                    _plugin.DeregisterEventHandler<EventPlayerDisconnect>(PlayerDisconnected, HookMode.Pre);
                    _rtvManager.DisableRTV();
               }
            }
        }

        // Define player disconnected event handler
        public HookResult PlayerDisconnected(EventPlayerDisconnect @event, GameEventInfo @eventInfo)
        {
            var player = @event.Userid;
            if (player?.UserId != null)
                _rtvManager!.RemoveVote(player.UserId.Value);

            return HookResult.Continue;
        }
    }
}