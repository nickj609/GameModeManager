// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class EndOfMapVote : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Plugin? _plugin;
        private GameRules _gameRules;
        private Config _config = new();
        private PluginState _pluginState;
        private VoteManager _voteManager;
        private TimeLimitManager _timeLimit;
        private MaxRoundsManager _maxRounds;

        // Define class instance
        public EndOfMapVote(TimeLimitManager timeLimit, MaxRoundsManager maxRounds, PluginState pluginState, GameRules gameRules, VoteManager voteManager)
        {
            _timeLimit = timeLimit;
            _maxRounds = maxRounds;
            _gameRules = gameRules;
            _pluginState = pluginState;
            _voteManager = voteManager;
        }

        // Define class properties
        private Timer? timer;
        private ConVar? gameType;
        private ConVar? gameMode;
        private bool deathMatch => gameMode?.GetPrimitiveValue<int>() == 2 && gameType?.GetPrimitiveValue<int>() == 1;

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
            _pluginState.RTVRoundsBeforeEnd = _config.RTV.TriggerRoundsBeforeEnd;
            _pluginState.RTVSecondsBeforeEnd = _config.RTV.TriggerSecondsBeforeEnd;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;

            // Register event handlers
            if(_config.RTV.Enabled)
            {
                plugin.RegisterEventHandler<EventRoundStart>(EventRoundStartHandler);
                plugin.RegisterEventHandler<EventRoundAnnounceMatchStart>(EventRoundAnnounceMatchStartHandler);
            }
        }

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            KillTimer();
            gameMode = ConVar.Find("game_mode");
            gameType = ConVar.Find("game_type");
        }

        // Define class methods
        void KillTimer()
        {
            timer?.Kill();
            timer = null;
        }

         public void StartVote()
        {
            KillTimer();
            if (_config.RTV.EndMapVote && !_pluginState.EofVoteHappened && !_pluginState.EofVoteHappening)
            {
                _voteManager.StartVote(_pluginState.RTVDuration);
            }
        }

        bool CheckTimeLeft()
        {
            return !_timeLimit.UnlimitedTime() && _timeLimit.TimeRemaining() <= _pluginState.RTVSecondsBeforeEnd;
        }

        bool CheckMaxRounds()
        {
            if (_maxRounds.UnlimitedRounds)
            {
                return false;
            }

            if (_maxRounds.RemainingRounds <= _pluginState.RTVRoundsBeforeEnd)
            {
                return true;
            }

            return _maxRounds.CanClinch && _maxRounds.RemainingWins <= _pluginState.RTVRoundsBeforeEnd;
        }

        public void StartTimer()
        {
            KillTimer();
            if (!_timeLimit.UnlimitedTime() && _config.RTV.EndMapVote)
            {
                timer = _plugin?.AddTimer(1.0F, () =>
                {
                    if (_gameRules != null && !_gameRules.WarmupRunning && !_pluginState.DisableCommands && _timeLimit.TimeRemaining() > 0)
                    {
                        if (CheckTimeLeft())
                            StartVote();
                    }
                }, TimerFlags.REPEAT);
            }
        }

        // Define class event handlers
        public HookResult EventRoundStartHandler(EventRoundStart @event, GameEventInfo info)
        {
            if(!_pluginState.EofVoteHappened && !_pluginState.EofVoteHappening)
            {
                if (!_pluginState.DisableCommands && !_gameRules.WarmupRunning && CheckMaxRounds() && _config.RTV.EndMapVote)
                {
                    StartVote();
                }
                else if (deathMatch)
                {
                    StartTimer();
                }
            }
            return HookResult.Continue;  
        }
        public HookResult EventRoundAnnounceMatchStartHandler(EventRoundAnnounceMatchStart @event, GameEventInfo info)
        {
                StartTimer();
                return HookResult.Continue;
        }
    }
}