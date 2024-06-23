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
        public TimeLimitManager(GameRules gameRules)
        {
            _gameRules = gameRules;
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
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            LoadCvar();
        }
    }
}