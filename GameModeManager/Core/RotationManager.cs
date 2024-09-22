// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using TimerFlags = CounterStrikeSharp.API.Modules.Timers.TimerFlags;
 
// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class RotationManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        bool _rotationEnabled = false;
        private Timer? _rotationTimer;
        private ServerManager _serverManager;
        private Config _config = new Config();

        // Define class instance
        public RotationManager(ServerManager serverManager)
        {
            _serverManager = serverManager;
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
                        
                    },TimerFlags.REPEAT);
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
            _rotationTimer?.Kill();
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
                    // Disable server hibernation
                    if(!Extensions.IsHibernationEnabled())
                    {
                        Server.ExecuteCommand("sv_hibernate_when_empty false");
                    }

                    // Create timer
                    if(!_rotationEnabled)
                    {
                        _rotationEnabled = true;
                        _rotationTimer = new Timer(_config.Rotation.CustomTimeLimit, () =>
                        {
                            _serverManager.TriggerRotation();
                            _rotationEnabled = false;
                        },TimerFlags.REPEAT);
                    }
                }
            }
            return HookResult.Continue;
        }
    }
}