// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using GameModeManager.Shared.Models;
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
        // Define class dependencies
        private Plugin? _plugin;
        private Timer? _rotationTimer;
        private PluginState _pluginState;
        private ServerManager _serverManager;
        private MapChooserBridge _mapChooser;
        private Config _config = new Config();

        // Define class constructor
        public RotationManager(ServerManager serverManager, PluginState pluginState, MapChooserBridge mapChooser)
        {
            _mapChooser = mapChooser;
            _pluginState = pluginState;
            _serverManager = serverManager;
        }

        // Define class properties
        private bool _mapStarted;
        private bool _modeDueLastMap;
        private IMode? _lastMode;
        
        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
            _pluginState.Game.RotationsEnabled = _config.Rotation.Enabled;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        { 
            _plugin = plugin;

            // Register event handlers
            plugin.RegisterEventHandler<EventCsWinPanelMatch>(EventCsWinPanelMatchHandler);
            plugin.RegisterEventHandler<EventPlayerDisconnect>(EventPlayerDisconnectHandler);
            plugin.RegisterEventHandler<EventPlayerConnectFull>(EventPlayerConnectFullHandler);

            // Create mode schedules
            if (_config.Rotation.ModeSchedules)
            {
                foreach (ScheduleEntry entry in _config.Rotation.Schedule)
                    StartSchedule(entry);
            }
        }

        // Define on map start behavior
        // A map left early (RTV, an admin's map change) isn't a rotation, so TriggerRotation didn't count it. It counts when
        // CountSkippedMaps is on, and always when it changed the mode on the map that was due to: that change was the rotation.
        public void OnMapStart(string map)
        {
            if (_mapStarted && !_pluginState.Game.RotationStarted)
            {
                bool modeChanged = _lastMode != null && !_pluginState.Game.CurrentMode.Equals(_lastMode);
                if (_config.Rotation.CountSkippedMaps || (_modeDueLastMap && modeChanged))
                    _pluginState.Game.MapRotations++;
            }

            _mapStarted = true;
            _pluginState.Game.RotationStarted = false;
            _lastMode = _pluginState.Game.CurrentMode;
            _modeDueLastMap = _mapChooser.ModeChangeDue();
        }

        // Define class methods
        private void StartSchedule(ScheduleEntry entry)
        {
            TimeSpan delay = DateTime.Parse(entry.Time) - DateTime.Now;

            if (delay.TotalMilliseconds <= 0)
                delay = delay.Add(TimeSpan.FromDays(1));

            _plugin?.AddTimer((float)delay.TotalSeconds, () =>
            {
                _serverManager.TriggerScheduleChange(entry);
                StartSchedule(entry);
            });
        }

        // Define event handlers
        public HookResult EventCsWinPanelMatchHandler(EventCsWinPanelMatch @event, GameEventInfo info)
        {  
            _serverManager.TriggerRotation();
            return HookResult.Continue;
        }

        public HookResult EventPlayerConnectFullHandler(EventPlayerConnectFull @event, GameEventInfo info)
        {
            if (PlayerExtensions.ValidPlayerCount(false) > 0)
            {
                _rotationTimer?.Kill();
                _rotationTimer = null;
            }

            return HookResult.Continue;
        }

        public HookResult EventPlayerDisconnectHandler(EventPlayerDisconnect @event, GameEventInfo info)
        {  
            if(_config.Rotation.WhenServerEmpty)
            {
                if(ServerExtensions.IsServerEmpty())
                {
                    // Disable server hibernation
                    if(ServerExtensions.IsHibernationEnabled())
                        Server.ExecuteCommand("sv_hibernate_when_empty false");

                    // Create timer
                    _rotationTimer ??= _plugin?.AddTimer(_config.Rotation.CustomTimeLimit, _serverManager.TriggerRotation, TimerFlags.REPEAT);
                }
            }
            return HookResult.Continue;
        }
    }
}