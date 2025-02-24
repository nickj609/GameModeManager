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
        // Define dependencies
        private Timer? _timer;
        private GameRules _gameRules;
        private Config _config = new();
        private PluginState _pluginState;
        private VoteManager _voteManager;
        private TimeLimitManager _timeLimit;
        private MaxRoundsManager _maxRounds;

        // Define class properties
        private bool deathMatch => _gameMode?.GetPrimitiveValue<int>() == 2 && _gameType?.GetPrimitiveValue<int>() == 1;
        private ConVar? _gameType;
        private ConVar? _gameMode;

        // Define class instance
        public EndOfMapVote(TimeLimitManager timeLimit, MaxRoundsManager maxRounds, PluginState pluginState, GameRules gameRules, VoteManager voteManager)
        {
            _timeLimit = timeLimit;
            _maxRounds = maxRounds;
            _pluginState = pluginState;
            _gameRules = gameRules;
            _voteManager = voteManager;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _gameMode = ConVar.Find("game_mode");
            _gameType = ConVar.Find("game_type");

            void MaybeStartTimer()
            {
                KillTimer();
                if (!_timeLimit.UnlimitedTime && _config.RTV.EndMapVote)
                {
                    _timer = plugin.AddTimer(1.0F, () =>
                    {
                        if (_gameRules is not null && !_gameRules.WarmupRunning && !_pluginState.DisableCommands && _timeLimit.TimeRemaining > 0)
                        {
                            if (CheckTimeLeft())
                                StartVote();
                        }
                    }, TimerFlags.REPEAT);
                }
            }

            plugin.RegisterEventHandler<EventRoundStart>((ev, info) =>
            {

                if (!_pluginState.DisableCommands && !_gameRules.WarmupRunning && CheckMaxRounds() && _config.RTV.EndMapVote)
                {
                    StartVote();
                }
                else if (deathMatch)
                {
                    MaybeStartTimer();
                }

                return HookResult.Continue;
            });

            plugin.RegisterEventHandler<EventRoundAnnounceMatchStart>((ev, info) =>
            {
                MaybeStartTimer();
                return HookResult.Continue;
            });
        }

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            KillTimer();
        }

        // Define class methods
        bool CheckMaxRounds()
        {
            if (_maxRounds.UnlimitedRounds)
            {
                return false;
            }

            if (_maxRounds.RemainingRounds <= _config.RTV.TriggerRoundsBeforeEnd)
            {
                return true;
            }

            return _maxRounds.CanClinch && _maxRounds.RemainingWins <= _config.RTV.TriggerRoundsBeforeEnd;
        }

        bool CheckTimeLeft()
        {
            return !_timeLimit.UnlimitedTime && _timeLimit.TimeRemaining <= _config.RTV.TriggerSecondsBeforeEnd;
        }

        public void StartVote()
        {
            KillTimer();
            if (_config.RTV.EndMapVote)
            {
                _voteManager.StartVote(_config.RTV.VoteDuration);
            }
        }

        void KillTimer()
        {
            _timer?.Kill();
            _timer = null;
        }
    }
}