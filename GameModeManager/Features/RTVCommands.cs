// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class RTVCommands : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Plugin? _plugin;
        private RTVManager _rtvManager;
        private PluginState _pluginState;
        private Config _config = new Config();
        private ILogger<ModeCommands> _logger;
        private AsyncVoteManager _asyncVoteManager;

        // Define class instance
        public RTVCommands(PluginState pluginState, ILogger<ModeCommands> logger, RTVManager rtvManager, AsyncVoteManager asyncVoteManager)
        {
            _logger = logger;
            _rtvManager = rtvManager;
            _pluginState = pluginState;
            _asyncVoteManager = asyncVoteManager;
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

            if (_config.RTV.Enabled)
            {
                _plugin.AddCommand("css_rtv", "Rocks the Vote!", OnRTVCommand);
                _plugin.AddCommand("css_rtv_start_vote", "Starts vote.", OnRTVStartVoteCommand);
                _plugin.AddCommand("css_rtv_enabled", "Enables or disables RTV.", OnRTVEnabledCommand);
                _plugin.AddCommand("css_rtv_end_of_map_vote", "Sets end of map vote", OnRTVEndOfMapVoteCommand);
                _plugin.AddCommand("css_rtv_duration", "Sets the duration of the RTV vote.", OnRTVDurationCommand);
                _plugin.AddCommand("css_rtv_rounds_before_end", "Sets the rounds before end that the vote starts.", OnRTVRoundsBeforeEndCommand);
                _plugin.AddCommand("css_rtv_seconds_before_end", "Sets the seconds before end that the vote starts.", OnRTVSecondsBeforeEndCommand);
                _plugin.RegisterEventHandler<EventPlayerDisconnect>(PlayerDisconnected, HookMode.Pre);
            }
        }

        // Define command handlers
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(minArgs: 1, usage: "<duration>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnRTVDurationCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
            {  
                if (int.TryParse(command.ArgByIndex(1), out var duration))
                {
                    _pluginState.RTVDuration = duration;
                }
            }
            return;
        }

        [RequiresPermissions("@css/cvar")]
        [CommandHelper(minArgs: 1, usage: "<seconds>",whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnRTVSecondsBeforeEndCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
            {
                if (int.TryParse(command.ArgByIndex(1), out var seconds))
                {
                    _pluginState.RTVSecondsBeforeEnd = seconds;
                }
            }
            return;
        }

        [RequiresPermissions("@css/cvar")]
        [CommandHelper(minArgs: 1, usage: "<rounds>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnRTVRoundsBeforeEndCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
            {
                if (int.TryParse(command.ArgByIndex(1), out var rounds))
                {
                    _pluginState.RTVRoundsBeforeEnd = rounds;
                }
            }
            return;
        }

        [RequiresPermissions("@css/cvar")]
        [CommandHelper(minArgs: 1, usage: "<true|false>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnRTVEndOfMapVoteCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
            { 
                if (bool.TryParse(command.ArgByIndex(2), out var endOfMapVote))
                {
                    _pluginState.EndOfMapVote = endOfMapVote;
                }
            }
            return;
        }

        [RequiresPermissions("@css/cvar")]
        [CommandHelper(minArgs: 2, usage: "<duration> <true|false>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnRTVStartVoteCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
            { 
                if (int.TryParse(command.ArgByIndex(1), out var duration))
                {
                    _pluginState.RTVDuration = duration;
                }

                if (bool.TryParse(command.ArgByIndex(2), out var changeImmediately))
                {
                    _pluginState.ChangeImmediately = changeImmediately;
                }
                _asyncVoteManager.StartVote(null, null);
            }
            return;
        }

        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnRTVCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                _asyncVoteManager.RTVCounter(player);
            }
            return;
        }

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

        // Define event handler
        public HookResult PlayerDisconnected(EventPlayerDisconnect @event, GameEventInfo @eventInfo)
        {
            var player = @event.Userid;
            if (player?.UserId != null)
            {
                _asyncVoteManager!.RemoveVote(player.UserId.Value);
            }
            return HookResult.Continue;
        }
    }
}