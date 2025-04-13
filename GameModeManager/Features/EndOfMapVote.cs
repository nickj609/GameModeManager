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
        private TimeLimitManager _timeLimitManager;
        private MaxRoundsManager _maxRoundsManager;
        private AsyncVoteManager _asyncVoteManager;

        // Define class instance
        public EndOfMapVote(TimeLimitManager timeLimitManager, MaxRoundsManager maxRoundsManager, PluginState pluginState, GameRules gameRules, AsyncVoteManager asyncVoteManager)
        {
            _gameRules = gameRules;
            _pluginState = pluginState;
            _timeLimitManager = timeLimitManager;
            _maxRoundsManager = maxRoundsManager;
            _asyncVoteManager = asyncVoteManager;
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
            if (_pluginState.EndOfMapVote && !_pluginState.EofVoteHappened && !_pluginState.EofVoteHappening)
            {
                _asyncVoteManager.StartVote(null, null);
            }
        }

        bool CheckTimeLeft()
        {
            return !_timeLimitManager.UnlimitedTime() && _timeLimitManager.TimeRemaining() <= _pluginState.RTVSecondsBeforeEnd;
        }

        bool CheckMaxRounds()
        {
            if (_maxRoundsManager.UnlimitedRounds)
            {
                return false;
            }
            if (_maxRoundsManager.RemainingRounds <= _pluginState.RTVRoundsBeforeEnd)
            {
                return true;
            }
            return _maxRoundsManager.CanClinch && _maxRoundsManager.RemainingWins <= _pluginState.RTVRoundsBeforeEnd;
        }

        public void StartTimer()
        {
            KillTimer();
            if (!_timeLimitManager.UnlimitedTime() && _pluginState.EndOfMapVote)
            {
                timer = _plugin?.AddTimer(1.0F, () =>
                {
                    if (_gameRules != null && !_gameRules.WarmupRunning && !_pluginState.DisableCommands && _timeLimitManager.TimeRemaining() > 0)
                    {
                        if (CheckTimeLeft())
                            StartVote();
                    }
                }, TimerFlags.REPEAT);
            }
            else if (!_maxRoundsManager.UnlimitedRounds && _pluginState.EndOfMapVote)
            {
                timer = _plugin?.AddTimer(1.0F, () =>
                {
                    if (_gameRules != null && !_gameRules.WarmupRunning && !_pluginState.DisableCommands && _timeLimitManager.TimeRemaining() > 0 && _pluginState.TimeLimitCustom)
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
                if (!_pluginState.DisableCommands && !_gameRules.WarmupRunning && CheckMaxRounds() && _pluginState.EndOfMapVote)
                {
                    StartVote();
                }
                else if (deathMatch && _pluginState.EndOfMapVote)
                {
                    StartTimer();
                }
            }
            return HookResult.Continue;  
        }
        public HookResult EventRoundAnnounceMatchStartHandler(EventRoundAnnounceMatchStart @event, GameEventInfo info)
        {
            if(_pluginState.EndOfMapVote)
            {
                StartTimer();
            }
            return HookResult.Continue;
        }
    }
}