// Included libraries
using GameModeManager.Core;
using GameModeManager.Shared;
using GameModeManager.Contracts;

// Declare namespace
namespace GameModeManager.Services
{
    // Define class
    public class TimeLimitApi : ITimeLimitApi, IPluginDependency<Plugin, Config>
    {
        // Define class constructor
        public TimeLimitApi(PluginState pluginState, TimeLimitManager timeLimitManager, MaxRoundsManager maxRoundsManager)
        {
            Control = new TimeLimitController(timeLimitManager);
            Messaging = new TimeLimitMessenger(timeLimitManager);
            State = new TimeLimitState(pluginState, maxRoundsManager, timeLimitManager);
        }

        // Define class properties
        public ITimeLimitState State { get; }
        public ITimeLimitControl Control { get; }
        public ITimeLimitMessaging Messaging { get; }

        // Define child classes
        private class TimeLimitState : ITimeLimitState
        {
            // Define class dependencies
            private readonly PluginState _pluginState;
            private readonly TimeLimitManager _timeLimitManager;
            private readonly MaxRoundsManager _maxRoundsManager;

            // Define class constructor
            public TimeLimitState(PluginState pluginState, MaxRoundsManager maxRoundsManager, TimeLimitManager timeLimitManager)
            {
                _pluginState = pluginState;
                _maxRoundsManager = maxRoundsManager;
                _timeLimitManager = timeLimitManager;
            }

            // Define class properties
            public int MaxWins => _maxRoundsManager.MaxWins;
            public bool Enabled => _pluginState.TimeLimit.Enabled;
            public bool Custom => _pluginState.TimeLimit.CustomLimit;
            public float TimeLimit => _pluginState.TimeLimit.Duration;
            public bool Scheduled => _pluginState.TimeLimit.Scheduled;
            public decimal TimePlayed => _timeLimitManager.TimePlayed();
            public int RemainingWins => _maxRoundsManager.RemainingWins;
            public bool UnlimitedTime => _timeLimitManager.UnlimitedTime();
            public bool UnlimitedRounds => _maxRoundsManager.UnlimitedRounds;
            public decimal TimeRemaining => _timeLimitManager.TimeRemaining();
        }

        private class TimeLimitController : ITimeLimitControl
        {
            // Define class dependencies
            private readonly TimeLimitManager _timeLimitManager;

            // Define class constructor
            public TimeLimitController(TimeLimitManager timeLimitManager)
            {
                _timeLimitManager = timeLimitManager;
            }

            // Define class methods
            public void Enforce() => _timeLimitManager.EnableTimeLimit();
            public void Disable() => _timeLimitManager.DisableTimeLimit();
            public void CustomLimit(int time) => _timeLimitManager.EnableTimeLimit(time);
        }

        private class TimeLimitMessenger : ITimeLimitMessaging
        {
            // Define class dependencies
            private readonly TimeLimitManager _timeLimitManager;

            // Define class constructor
            public TimeLimitMessenger(TimeLimitManager timeLimitManager)
            {
                _timeLimitManager = timeLimitManager;
            }

            // Define class methods
            public string TimeLeftMessage() => _timeLimitManager.GetTimeLeftMessage();
        }
    }
}