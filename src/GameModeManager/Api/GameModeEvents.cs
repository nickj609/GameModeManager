// Included libraries
using GameModeManager.Shared;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Api
{
    // Define class
    // The events other plugins get through game_mode:api (IGameModeEvents). GameModeManager raises them here; each handler runs
    // on its own, so one that throws is logged and the others still run.
    public class GameModeEvents : IGameModeEvents, IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private ILogger<GameModeEvents> _logger;

        // Define class constructor
        public GameModeEvents(ILogger<GameModeEvents> logger)
        {
            _logger = logger;
        }

        // Define class properties
        private const float SameNextMapSeconds = 3; // An admin's next map and GG1's message about it are one change
        private string _lastNextMap = "";
        private float _lastNextMapTime = -100;

        public event Action<IMode, IMode>? ModeChanged;
        public event Action<string>? MapChanging;
        public event Action<string>? NextMapSet;
        public event Action<ISetting, bool>? SettingChanged;
        public event Action<IValueSetting, string>? ValueSettingChanged;
        public event Action<IGameAction>? ActionRun;
        public event Action<string, bool, int, int>? VoteFinished;
        public event Action<int>? RoundRestored;
        public event Action<IMode>? WarmupStarted;
        public event Action? WarmupEnded;

        // Define class methods
        public void RaiseModeChanged(IMode old, IMode current) => Raise(nameof(ModeChanged), ModeChanged, handler => handler(old, current));
        public void RaiseMapChanging(string map) => Raise(nameof(MapChanging), MapChanging, handler => handler(map));
        public void RaiseSettingChanged(ISetting setting, bool on) => Raise(nameof(SettingChanged), SettingChanged, handler => handler(setting, on));
        public void RaiseValueSettingChanged(IValueSetting setting, string value) => Raise(nameof(ValueSettingChanged), ValueSettingChanged, handler => handler(setting, value));
        public void RaiseActionRun(IGameAction action) => Raise(nameof(ActionRun), ActionRun, handler => handler(action));
        public void RaiseVoteFinished(string title, bool passed, int yes, int no) => Raise(nameof(VoteFinished), VoteFinished, handler => handler(title, passed, yes, no));
        public void RaiseRoundRestored(int round) => Raise(nameof(RoundRestored), RoundRestored, handler => handler(round));
        public void RaiseWarmupStarted(IMode mode) => Raise(nameof(WarmupStarted), WarmupStarted, handler => handler(mode));
        public void RaiseWarmupEnded() => Raise(nameof(WarmupEnded), WarmupEnded, handler => handler());

        // Once per change: setting it and GG1's announcement of it both report it
        public void RaiseNextMapSet(string name)
        {
            if (name.Equals(_lastNextMap, StringComparison.OrdinalIgnoreCase) && Server.CurrentTime - _lastNextMapTime < SameNextMapSeconds)
                return;

            _lastNextMap = name;
            _lastNextMapTime = Server.CurrentTime;
            Raise(nameof(NextMapSet), NextMapSet, handler => handler(name));
        }

        private void Raise<T>(string name, T? handlers, Action<T> call) where T : Delegate
        {
            if (handlers == null)
                return;

            foreach (T handler in handlers.GetInvocationList().Cast<T>())
            {
                try
                {
                    call(handler);
                }
                catch (Exception error)
                {
                    _logger.LogError("A {Event} handler in {Plugin} failed: {Error}", name, handler.Method.DeclaringType?.Assembly.GetName().Name ?? "another plugin", error.Message);
                }
            }
        }
    }
}
