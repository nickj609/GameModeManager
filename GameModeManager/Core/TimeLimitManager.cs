// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
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
        private ConVar? _warmupTime;
        private GameRules _gameRules;
        private ServerManager _serverManager;
        public decimal TimeLimitValue => (decimal)(_timeLimit?.GetPrimitiveValue<float>() ?? 0F) * 60M;
        public decimal WarmupTimeValue => (decimal)(_warmupTime?.GetPrimitiveValue<float>() ?? 0F) * 60M;
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
        public TimeLimitManager(GameRules gameRules, ServerManager serverManager)
        {
            _gameRules = gameRules;
            _serverManager = serverManager;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            LoadCvar();
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
            _warmupTime = ConVar.Find("mp_warmuptime");
        }

        // Define method to enforce time limit
        public void EnforceTimeLimit(bool enabled)
        {
            if(enabled && _plugin != null)
            {
                // Clear previous timers
                _plugin.Timers.Clear();

                // Enforce time limit
                _plugin.AddTimer((float)TimeRemaining, () =>
                {
                    _serverManager.TriggerRotation();
                });
            }
            else if(!enabled && _plugin != null)
            {
                // Clear previous timers
                _plugin.Timers.Clear();
            }
        }

        // Define method to enforce custom time limit
        public void EnforceCustomTimeLimit(bool enabled, float timeLimit)
        {
            if(enabled && _plugin != null)
            {
                // Clear previous timers
                _plugin.Timers.Clear();

                // Enforce time limit
                _plugin.AddTimer(timeLimit, () =>
                {
                    _serverManager.TriggerRotation();
                });
            }
            else if(!enabled && _plugin != null)
            {
                // Clear previous timers
                _plugin.Timers.Clear();
            }
        }
    }
}