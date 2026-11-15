// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Cvars;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.Rotation
{
    // Define class
    public class TimeLimitManager : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Config _config = new();
        private readonly GameRules _gameRules;
        private ILogger<TimeLimitManager> _logger;
        private readonly PluginState _pluginState;
        private readonly ServerManager _serverManager;

        // Define class constructor
        public TimeLimitManager(GameRules gameRules, ServerManager serverManager, PluginState pluginState, ILogger<TimeLimitManager> logger)
        {
            _logger = logger;
            _gameRules = gameRules;
            _pluginState = pluginState;
            _serverManager = serverManager;
        }

        // Define class properties
        private Plugin? _plugin;
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
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;

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

            // Also stops the last map's timer, which would rotate this map when it fires
            DisableTimeLimit();
        }

        // Define class methods
        public bool UnlimitedTime()
        {
            return StandardUnlimitedTime;
        }

        public decimal TimeRemaining()
        {
            timeLimit ??= ConVar.Find("mp_timelimit");
            return timeRemaining;
        }

        // Whether this map has a time limit at all: a custom one from !timelimit, or mp_timelimit
        public bool HasTimeLimit()
        {
            timeLimit ??= ConVar.Find("mp_timelimit");
            return _pluginState.TimeLimit.CustomLimit ? _pluginState.TimeLimit.Duration > 0 : !StandardUnlimitedTime;
        }

        private void LoadCvar()
        {
            timeLimit = ConVar.Find("mp_timelimit");
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
            _pluginState.TimeLimit.Duration = 0f;
            _pluginState.TimeLimit.CustomStartTime = 0f;
            _logger.LogDebug("Time limit timer disabled and state reset.");
        }

        // GG1MapChooser's "extend map" vote raises mp_timelimit: keep playing until the new limit, then rotate
        private void OnTimeLimitReached()
        {
            if (_pluginState.TimeLimit.CustomLimit && StandardTimeLimitValue > (decimal)_pluginState.TimeLimit.Duration + 1)
                _pluginState.TimeLimit.Duration = (float)StandardTimeLimitValue;

            decimal remaining = timeRemaining;
            if (remaining > 1)
            {
                _logger.LogInformation($"Time limit extended: {remaining:0}s left.");
                timer = _plugin?.AddTimer((float)remaining, OnTimeLimitReached);
                return;
            }

            _logger.LogDebug("Time limit reached, triggering rotation.");
            _serverManager.TriggerRotation();
            DisableTimeLimit();
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
                    
                    timer = _plugin?.AddTimer((float)remaining, OnTimeLimitReached);
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
            _pluginState.TimeLimit.Duration = customDurationSeconds; 
            _pluginState.TimeLimit.CustomStartTime = Server.CurrentTime; 

            // Set time limit cvar
            var timeLimitConVar = ConVar.Find("mp_timelimit");
            timeLimitConVar?.SetValue(customDurationSeconds / 60f); 
            LoadCvar(); 

            _logger.LogDebug($"Starting custom timer for {customDurationSeconds} seconds.");

            timer = _plugin?.AddTimer(customDurationSeconds, OnTimeLimitReached);
        }

        // Define event handlers
        public HookResult EventRoundAnnounceMatchStartHandler(EventRoundAnnounceMatchStart @event, GameEventInfo info)
        {
            _logger.LogDebug("EventRoundAnnounceMatchStart triggered.");
            DisableTimeLimit();
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