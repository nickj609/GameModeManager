// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Cvars;

// Declare namespace
namespace GameModeManager
{
    // Define class
    public class TimeLimitManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private ConVar? _timeLimit;
        private ConVar? _warmupTime;
        private GameRules _gameRules;
         private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ILogger<TimeLimitManager> _logger;
        private Config _config = new Config();
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
        public TimeLimitManager(GameRules gameRules, StringLocalizer stringLocalizer, ILogger<TimeLimitManager> logger, PluginState pluginState )
        {
            _logger = logger;
            _gameRules = gameRules;
            _pluginState = pluginState;
            _localizer = stringLocalizer;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
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
        public void EnforceTimeLimit(Plugin plugin, bool enabled)
        {
            if(enabled)
            {
                // Clear previous timers
                plugin.Timers.Clear();

                // Enforce time limit
                plugin.AddTimer((float)TimeRemaining, () =>
                {
                    ServerManager.TriggerRotation(plugin, _config, _pluginState, _logger, _localizer);
                });
            }
            else
            {
                // Clear previous timers
                plugin.Timers.Clear();
            }
        }

        public void EnforceCustomTimeLimit(Plugin plugin, float timeLimit)
        {

            // Clear previous timers
            plugin.Timers.Clear();

            // Enforce time limit
            plugin.AddTimer(timeLimit, () =>
            {
                ServerManager.TriggerRotation(plugin, _config, _pluginState, _logger, _localizer);
            });
        }
    }
}