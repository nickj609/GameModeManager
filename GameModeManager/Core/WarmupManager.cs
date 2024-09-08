// Included libraries
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class WarmupManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin? _plugin;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private MapGroupManager _mapGroupManager;
        private TimeLimitManager _timeLimitManager;

        // Define class instance
        public WarmupManager(StringLocalizer localizer, PluginState pluginState, TimeLimitManager timeLimitManager, MapGroupManager mapGroupManager)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _mapGroupManager = mapGroupManager;
            _timeLimitManager = timeLimitManager;
            
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Load dependencies
        public void OnLoad(Plugin plugin)
        { 
            _plugin = plugin;
            _localizer = new StringLocalizer(plugin.Localizer);
            plugin.RegisterEventHandler<EventWarmupEnd>(OnWarmupEnd);
            plugin.RegisterEventHandler<EventRoundAnnounceWarmup>(OnAnnounceWarmup);

            // Create map group list
            List<MapGroup> mapGroups = _mapGroupManager.CreateMapGroupList(_config.Warmup.Default.MapGroups);

            // Set warmup mode  
            _pluginState.WarmupMode = new Mode(_config.Warmup.Default.Name, _config.Warmup.Default.Config, _config.Warmup.Default.DefaultMap, mapGroups);
        }

        // Define on warmup end behavior
        public HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo info)
        {
            if(_pluginState.WarmupStarted == true)
            {
                // Disable warmup started flag
                _pluginState.WarmupStarted = false;

                // Check if modes are the same
                if (_pluginState.CurrentMode.Name == _pluginState.NextMode.Name)
                {
                    // Change map
                    ServerManager.ChangeMap(_pluginState.NextMap, _config, _pluginState);
                }
                else // If not
                {
                    // Change mode with map preference
                    if (_plugin != null)
                    {
                        ServerManager.ChangeMode(_pluginState.NextMode, _pluginState.NextMap, _config, _plugin, _pluginState, 0);
                    }
                }
            }
            
            return HookResult.Continue;
        }

        // Define on warmup start behavior
        public HookResult OnAnnounceWarmup(EventRoundAnnounceWarmup @event, GameEventInfo info)
        {
            if(_pluginState.WarmupStarted == true && _plugin != null)
            {
                _timeLimitManager.EnforceCustomTimeLimit(_plugin, _config.Warmup.Time);
            }
            return HookResult.Continue;
        }

        // Define on map start behavior
        public void OnMapStart (string map)
        {
            if (_pluginState.WarmupStarted == true)
            {
                Server.ExecuteCommand($"mp_warmuptime {_config.Warmup.Time}; mp_warmuptime_all_players_connected {_config.Warmup.Time}; mp_warmup_start");
            }
        }
    }
}