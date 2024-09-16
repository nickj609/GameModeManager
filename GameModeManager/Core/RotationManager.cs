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
            // Register event handlers
            plugin.RegisterEventHandler<EventCsWinPanelMatch>(EventCsWinPanelMatchHandler);
            plugin.RegisterEventHandler<EventPlayerDisconnect>(EventPlayerDisconnectHandler);
            plugin.RegisterEventHandler<EventPlayerConnectFull>(EventPlayerConnectFullHandler);

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
        }    

        // Define event game end handler
        public HookResult EventCsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info)
        {  
            _serverManager.TriggerRotation();
            return HookResult.Continue;
        }

        // Define event player connect full handler
        public HookResult EventPlayerConnectFullHandler(EventPlayerConnectFull @event, GameEventInfo info)
        {
            // Check if server empty
             if(!Extensions.IsServerEmpty())
            {
                _timeLimitManager.RemoveTimeLimit();
            }
            return HookResult.Continue;
        }

        // Define event player disconnect handler
        public HookResult EventPlayerDisconnectHandler(EventPlayerDisconnect @event, GameEventInfo info)
        {  
            // Check if rotation on server empty is enabled
            if(_config.Rotation.WhenServerEmpty)
            {
                // Check if server empty
                if(Extensions.IsServerEmpty())
                {
                    // Check if server is hibernating
                    if(!Extensions.IsHibernationEnabled())
                    {
                        // Disable hibernation
                        Server.ExecuteCommand("sv_hibernate_when_empty false");
                    }

                    // Check if custom time limit set
                    if(_config.Rotation.EnforceCustomTimeLimit)
                    {
                        // Enforce custom time limit
                        _timeLimitManager.EnforceTimeLimit(_config.Rotation.CustomTimeLimit);
                    }
                    else
                    {
                        // Check if map has an unlimited time set
                        if(_timeLimitManager.TimeRemaining != 0)
                        {
                            // Enforce time limit
                            _timeLimitManager.EnforceTimeLimit();
                        }
                    }
                }
            }
            return HookResult.Continue;
        }
    }
}