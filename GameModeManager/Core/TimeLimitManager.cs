// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using GameModeManager.Contracts;
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
        private readonly GameRules _gameRules;
        private readonly PluginState _pluginState;
        private readonly StringLocalizer _localizer;
        private readonly ServerManager _serverManager;
        private readonly MaxRoundsManager _maxRoundsManager;

        // Define variables
        private Timer? _timer;
        private ConVar? _timeLimit;
        private Config _config = new();
        public decimal TimeLimitValue => (decimal)(_timeLimit?.GetPrimitiveValue<float>() ?? 0F) * 60M;
        public bool UnlimitedTime => TimeLimitValue <= 0;
        public decimal TimePlayed => _gameRules.WarmupRunning ? 0 : (decimal)(Server.CurrentTime - _gameRules.GameStartTime);
        public decimal TimeRemaining => UnlimitedTime || TimePlayed > TimeLimitValue ? 0 : TimeLimitValue - TimePlayed;

        // Define class instance
        public TimeLimitManager(GameRules gameRules, ServerManager serverManager, PluginState pluginState, MaxRoundsManager maxRoundsManager, IStringLocalizer iLocalizer)
        {
            _gameRules = gameRules;
            _pluginState = pluginState;
            _serverManager = serverManager;
            _maxRoundsManager = maxRoundsManager;
            _localizer = new StringLocalizer(iLocalizer, "timeleft.prefix");
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            if (_config.Commands.TimeLimit)
            {
                LoadCvar();
                plugin.RegisterEventHandler<EventGameEnd>(EventGameEndHandler);
                plugin.RegisterEventHandler<EventPlayerDisconnect>(EventPlayerDisconnectHandler);
                plugin.RegisterEventHandler<EventRoundAnnounceMatchStart>(EventRoundAnnounceMatchStartHandler);
            }
        }

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            LoadCvar();
        }

        // Function to load timelimit
        public void LoadCvar()
        {
            _timeLimit = ConVar.Find("mp_timelimit");
        }

        // Function to disable time limit
        public void DisableTimeLimit()
        {
            if (_timer != null)
            {
                _timer.Kill();
                _timer = null; // Clear the reference
                _pluginState.TimeLimitCustom = false;
                _pluginState.TimeLimitEnabled = false;
                _pluginState.TimeLimitScheduled = false;
            }
        }

        // Functions to enable time limit
        public void EnableTimeLimit()
        {
            _pluginState.TimeLimitEnabled = true;
            _pluginState.TimeLimitScheduled = false;
            _timer = new Timer((float)TimeRemaining, () =>
            {
                _serverManager.TriggerRotation();
                _pluginState.TimeLimitEnabled = false; // Disable after rotation
                _timer = null; // Clear timer reference
            });
        }

        public void EnableTimeLimit(float timeLimit)
        {
            _pluginState.TimeLimitCustom = false;
            _pluginState.TimeLimitEnabled = true;
            _pluginState.TimeLimitScheduled = false;
            _timer = new Timer(timeLimit, () =>
            {
                _serverManager.TriggerRotation();
                _pluginState.TimeLimitEnabled = false; // Disable after rotation
                _timer = null; // Clear timer reference
            });
        }

        // Functions to get time left message
        public string GetTimeLeftMessage()
        {
            string _message;

            if (_gameRules.WarmupRunning)
            {
                return _localizer.Localize("timeleft.warmup");
            }

            if (!UnlimitedTime)
            {
                if (TimeRemaining > 1)
                {
                    TimeSpan remaining = TimeSpan.FromSeconds((double)TimeRemaining);

                    if (remaining.Hours > 0)
                    {
                        _message = _localizer.Localize("timeleft.remaining-time-hour", remaining.Hours.ToString("00"), remaining.Minutes.ToString("00"), remaining.Seconds.ToString("00"));
                    }
                    else if (remaining.Minutes > 0)
                    {
                        _message = _localizer.Localize("timeleft.remaining-time-minute", remaining.Minutes, remaining.Seconds);
                    }
                    else
                    {
                        _message = _localizer.Localize("timeleft.remaining-time-second", remaining.Seconds);
                    }
                }
                else
                {
                    _message = _localizer.Localize("timeleft.remaining-time-over");
                }
            }
            else if (!_maxRoundsManager.UnlimitedRounds)
            {
                if (_maxRoundsManager.RemainingRounds > 1)
                {
                    _message = _localizer.Localize("timeleft.remaining-rounds", _maxRoundsManager.RemainingRounds);
                }
                else
                {
                    _message = _localizer.Localize("timeleft.last-round");
                }
            }
            else
            {
                _message = _localizer.Localize("timeleft.no-time-limit");
            }

            return _message;
        }

        // Construct handlers to enable and disable time limit
        public HookResult EventRoundAnnounceMatchStartHandler(EventRoundAnnounceMatchStart @event, GameEventInfo info)
        {
            if (_pluginState.TimeLimitScheduled)
            {
                if (_pluginState.TimeLimitCustom)
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

        public HookResult EventGameEndHandler(EventGameEnd @event, GameEventInfo info)
        {
            DisableTimeLimit();
            return HookResult.Continue;
        }

        public HookResult EventPlayerDisconnectHandler(EventPlayerDisconnect @event, GameEventInfo info)
        {
            if (Extensions.IsServerEmpty())
            {
                DisableTimeLimit();
            }
            return HookResult.Continue;
        }
    }
}