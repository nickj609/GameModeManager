// Included libraries
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using TimerFlags = CounterStrikeSharp.API.Modules.Timers.TimerFlags;

// Declare namespace
namespace GameModeManager.Timers
{
    // Define class
    public class CountdownTimer
    {
        // Define dependencies
        private Timer? timer;
        private float interval;
        private Action? callback;

        // Define class instance
        public CountdownTimer(float totalTime, Action onFinish, string message)
        {
            interval = totalTime;
            callback = onFinish;
            StartTimer(message);
        }

        // Define resuable method to create timer
        private void StartTimer(string message)
        {
            timer = new Timer(1.0f, () => DisplayCountdown(message), TimerFlags.REPEAT);
        }

        // Define resuable method to display countdown
        private void DisplayCountdown(string message)
        {
            foreach (CCSPlayerController player in Extensions.ValidPlayers(false))
            {
                MenuManager.GetActiveMenus().Clear();
                player.PrintToCenterAlert(message + " " + interval.ToString() + "...");
            }

            interval -= 1.0f;

            if (interval <= 0.0f)
            {
                StopTimer();
                callback?.Invoke();
            }
        }

        // Define reusable method to stop timer
        private void StopTimer()
        {
            if (timer != null)
            {
                timer.Kill();
                timer = null;
            }
        }
    }
}