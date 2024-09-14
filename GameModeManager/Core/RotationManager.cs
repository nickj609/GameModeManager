// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
 
// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class RotationManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin? _plugin;
        private ServerManager _serverManager;
        private Config _config = new Config();
        private TimeLimitManager _timeLimitManager;

        // Define class instance
        public RotationManager(TimeLimitManager timeLimitManager, ServerManager serverManager)
        {
            _serverManager = serverManager;
            _timeLimitManager = timeLimitManager;
        }
        
        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        { 
            // Load plugin
            _plugin = plugin;

            // Define event game end handler
            _plugin.RegisterEventHandler<EventCsWinPanelMatch>((@event, info) =>
            {  
                _serverManager.TriggerRotation();
                return HookResult.Continue;
            }, HookMode.Post);

            // Create mode schedules
            if (_config.Rotation.ModeSchedules)
            {
                // Parse schedule entries
                foreach (ScheduleEntry entry in _config.Rotation.Schedule)
                {
                    // Schedule the mode change
                    DateTime targetTime = DateTime.Parse(entry.Time); // Parse the time string

                    // Calculate delay until target time (considering if it's passed today)
                    TimeSpan delay = targetTime - DateTime.Now;
                    if (delay.TotalMilliseconds <= 0)
                    {
                        // If target time has already passed, calculate for the next day
                        delay = delay.Add(TimeSpan.FromDays(1));
                    }

                    // Create timer
                    new Timer((float)delay.TotalSeconds, () =>
                    {
                        // Trigger schedule
                        _serverManager.TriggerScheduleChange(entry);

                        // Update delay for the next occurrence (tomorrow)
                        delay = targetTime.AddDays(1) - DateTime.Now;
                        
                    }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
                }
            }

            if(_config.Rotation.WhenServerEmpty)
            {
                _plugin.RegisterEventHandler<EventPlayerConnect>((@event, info) =>
                {  
                    if(Extensions.ValidPlayerCount(false) == 1)
                    {
                        if (!_timeLimitManager.UnlimitedTime)
                        {
                            _timeLimitManager.EnforceTimeLimit(false);
                        }
                    }
                    return HookResult.Continue;
                }, HookMode.Post);

                _plugin.RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
                {  
                    if(Extensions.ValidPlayerCount(false) == 0)
                    {
                        if(!Extensions.IsHibernationEnabled())
                        {
                            Server.ExecuteCommand("sv_hibernate_when_empty false");
                        }

                        if(!_timeLimitManager.UnlimitedTime)
                        {
                            _timeLimitManager.EnforceTimeLimit(true);
                        }
                    }
                    return HookResult.Continue;
                }, HookMode.Post);
            }
        }    
    }
}