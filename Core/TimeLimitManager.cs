// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;

// Declare namespace
namespace GameModeManager
{
    // Define class
    public class TimeLimitManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private RotationManager _rotationManager;
        private GameRules _gameRules;
        private ConVar? _timeLimit;
        private decimal TimeLimitValue => (decimal)(_timeLimit?.GetPrimitiveValue<float>() ?? 0F) * 60M;
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
        public TimeLimitManager(GameRules gameRules, RotationManager rotationManager)
        {
            _gameRules = gameRules;
            _rotationManager = rotationManager;
        }

        // Define method to load convars
        void LoadCvar()
        {
            _timeLimit = ConVar.Find("mp_timelimit");
        }

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            LoadCvar();
            if (!UnlimitedTime) // If time limit exists
            {
                // Create timer based on the retrieved time limit
                StartTimer();
            }
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            LoadCvar();
        }

        private Timer? timer;

        // Define time limit timer to force change map on time limit end
        private void StartTimer()
        {
            // Calculate interval in milliseconds
            int intervalInMilliseconds = (int)Math.Floor(TimeLimitValue);

            // Create a delegate to encapsulate rotation logic
            TimerCallback callback = delegate { _rotationManager.TriggerRotation(); };

            // Create timer with interval and callback
            timer = new Timer(callback, null, intervalInMilliseconds, Timeout.Infinite);
        }
    }
}