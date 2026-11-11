// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.Actions
{
    // Define class
    // Freeze players, on the Rounds page: everyone alive stands still (players who respawn are frozen too), and the round clock
    // holds while they are. Not a match pause: the bomb timer, smokes and molotovs and grenades in the air keep going, which is
    // why CS2 and MatchZy only pause in freeze time (Pause match). For casual games and admin moments. A new round or map
    // ends it.
    public class PlayerFreeze : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private GameRules _gameRules;
        private StringLocalizer _localizer;
        private Plugin? _plugin;

        // Define class constructor
        public PlayerFreeze(GameRules gameRules, StringLocalizer localizer)
        {
            _gameRules = gameRules;
            _localizer = localizer;
        }

        // Define class properties
        private const float Tick = 0.25f;
        private Timer? _timer;
        public bool Frozen { get; private set; }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            plugin.RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                Stop();
                return HookResult.Continue;
            });
        }

        // Define on map start behavior
        public void OnMapStart(string mapName)
        {
            _timer = null; // The map change stopped it
            Frozen = false;
        }

        // Define class methods
        public void Toggle(CCSPlayerController player)
        {
            if (Frozen)
            {
                Stop();
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("freeze.off-message", player.PlayerName));
                return;
            }

            Frozen = true;
            PlayerExtensions.FreezePlayers();
            _timer = _plugin?.AddTimer(Tick, () =>
            {
                PlayerExtensions.FreezePlayers(); // Players who spawned since
                _gameRules.HoldRoundClock(Tick);
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("freeze.on-message", player.PlayerName));
        }

        private void Stop()
        {
            _timer?.Kill();
            _timer = null;
            if (!Frozen)
                return;
            Frozen = false;
            PlayerExtensions.UnfreezePlayers();
        }
    }
}
