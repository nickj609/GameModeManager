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
        // Define class dependencies
        private Config _config = new();
        private readonly GameRules _gameRules;
        private readonly PluginState _pluginState;
        private readonly StringLocalizer _localizer;
        private readonly ServerManager _serverManager;
        private readonly MaxRoundsManager _maxRoundsManager;

        // Define class instance
        public TimeLimitManager(GameRules gameRules, ServerManager serverManager, PluginState pluginState, MaxRoundsManager maxRoundsManager, IStringLocalizer iLocalizer)
        {
            _gameRules = gameRules;
            _pluginState = pluginState;
            _serverManager = serverManager;
            _maxRoundsManager = maxRoundsManager;
            _localizer = new StringLocalizer(iLocalizer, "timeleft.prefix");
        }

        // Define class properties
        private Timer? timer;
        private ConVar? timeLimit;
        private decimal timeLimitValue => (decimal)(timeLimit?.GetPrimitiveValue<float>() ?? 0F) * 60M;
        private bool unlimitedTime => timeLimitValue <= 0;
        private decimal timePlayed => _gameRules.WarmupRunning ? 0 : (decimal)(Server.CurrentTime - _gameRules.GameStartTime);
        private decimal timeRemaining => unlimitedTime || timePlayed > timeLimitValue ? 0 : timeLimitValue - timePlayed;

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

        // Define methods to get values
        public bool UnlimitedTime()
        {
            return unlimitedTime;
        }

        public decimal TimePlayed()
        {
            return timePlayed;
        }

        public decimal TimeRemaining()
        {
            return timeRemaining;
        }

        // Define method to load cvars
        public void LoadCvar()
        {
            timeLimit = ConVar.Find("mp_timelimit");
        }

        // Define method to disable time limit
        public void DisableTimeLimit()
        {
            if (timer != null)
            {
                timer.Kill();
                timer = null;
                _pluginState.TimeLimitCustom = false;
                _pluginState.TimeLimitEnabled = false;
                _pluginState.TimeLimitScheduled = false;
            }
        }

        // Define methods to enable time limit
        public void EnableTimeLimit()
        {
            _pluginState.TimeLimitEnabled = true;
            _pluginState.TimeLimitScheduled = false;
            timer = new Timer((float)timeRemaining, () =>
            {
                _serverManager.TriggerRotation();
                _pluginState.TimeLimitEnabled = false;
                timer = null; 
            });
        }

        public void EnableTimeLimit(float timeLimit)
        {
            _pluginState.TimeLimitCustom = false;
            _pluginState.TimeLimitEnabled = true;
            _pluginState.TimeLimitScheduled = false;
            timer = new Timer(timeLimit, () =>
            {
                _serverManager.TriggerRotation();
                _pluginState.TimeLimitEnabled = false;
                timer = null; 
            });
        }

        // Define method to get timeleft message
        public string GetTimeLeftMessage()
        {
            string _message;

            if (_gameRules.WarmupRunning)
            {
                return _localizer.Localize("timeleft.warmup");
            }

            if (!unlimitedTime)
            {
                if (timeRemaining > 1)
                {
                    TimeSpan remaining = TimeSpan.FromSeconds((double)timeRemaining);

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

        // Define event handlers
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