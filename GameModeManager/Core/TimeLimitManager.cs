// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Cvars;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class TimeLimitManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin? _plugin;
        private ConVar? _timeLimit;
        private GameRules _gameRules;
        private PluginState _pluginState;
        private ServerManager _serverManager;
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
        public TimeLimitManager(GameRules gameRules, ServerManager serverManager, PluginState pluginState)
        {
            _gameRules = gameRules;
            _pluginState = pluginState;
            _serverManager = serverManager;

        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            // Load plugin instance
            _plugin = plugin;

            // Load convars
            LoadCvar();

            // Register event handler
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

        public void RemoveTimeLimit()
        {
            if(_plugin != null)
            {
                // Clear timers
                _plugin.Timers.Clear();

                // Set plugin state
                _pluginState.TimeLimitEnabled = false;
                _pluginState.TimeLimitEnabled = false;
                _pluginState.TimeLimitScheduled = false;
            }        
        }

        // Define methods to enforce time limit
        public void EnforceTimeLimit()
        {
            if(_plugin != null)
            {
                if(TimeRemaining != 0 && TimePlayed != 0)
                {
                    // Create timer
                    _plugin.AddTimer((float)TimeRemaining, () =>
                    {
                        _serverManager.TriggerRotation();
                        _pluginState.TimeLimitEnabled = false;
                    });

                    // Set plugin state
                    _pluginState.TimeLimitEnabled = true;
                    _pluginState.TimeLimitScheduled = false;
                }
            }
        }

        public void EnforceTimeLimit(float timeLimit)
        {
            if(_plugin != null)
            {
                // Create timer
                _plugin.AddTimer(timeLimit, () =>
                {
                    _serverManager.TriggerRotation();
                    _pluginState.TimeLimitEnabled = false;
                });

                // Set plugin state
                _pluginState.TimeLimitCustom = false;
                _pluginState.TimeLimitEnabled = true;
                _pluginState.TimeLimitScheduled = false;
            }
        }

        // Define on match start behavior
         public HookResult EventRoundAnnounceMatchStartHandler(EventRoundAnnounceMatchStart @event, GameEventInfo info)
        {
            if (_pluginState.TimeLimitScheduled)
            {
                if(_pluginState.TimeLimitCustom)
                {
                    EnforceTimeLimit(_pluginState.TimeLimit);
                }
                else
                {
                    EnforceTimeLimit();
                }
            }
            else
            {
                RemoveTimeLimit();
            }
            return HookResult.Continue;
        }
    }
}