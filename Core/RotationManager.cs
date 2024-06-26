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
        private Plugin? _plugin;
        private ILogger<RotationManager> _logger;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private Config _config = new Config();

        // Define class instance
        public RotationManager(PluginState pluginState, StringLocalizer stringLocalizer, ILogger<RotationManager> logger)
        {
            _logger = logger;
            _pluginState = pluginState;
            _localizer = stringLocalizer;
        }
        
        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;

             // Parse schedule entries
            foreach (var entry in _config.Rotation.Schedule)
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

                // Create a Timer callback delegate (without capturing variables)
                TimerCallback callback = TriggerScheduleChange;

                // Schedule the timer callback
                Timer timer = new Timer(callback, entry , delayInMilliseconds, Timeout.Infinite);
            }
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        { 
            _plugin = plugin;

            // Define event game end handler
            _plugin.RegisterEventHandler<EventCsWinPanelMatch>((@event, info) =>
            {  
                TriggerRotation();
                return HookResult.Continue;
            }, HookMode.Post);
        }

        // Define method to trigger mode and map rotations
        public void TriggerRotation()
        {  
            // Check if RTV is disabled in config and if so enable randomization
            if(!_pluginState.RTVEnabled)
            {
                // Check if game mode rotation is enabled
                if(_plugin != null && _config.Rotation.ModeRotation && _pluginState.MapRotations % _config.Rotation.ModeInterval == 0)
                {  
                    // Log information
                    _logger.LogInformation("Game has ended. Picking random game mode...");
            
                    // Get random game mode
                    Random _rnd = new Random();
                    int _randomIndex = _rnd.Next(0, _pluginState.Modes.Count); 
                    Mode _randomMode = _pluginState.Modes[_randomIndex];

                    // Change mode
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("Game has ended. Changing mode..."));  
                    ServerManager.ChangeMode(_randomMode,_plugin , _pluginState, _config.GameModes.Delay);
                }
                else
                {
                    // Define random map
                    Map _randomMap;    

                    // Choose random map           
                    if (_config.Rotation.Cycle == 2)
                    {
                        List<Map> _mapList = new List<Map>();

                        foreach (string mapGroup in _config.Rotation.MapGroups)
                        {
                            MapGroup? _mapGroup = _pluginState.MapGroups.FirstOrDefault(m => m.Name == mapGroup);

                            if (_mapGroup != null)
                            {
                                foreach (Map _map in _mapGroup.Maps)
                                {
                                    _mapList.Add(_map);
                                }
                            }
                        } 

                        // Get a random map from current game mode
                        Random _rnd = new Random();
                        int _randomIndex = _rnd.Next(0, _mapList.Count); 
                        _randomMap = _mapList[_randomIndex];

                    }
                    else if (_config.Rotation.Cycle == 1)
                    {
                        // Log message
                        _logger.LogInformation("Game has ended. Picking random map from all maps list...");

                        // Get a random map from current game mode
                        Random _rnd = new Random();
                        int _randomIndex = _rnd.Next(0, _pluginState.CurrentMode.Maps.Count); 
                        _randomMap = _pluginState.CurrentMode.Maps[_randomIndex];
                    }
                    else
                    {
                        // Log message
                        _logger.LogInformation("Game has ended. Picking random map from current mode...");

                        // Get a random map from all registered maps
                        Random _rnd = new Random();
                        int _randomIndex = _rnd.Next(0, _pluginState.Maps.Count); 
                        _randomMap = _pluginState.Maps[_randomIndex];
                    }

                    // Change map
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("Game has ended. Changing map..."));
                    ServerManager.ChangeMap(_randomMap);
                }
            }
            _pluginState.MapRotations++;
        }

        // Define method to trigger schedule change
        private void TriggerScheduleChange(object? state)
        {
            // Cast the state object back to ScheduleEntry
            ScheduleEntry? entry = state as ScheduleEntry;

            if (entry != null)
            {
                // Find the mode
                Mode? _mode = _pluginState.Modes.FirstOrDefault(m => m.Name == entry.Mode);

                if (_mode != null && _plugin != null)
                {
                    // Change the mode based on the entry (replace with your logic)
                    ServerManager.ChangeMode(_mode, _plugin, _pluginState, _config.GameModes.Delay);
                }
            }
        }

        // Define on map start behavior
        public void OnMapStart(Plugin plugin)
        {
            // Disable server hibernation
            if (ServerManager.IsHibernationEnabled() && !_pluginState.RTVEnabled)
            {
                Server.ExecuteCommand("sv_hibernate_when_empty false");
            }
        }
    }
}