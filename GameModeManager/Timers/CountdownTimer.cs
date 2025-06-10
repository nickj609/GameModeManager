// Included libraries
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using TimerFlags = CounterStrikeSharp.API.Modules.Timers.TimerFlags;

// Declare namespace
namespace GameModeManager.Timers
{
    // Define class
    public class CountdownTimer
    {
        // Define class dependencies
        private Timer? timer;
        private float interval;
        private Action? callback;

        // Define class constructor
        public CountdownTimer(float totalTime, Action onFinish, string message)
        {
            interval = totalTime;
            callback = onFinish;
            StartTimer(message);
        }

        // Define class methods
        private void StartTimer(string message)
        {
            timer = new Timer(1.0f, () => DisplayCountdown(message), TimerFlags.REPEAT);
        }

        private void DisplayCountdown(string message)
        {
            foreach (CCSPlayerController player in PlayerExtensions.ValidPlayers(false))
            {
                CounterStrikeSharp.API.Modules.Menu.MenuManager.GetActiveMenus().Clear();
                player.PrintToCenterAlert(message + " " + interval.ToString() + "...");
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