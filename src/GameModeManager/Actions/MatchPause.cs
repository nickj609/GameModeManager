// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

// Declare namespace
namespace GameModeManager.Actions
{
    // Define class
    // Pausing the match and timeouts, for the Rounds page. CS2 only stops a match in freeze time: mp_pause_match and the
    // timeouts asked for mid-round wait for the next round. The game doesn't say a pause is waiting, so this remembers its own
    // request until the round starts. Then one row can say "Pause match", "Pause at next round" or "Unpause match", and
    // timeouts can't be stacked on a pause or on each other.
    public class MatchPause : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private GameRules _gameRules;
        private StringLocalizer _localizer;

        // Define class constructor
        public MatchPause(GameRules gameRules, StringLocalizer localizer)
        {
            _gameRules = gameRules;
            _localizer = localizer;
        }

        // Define class properties
        private bool _pausePending;
        private bool _timeoutPending;

        public enum PauseState { Running, Pending, Paused }

        public PauseState State => _gameRules.MatchPaused ? PauseState.Paused : _pausePending ? PauseState.Pending : PauseState.Running;

        // A timeout can be called when the match isn't paused or about to be, and no timeout is running or waiting
        public bool CanCallTimeout => State == PauseState.Running && !_gameRules.TimeoutRunning && !_timeoutPending && !_gameRules.WarmupRunning;

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            // The round started: a pause or timeout asked for mid-round has happened (or the game dropped it)
            plugin.RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                Server.NextFrame(() =>
                {
                    _pausePending = false;
                    _timeoutPending = false;
                });
                return HookResult.Continue;
            });
        }

        // Define on map start behavior
        public void OnMapStart(string mapName)
        {
            _pausePending = false;
            _timeoutPending = false;
        }

        // Define class methods
        public void Pause(CCSPlayerController player)
        {
            if (State != PauseState.Running)
                return;

            bool now = _gameRules.FreezePeriod;
            _pausePending = !now;
            Server.ExecuteCommand("mp_pause_match");
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix(now ? "pause.message" : "pause.pending-message", player.PlayerName));
        }

        // Also cancels a pause that's still waiting for the next round
        public void Unpause(CCSPlayerController player)
        {
            if (State == PauseState.Running)
                return;

            _pausePending = false;
            Server.ExecuteCommand("mp_unpause_match");
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("unpause.message", player.PlayerName));
        }

        public void Timeout(CCSPlayerController player, bool counterTerrorists)
        {
            if (!CanCallTimeout)
                return;

            _timeoutPending = !_gameRules.FreezePeriod;
            Server.ExecuteCommand(counterTerrorists ? "timeout_ct_start" : "timeout_terrorist_start");
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("timeout.message", player.PlayerName, counterTerrorists ? "CT" : "T"));
        }

        // The pause row's text for the current state
        public string PauseLabel()
        {
            return _localizer.Localize(State switch
            {
                PauseState.Paused => "action.unpause",
                PauseState.Pending => "action.pause-pending",
                _ => "action.pause"
            });
        }

        // The pause row's action for the current state
        public void Toggle(CCSPlayerController player)
        {
            if (State == PauseState.Running)
                Pause(player);
            else
                Unpause(player);
        }
    }
}
