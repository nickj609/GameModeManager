// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager
{
    // Define class
    public class RotationManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin _plugin;
        private ILogger _logger;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private Config _config = new Config();

        // Define class instance
        public RotationManager(Plugin plugin, PluginState pluginState, StringLocalizer stringLocalizer, ILogger logger)
        {
            _plugin = plugin;
            _logger = logger;
            _pluginState = pluginState;
            _localizer = stringLocalizer;
        }
        
        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;

             // Parse schedule entries
            foreach (var entry in _config.GameModes.Schedule.Entries)
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

                // Convert TimeSpan to milliseconds for timer constructor
                int delayInMilliseconds = (int)delay.TotalMilliseconds;

                // Set the timer state (can be the ScheduleEntry itself)
                entry.TimerState = entry;

                // Create a Timer callback delegate (without capturing variables)
                TimerCallback callback = TriggerScheduleChange;

                // Schedule the timer callback
                Timer timer = new Timer(callback, entry.TimerState , delayInMilliseconds, Timeout.Infinite);
            }
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        { 
            _plugin = plugin;
            _logger = plugin.Logger;
            _localizer = new StringLocalizer(plugin.Localizer);

            // Define event game end handler
            _plugin.RegisterEventHandler<EventCsWinPanelMatch>((@event, info) =>
            {  
                TriggerRotation();
                return HookResult.Continue;
            }, HookMode.Post);
        }

        public void TriggerRotation()
        {  
            // Check if RTV is disabled in config and if so enable randomization
            if(!_pluginState.RTVEnabled)
            {
                // Check if game mode rotation is enabled
                if(_plugin != null && _config.GameModes.Rotation && _pluginState.MapRotations % _config.GameModes.Interval == 0)
                {  
                    _logger.LogInformation("Game has ended. Picking random game mode...");
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("Game has ended. Changing mode..."));  
                    // Get random game mode
                    Random _rnd = new Random();
                    int _randomIndex = _rnd.Next(0, _pluginState.Modes.Count); 
                    Mode _randomMode = _pluginState.Modes[_randomIndex];

                    // Change mode
                    ServerManager.ChangeMode(_randomMode,_plugin , _pluginState, _config.MapGroups.Delay);

                }
                else
                {
                    _logger.LogInformation("Game has ended. Picking random map from current mode...");
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("Game has ended. Changing map..."));  

                    // Get a random map
                    Random _rnd = new Random();
                    int _randomIndex = _rnd.Next(0, _pluginState.CurrentMode.Maps.Count); 
                    Map _randomMap = _pluginState.CurrentMode.Maps[_randomIndex];

                    // Change map
                    ServerManager.ChangeMap(_randomMap);
                }
            }
            _pluginState.MapRotations++;
        }

        private void TriggerScheduleChange(object? state)
        {
            // Cast the state object back to ScheduleEntry
            ScheduleEntry? entry = state as ScheduleEntry;

            if (entry != null)
            {
                // Find the mode
                Mode? _mode = _pluginState.Modes.FirstOrDefault(m => m.Name == entry.Mode);

                if (_mode != null && _config != null && _plugin != null)
                {
                    // Change the mode based on the entry (replace with your logic)
                    ServerManager.ChangeMode(_mode, _plugin, _pluginState, _config.MapGroups.Delay);
                }
            }
        }

        // Define on map start behavior
        public void OnMapStart(Plugin plugin)
        {
            if (ServerManager.IsHibernationEnabled() && !_pluginState.RTVEnabled)
            {
                Server.ExecuteCommand("sv_hibernate_when_empty false");
            }
        }
    }
}