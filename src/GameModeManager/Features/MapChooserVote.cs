// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    // GG1 starts its end-of-map vote on round-limited matches by itself (WinDrawSettings);
    // this starts it for matches that end on the time limit (surf, KZ, deathmatch ...) or on arms race kills.
    public class MapChooserVote : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Plugin? _plugin;
        private GameRules _gameRules;
        private Config _config = new();
        private MapChooserBridge _mapChooser;
        private ServerManager _serverManager;
        private TimeLimitManager _timeLimitManager;

        // Define class constructor
        public MapChooserVote(GameRules gameRules, MapChooserBridge mapChooser, TimeLimitManager timeLimitManager, ServerManager serverManager)
        {
            _gameRules = gameRules;
            _mapChooser = mapChooser;
            _serverManager = serverManager;
            _timeLimitManager = timeLimitManager;
        }

        // Define class properties
        private Timer? _timer;
        private bool _started;
        private bool _autoVote = true;
        private ConVar? _gameType;
        private ConVar? _gameMode;
        private bool ArmsRace => _gameMode?.GetPrimitiveValue<int>() == 0 && _gameType?.GetPrimitiveValue<int>() == 1;

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            plugin.AddCommand("css_mapvote_auto", "Automatic end-of-map vote for this mode. Usage: css_mapvote_auto <true|false>", OnAutoVoteCommand);
            plugin.AddCommand("css_mapvote_extend", "Extend option in map votes for this mode. Usage: css_mapvote_extend <true|false>", OnExtendCommand);

            // The names 1.0.x used, which mode cfgs still run
            plugin.AddCommand("css_rtv_end_of_map_vote", "Same as css_mapvote_auto.", OnAutoVoteCommand);
            plugin.AddCommand("css_rtv_extend", "Same as css_mapvote_extend.", OnExtendCommand);
            plugin.AddCommand("css_rtv_start_vote", "Starts the end-of-map vote.", OnStartVoteCommand);

            plugin.AddCommandListener("host_workshop_map", OnWorkshopMapCommand, HookMode.Pre);
            plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
            plugin.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            plugin.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        }

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            _timer?.Kill();
            _timer = null;
            _started = false;
            _gameMode = ConVar.Find("game_mode");
            _gameType = ConVar.Find("game_type");
        }

        // Define class methods
        private void Start()
        {
            if (_started)
                return;

            _started = true;
            _mapChooser.StartMapVote();
        }

        // Define command handlers
        [CommandHelper(minArgs: 1, usage: "<true|false>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnAutoVoteCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (bool.TryParse(command.ArgByIndex(1), out bool enabled))
            {
                _autoVote = enabled;
                _mapChooser.SetRoundVote(enabled);
            }
        }

        [CommandHelper(minArgs: 1, usage: "<true|false>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnExtendCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (bool.TryParse(command.ArgByIndex(1), out bool enabled))
                _mapChooser.SetExtendInVote(enabled);
        }

        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnStartVoteCommand(CCSPlayerController? player, CommandInfo command)
        {
            _mapChooser.StartMapVote();
        }

        // Define event handlers
        // GG1 runs host_workshop_map when a vote's winner is a workshop map. A mode entry isn't one: change the mode
        // instead. GG1 has already counted down, so no second delay.
        private HookResult OnWorkshopMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            var mode = _mapChooser.FindModeEntry(command.ArgByIndex(1));
            if (mode == null)
                return HookResult.Continue;

            Server.NextFrame(() => _serverManager.ChangeMode(mode, 0));
            return HookResult.Handled;
        }

        // Clears a vote panel left on screen by a reconnect: straight away, and again in case the player's game wasn't ready yet
        private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.IsBot)
                return HookResult.Continue;

            _mapChooser.HideVotePanels(player);
            _plugin?.AddTimer(2.0f, () =>
            {
                if (player.IsValid)
                    _mapChooser.HideVotePanels(player);
            }, TimerFlags.STOP_ON_MAPCHANGE);
            return HookResult.Continue;
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            // One check per second while the map has a time limit
            if (_timer == null && _config.MapChooser.TriggerSecondsBeforeEnd > 0)
            {
                _timer = _plugin?.AddTimer(1.0f, () =>
                {
                    if (!_autoVote || _gameRules.WarmupRunning || _timeLimitManager.UnlimitedTime())
                        return;

                    var remaining = _timeLimitManager.TimeRemaining();
                    // A GG1 "extend map" result moved the end: vote again when the new end comes near
                    if (remaining > _config.MapChooser.TriggerSecondsBeforeEnd)
                        _started = false;
                    else if (remaining > 0)
                        Start();
                }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            }
            return HookResult.Continue;
        }

        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            if (!_autoVote || _started || !ArmsRace || _config.MapChooser.TriggerKillsBeforeEnd <= 0 || _gameRules.WarmupRunning)
                return HookResult.Continue;

            var leader = PlayerExtensions.ValidPlayers(true).OrderByDescending(p => p.Score).FirstOrDefault();
            if (leader?.ActionTrackingServices?.MatchStats?.Kills >= _config.MapChooser.TriggerKillsBeforeEnd)
                Server.NextFrame(Start);

            return HookResult.Continue;
        }
    }
}
