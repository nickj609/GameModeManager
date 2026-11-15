// Included libraries
using CounterStrikeSharp.API.Core;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using TimerFlags = CounterStrikeSharp.API.Modules.Timers.TimerFlags;

// Declare namespace
namespace GameModeManager.Common
{
    // Define class
    // A center message every second, "Map changing in 5...", then the action. The message is built for each player, so it's
    // in their language.
    public class CountdownTimer
    {
        // Define class dependencies
        private Timer? timer;
        private float interval;
        private Action? callback;

        // Define class constructor
        public CountdownTimer(float totalTime, Action onFinish, Func<CCSPlayerController, int, string> message)
        {
            interval = totalTime;
            callback = onFinish;
            StartTimer(message);
        }

        // Define class methods
        private void StartTimer(Func<CCSPlayerController, int, string> message)
        {
            timer = new Timer(1.0f, () => DisplayCountdown(message), TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        }

        private void DisplayCountdown(Func<CCSPlayerController, int, string> message)
        {
            foreach (CCSPlayerController player in PlayerExtensions.ValidPlayers(false))
            {
                CounterStrikeSharp.API.Modules.Menu.MenuManager.GetActiveMenus().Clear();
                player.PrintToCenterAlert(message(player, (int)interval));
            }

            interval -= 1.0f;

            if (interval <= 0.0f)
            {
                KillTimer();
                callback?.Invoke();
            }
        }
        private void KillTimer()
        {
            if (timer != null)
            {
                timer.Kill();
                timer = null;
            }
        }
    }
}