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
                    // Parse the time string
                    DateTime targetTime = DateTime.Parse(entry.Time);

                    // Calculate delay until target time (considering if it's passed today)
                    TimeSpan delay = targetTime - DateTime.Now;
                    if (delay.TotalMilliseconds <= 0)
                    {
                        delay = delay.Add(TimeSpan.FromDays(1));
                    }

                    // Create timer
                    new Timer((float)delay.TotalSeconds, () =>
                    {
                        _serverManager.TriggerScheduleChange(entry);
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
                if(Extensions.IsServerEmpty())
                {
                    if(!Extensions.IsHibernationEnabled())
                    {
                        Server.ExecuteCommand("sv_hibernate_when_empty false");
                    }

                    // Check if custom time limit set
                    if(_config.Rotation.EnforceCustomTimeLimit)
                    {
                        _timeLimitManager.EnforceTimeLimit(_config.Rotation.CustomTimeLimit);
                    }
                    else
                    {
                        // Check if map has an unlimited time set
                        if(_timeLimitManager.TimeRemaining != 0)
                        {
                            _timeLimitManager.EnforceTimeLimit();
                        }
                    }
                }
            }
            return HookResult.Continue;
        }
    }
}