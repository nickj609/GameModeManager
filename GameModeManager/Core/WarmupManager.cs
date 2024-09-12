// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class WarmupManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin? _plugin;
        private PluginState _pluginState;
        private Config _config = new Config();
        private TimeLimitManager _timeLimitManager;

        // Define class instance
        public WarmupManager(PluginState pluginState, TimeLimitManager timeLimitManager)
        {
            _pluginState = pluginState;
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
            plugin.RegisterEventHandler<EventWarmupEnd>(OnWarmupEnd);
            plugin.RegisterEventHandler<EventRoundAnnounceWarmup>(OnAnnounceWarmup);
        }

        // Define on warmup end behavior
        public HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo info)
        {
            if(_pluginState.WarmupStarted == true && _plugin != null)
            {
                // Disable warmup started flag
                _pluginState.WarmupStarted = false;
                _timeLimitManager.EnforceCustomTimeLimit(_plugin, false, _pluginState.WarmupTime);

                // Check if modes are the same
                if (_pluginState.CurrentMode.Name.Equals(_pluginState.NextMode.Name, StringComparison.OrdinalIgnoreCase))
                {
                    // Change map
                    ServerManager.ChangeMap(_pluginState.NextMap, _config, _plugin, _pluginState);
                }
                else // If not
                {
                    // Change mode with map preference
                    ServerManager.ChangeMode(_pluginState.NextMode, _pluginState.NextMap, _config, _plugin, _pluginState, 0);
                }
            }
            return HookResult.Continue;
        }

        // Define on warmup start behavior
        public HookResult OnAnnounceWarmup(EventRoundAnnounceWarmup @event, GameEventInfo info)
        {
            if(_pluginState.WarmupStarted == true && _plugin != null)
            {
                _timeLimitManager.EnforceCustomTimeLimit(_plugin, true, _pluginState.WarmupTime);
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