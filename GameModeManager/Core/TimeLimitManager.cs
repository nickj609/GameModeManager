// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;
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
        private ILogger<TimeLimitManager> _logger;
        private readonly PluginState _pluginState;
        private readonly StringLocalizer _localizer;
        private readonly ServerManager _serverManager;
        private readonly MaxRoundsManager _maxRoundsManager;

        // Define class constructor
        public TimeLimitManager(GameRules gameRules, ServerManager serverManager, PluginState pluginState, MaxRoundsManager maxRoundsManager, IStringLocalizer iLocalizer, ILogger<TimeLimitManager> logger)
        {
            _logger = logger;
            _gameRules = gameRules;
            _pluginState = pluginState;
            _serverManager = serverManager;
            _maxRoundsManager = maxRoundsManager;
            _localizer = new StringLocalizer(iLocalizer, "timeleft.prefix");
        }

        // Define class properties
        private Timer? timer;
        private ConVar? timeLimit; 
        private decimal StandardTimeLimitValue => (decimal)(timeLimit?.GetPrimitiveValue<float>() ?? 0F) * 60M;
        private bool StandardUnlimitedTime => StandardTimeLimitValue <= 0;

        // Calculate time played
        private decimal timePlayed
        {
            get
            {
                if (_gameRules.WarmupRunning)
                    return 0;

                if (_pluginState.TimeLimit.CustomLimit && _pluginState.TimeLimit.CustomStartTime > 0)
                    return (decimal)(Server.CurrentTime - _pluginState.TimeLimit.CustomStartTime);
                else if (!_pluginState.TimeLimit.CustomLimit)
                    return (decimal)(Server.CurrentTime - _gameRules.GameStartTime);
                else
                    return 0;
            }
        }

        // Calculate time remaining
        private decimal timeRemaining
        {
            get
            {
                if (_pluginState.TimeLimit.CustomLimit)
                {
                    if (_pluginState.TimeLimit.CustomStartTime > 0 && _pluginState.TimeLimit.Duration > 0)
                    {
                        decimal elapsed = (decimal)(Server.CurrentTime - _pluginState.TimeLimit.CustomStartTime);
                        decimal totalDuration = (decimal)_pluginState.TimeLimit.Duration; 
                        decimal remaining = totalDuration - elapsed;
                        return remaining > 0 ? remaining : 0;
                    }
                    return 0;
                }
                else
                {
                    if (StandardUnlimitedTime)
                        return 0;

                    decimal standardTotal = StandardTimeLimitValue;
                    decimal standardPlayed = timePlayed;

                    if (standardPlayed >= standardTotal)
                        return 0;

                    return standardTotal - standardPlayed;
                }
            }
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
            _pluginState.RTV.MaxExtends = _config.RTV.MaxExtends;
            _pluginState.RTV.IncludeExtend = _config.RTV.IncludeExtend;
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
            _pluginState.TimeLimit.CustomLimit = false;
            _pluginState.TimeLimit.Duration = 0f;
            _pluginState.TimeLimit.CustomStartTime = 0f;
            _pluginState.TimeLimit.Enabled = false;
            _pluginState.TimeLimit.Scheduled = false;
            _pluginState.RTV.MapExtends = 0;
            _pluginState.RTV.MaxExtends = _config.RTV.MaxExtends; 
        }

        // Define class methods
        public bool UnlimitedTime()
        {
            return StandardUnlimitedTime;
        }

        public decimal TimePlayed()
        {
            return timePlayed; 
        }

        public decimal TimeRemaining()
        {
            return timeRemaining; 
        }

        private void LoadCvar()
        {
            timeLimit = ConVar.Find("mp_timelimit");
        }

        public void ExtendMap()
        {
            if (_pluginState.TimeLimit.CustomLimit)
            {
                 float extensionSeconds = _config.RTV.ExtendTime * 60f;
                 _pluginState.TimeLimit.Duration += extensionSeconds;

                 // If the timer is already running, kill and restart it with the new total remaining time
                 if (timer != null)
                 {
                     timer.Kill();
                     decimal elapsed = (decimal)(Server.CurrentTime - _pluginState.TimeLimit.CustomStartTime);
                     decimal newTotalDuration = (decimal)_pluginState.TimeLimit.Duration;
                     decimal newRemaining = newTotalDuration - elapsed;

                     if (newRemaining > 0)
                     {
                         timer = new Timer((float)newRemaining, () =>
                         {
                             _serverManager.TriggerRotation();
                             DisableTimeLimit();
                        });
                         _logger.LogInformation($"Extended custom timer. New total duration: {_pluginState.TimeLimit}s. New remaining: {newRemaining}s");
                     }
                     else {
                          _logger.LogWarning($"Could not extend custom timer, calculated remaining time is not positive ({newRemaining}s).");
                          DisableTimeLimit();
                     }

                 } else {
                      _logger.LogInformation($"Custom time limit duration extended to {_pluginState.TimeLimit}s. Timer will start at match start.");
                 }

                 string _message = _localizer.LocalizeWithPrefixInternal("rtv.prefix", "rtv.map-time-extended", _config.RTV.ExtendTime);
                 Server.PrintToChatAll(_message);

            }
            else if (!StandardUnlimitedTime)
            {
                // Extend the standard mp_timelimit
                var timeLimitConVar = ConVar.Find("mp_timelimit");
                if (timeLimitConVar != null) 
                {
                    float currentTimeLimit = timeLimitConVar.GetPrimitiveValue<float>();
                    float newTimeLimit = currentTimeLimit + _config.RTV.ExtendTime;
                    _logger.LogInformation($"Setting mp_timelimit to {newTimeLimit}");
                    timeLimitConVar.SetValue(newTimeLimit);
                    string _message = _localizer.LocalizeWithPrefixInternal("rtv.prefix", "rtv.map-time-extended", _config.RTV.ExtendTime);
                    Server.PrintToChatAll(_message);
                }

            }
            else if (!_maxRoundsManager.UnlimitedRounds)
            {
                // Extend rounds if time is unlimited
                var maxRoundsConVar = ConVar.Find("mp_maxrounds");
                if (maxRoundsConVar != null) 
                {
                    int currentMaxRounds = maxRoundsConVar.GetPrimitiveValue<int>();
                    int newMaxRounds = currentMaxRounds + _config.RTV.ExtendRounds;
                    _logger.LogInformation($"Setting mp_maxrounds to {newMaxRounds}");
                    maxRoundsConVar.SetValue(newMaxRounds);
                    string _message = _localizer.LocalizeWithPrefixInternal("rtv.prefix", "rtv.map-rounds-extended", _config.RTV.ExtendRounds);
                    Server.PrintToChatAll(_message);
                }
            }
            else
            {
                _logger.LogWarning("Can't extend map because mp_timelimit and mp_maxrounds are not set.");
            }
            _pluginState.RTV.MapExtends++;
        }

        public void DisableTimeLimit()
        {
            if (timer != null)
            {
                timer.Kill();
                timer = null;
            }
            
            _pluginState.TimeLimit.CustomLimit = false;
            _pluginState.TimeLimit.Enabled = false;
            _pluginState.TimeLimit.Scheduled = false;
            _pluginState.TimeLimit.Duration = 0f;
            _pluginState.TimeLimit.CustomStartTime = 0f;
            _logger.LogDebug("Time limit timer disabled and state reset.");
        }

        public void EnableTimeLimit()
        {
            DisableTimeLimit();

            // Only enable if standard time isn't unlimited
            if (!StandardUnlimitedTime)
            {
                decimal remaining = TimeRemaining(); 

                if (remaining > 0) 
                {
                    _pluginState.TimeLimit.CustomLimit = false; 
                    _pluginState.TimeLimit.Enabled = true;
                    _pluginState.TimeLimit.Scheduled = false; 
                    
                     timer = new Timer((float)remaining, () =>
                    {
                        _logger.LogDebug("Standard time limit reached, triggering rotation."); 
                        _serverManager.TriggerRotation();
                        DisableTimeLimit(); 
                    });
                    _logger.LogDebug($"Standard timer enabled. Remaining: {remaining}s");
                } 
                else 
                {
                    _logger.LogDebug("Standard time limit calculated as 0 or less, not starting timer.");
                }
            } 
            else 
            {
                _logger.LogDebug("Standard mp_timelimit is 0, timer not enabled.");
            }
        }

        public void EnableTimeLimit(float customDurationSeconds)
        {
            DisableTimeLimit();

             if (customDurationSeconds <= 0) 
             {
                 _logger.LogWarning($"Attempted to enable custom time limit with non-positive duration: {customDurationSeconds}s. Aborting.");
                 return;
             }

            _pluginState.TimeLimit.CustomLimit = true; 
            _pluginState.TimeLimit.Enabled = true; 
            _pluginState.TimeLimit.Scheduled = false; 
            _pluginState.TimeLimit.Duration = customDurationSeconds; 
            _pluginState.TimeLimit.CustomStartTime = Server.CurrentTime; 

            // Set time limit cvar
            var timeLimitConVar = ConVar.Find("mp_timelimit");
            timeLimitConVar?.SetValue(customDurationSeconds / 60f); 
            LoadCvar(); 

            _logger.LogDebug($"Starting custom timer for {customDurationSeconds} seconds.");

            timer = new Timer(customDurationSeconds, () =>
            {
                _logger.LogDebug($"Custom time limit of {_pluginState.TimeLimit}s reached, triggering rotation.");
                _serverManager.TriggerRotation();
                DisableTimeLimit();
            });
        }

        public string GetTimeLeftMessage()
        {
            string _message;

            if (_gameRules.WarmupRunning)
                return _localizer.Localize("timeleft.warmup");

            decimal currentRemaining = timeRemaining;

            if (!UnlimitedTime())
            {
                if (currentRemaining > 1) 
                {
                    TimeSpan remaining = TimeSpan.FromSeconds((double)currentRemaining);

                    if (remaining.Hours > 0)
                        _message = _localizer.Localize("timeleft.remaining-time-hour", remaining.Hours.ToString("00"), remaining.Minutes.ToString("00"), remaining.Seconds.ToString("00"));
                    else if (remaining.Minutes > 0)
                        _message = _localizer.Localize("timeleft.remaining-time-minute", remaining.Minutes.ToString("00"), remaining.Seconds.ToString("00"));
                    else
                        _message = _localizer.Localize("timeleft.remaining-time-second", remaining.Seconds);
                }
                else
                {
                    _message = _localizer.Localize("timeleft.remaining-time-over");
                }
            }
            else if (!_maxRoundsManager.UnlimitedRounds) 
            {
                if (_maxRoundsManager.RemainingRounds > 1)
                    _message = _localizer.Localize("timeleft.remaining-rounds", _maxRoundsManager.RemainingRounds);
                else
                    _message = _localizer.Localize("timeleft.last-round");
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
            _logger.LogDebug("EventRoundAnnounceMatchStart triggered."); 
            if (_pluginState.TimeLimit.Scheduled)
            {
                _logger.LogDebug($"TimeLimitScheduled is true. Custom: {_pluginState.TimeLimit.CustomLimit}");

                if (_pluginState.TimeLimit.CustomLimit)
                {
                    _logger.LogDebug($"Enabling scheduled custom time limit: {_pluginState.TimeLimit.Duration}s");
                    EnableTimeLimit(_pluginState.TimeLimit.Duration); 
                }
                else
                {
                    _logger.LogDebug("Enabling scheduled standard time limit.");
                    EnableTimeLimit();
                }
                _pluginState.TimeLimit.Scheduled = false;
            }
            else
            {
                DisableTimeLimit();
            }
            return HookResult.Continue;
        }

        public HookResult EventGameEndHandler(EventGameEnd @event, GameEventInfo info)
        {
            _logger.LogDebug("EventGameEnd triggered. Disabling timer."); 
            DisableTimeLimit();
            return HookResult.Continue;
        }

        public HookResult EventPlayerDisconnectHandler(EventPlayerDisconnect @event, GameEventInfo info)
        {
            if (ServerExtensions.IsServerEmpty())
            {
                _logger.LogDebug("Server is empty. Disabling timer.");
                DisableTimeLimit();
            }
            return HookResult.Continue;
        }
    }
}