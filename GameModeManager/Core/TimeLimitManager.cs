// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Cvars;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class TimeLimitManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Timer? _timer;
        private ConVar? _timeLimit;
        private GameRules _gameRules;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;
        private MaxRoundsManager _maxRoundsManager;
        public decimal TimeLimitValue => (decimal)(_timeLimit?.GetPrimitiveValue<float>() ?? 0F) * 60M;
        public bool UnlimitedTime => TimeLimitValue <= 0;

        // Calculate time played
        public decimal TimePlayed
        {
            get
            {
                if (_gameRules.WarmupRunning)
                    return 0;

                return (decimal)(Server.CurrentTime - _gameRules.GameStartTime);
            }
        }

        // Calculate time remaining
        public decimal TimeRemaining
        {
            get
            {
                if (UnlimitedTime || TimePlayed > TimeLimitValue)
                    return 0;

                return TimeLimitValue - TimePlayed;
            }
        }

        // Define class instance
        public TimeLimitManager(GameRules gameRules, ServerManager serverManager, PluginState pluginState, MaxRoundsManager maxRoundsManager, IStringLocalizer iLocalizer)
        {
            _gameRules = gameRules;
            _pluginState = pluginState;
            _serverManager = serverManager;
            _maxRoundsManager = maxRoundsManager;
            _localizer = new StringLocalizer(iLocalizer, "timeleft.prefix");

        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            // Load convars
            LoadCvar();

            // Register event handlers
            plugin.RegisterEventHandler<EventGameEnd>(EventGameEndHandler);
            plugin.RegisterEventHandler<EventPlayerDisconnect>(EventPlayerDisconnectHandler);
            plugin.RegisterEventHandler<EventRoundAnnounceMatchStart>(EventRoundAnnounceMatchStartHandler);
        }

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            LoadCvar();
        }

        // Define method to load convars
        public void LoadCvar()
        {
            _timeLimit = ConVar.Find("mp_timelimit");
        }

        // Define method to remove timelimit
        public void DisableTimeLimit()
        {
            if(_timer != null)
            {
                // Clear timers
                _timer.Kill();

                 // Set plugin state
                _pluginState.TimeLimitCustom = false;
                _pluginState.TimeLimitEnabled = false;
                _pluginState.TimeLimitScheduled = false;
            }        
        }

        // Define methods to enforce time limit
        public void EnableTimeLimit()
        {
            // Set plugin state
            _pluginState.TimeLimitEnabled = true;
            _pluginState.TimeLimitScheduled = false;

            // Create timer
            _timer = new Timer((float)TimeRemaining, () =>
            {
                _serverManager.TriggerRotation();
                _pluginState.TimeLimitEnabled = false;
            });
        }

        public void EnableTimeLimit(float timeLimit)
        {
            // Set plugin state
            _pluginState.TimeLimitCustom = false;
            _pluginState.TimeLimitEnabled = true;
            _pluginState.TimeLimitScheduled = false;

            // Create timer
            _timer = new Timer(timeLimit, () =>
            {
                _serverManager.TriggerRotation();
                _pluginState.TimeLimitEnabled = false;
            });
        }

        // Define on match start behavior
        public HookResult EventRoundAnnounceMatchStartHandler(EventRoundAnnounceMatchStart @event, GameEventInfo info)
        {
            if (_pluginState.TimeLimitScheduled)
            {
                if(_pluginState.TimeLimitCustom)
                {
                    EnableTimeLimit(_pluginState.TimeLimit);
                }
                else
                {
                    EnableTimeLimit();
                }
            }
            else
            {
                DisableTimeLimit();
            }
            return HookResult.Continue;
        }

        // Define event game end handler
        public HookResult EventGameEndHandler(EventGameEnd @event, GameEventInfo info)
        {
            if (_pluginState.TimeLimitEnabled)
            {
                DisableTimeLimit();
            }
            return HookResult.Continue;
        }

        // Define event player disconnect handler
        public HookResult EventPlayerDisconnectHandler(EventPlayerDisconnect @event, GameEventInfo info)
        {
            if (Extensions.IsServerEmpty())
            {
                if (_pluginState.TimeLimitEnabled)
                {
                    DisableTimeLimit();
                }
            }
            return HookResult.Continue;
        }

        // Define reusable method to get time limit message
        public string GetTimeLeftMessage()
        {
            // Define message
            string _message;

            // If warmup, send general message
            if (_gameRules.WarmupRunning)
            {
                return _localizer.Localize("timeleft.warmup");
            }

            // Create message based on map conditions
            if (!UnlimitedTime) // If time not over
            {
                if (TimeRemaining > 1)
                {
                    // Get remaining time
                    TimeSpan remaining = TimeSpan.FromSeconds((double)TimeRemaining);

                    // If hours left
                    if (remaining.Hours > 0)
                    {
                        _message = _localizer.Localize("timeleft.remaining-time-hour", remaining.Hours.ToString("00"), remaining.Minutes.ToString("00"), remaining.Seconds.ToString("00"));
                    }
                    else if (remaining.Minutes > 0) // If minutes left
                    {
                        _message = _localizer.Localize("timeleft.remaining-time-minute", remaining.Minutes, remaining.Seconds);
                    }
                    else // If seconds left
                    {
                        _message = _localizer.Localize("timeleft.remaining-time-second", remaining.Seconds);
                    }
                }
                else // If time over
                {
                    _message = _localizer.Localize("timeleft.remaining-time-over");
                }
            }
            else if (!_maxRoundsManager.UnlimitedRounds) // If round limit not reached
            {
                if (_maxRoundsManager.RemainingRounds > 1) // If remaining rounds more than 1
                {
                    _message = _localizer.Localize("timeleft.remaining-rounds", _maxRoundsManager.RemainingRounds);
                }
                else // If last round
                {
                    _message = _localizer.Localize("timeleft.last-round");
                }
            }
            else // If no time or round limit
            {
                _message = _localizer.Localize("timeleft.no-time-limit");
            }

            // Return message
            return _message;
        }
    }
}