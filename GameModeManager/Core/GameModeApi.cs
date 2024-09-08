// Included libraries
using GameModeManager.Shared;
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class GameModeApi : IGameModeApi, IPluginDependency<Plugin, Config>
    {
            // Define dependencies
            private Plugin? _plugin;
            private PluginState _pluginState;
            private StringLocalizer _localizer;
            private Config _config = new Config();
            private ILogger<IGameModeApi> _logger;

            // Define globals
            public string CurrentMode 
            { 
                get
                {
                    return _pluginState.CurrentMode.Name;
                }
            } 
            public string CurrentMap
            { 
                get
                {
                    return _pluginState.CurrentMap.DisplayName;
                }
            }
            public string NextMode
            { 
                get
                {
                    return _pluginState.NextMode.Name;
                }
            }
            public string NextMap
            { 
                get
                {
                    return _pluginState.NextMap.DisplayName;
                }
            }
            

            // Define class instance
            public GameModeApi(PluginState pluginState, StringLocalizer localizer, ILogger<IGameModeApi> logger)
            {
                _logger = logger;
                _localizer = localizer;
                _pluginState = pluginState;
            }

            // Load config
            public void OnConfigParsed(Config config)
            {
                _config = config;
            }

            // Define on load behavior
            public void OnLoad(Plugin plugin)
            {
                _plugin = plugin;
            }

            // Change mode api handler
            public void ChangeMode(string name, float delay)
            {
                // Find mode
                Mode? mode = _pluginState.Modes.FirstOrDefault(m => m.Name == name);

                // Change mode
                if (mode != null && _plugin != null)
                {
                    ServerManager.ChangeMode(mode, _config, _plugin, _pluginState, delay);
                }
                else
                {
                    _logger.LogWarning($"Game Mode API: Mode {name} not found.");
                }
            }

            public void ChangeMode(string modeName, string mapName, float delay)
            {
                // Find mode
                Mode? mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(modeName, StringComparison.OrdinalIgnoreCase));

                // Find map
                Map? map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(mapName, StringComparison.OrdinalIgnoreCase));

                // Change mode
                if (mode != null && map != null && _plugin != null)
                {
                    ServerManager.ChangeMode(mode, map, _config, _plugin, _pluginState, delay);
                }
                else
                {
                    _logger.LogWarning($"Game Mode API: Mode {modeName} or Map {mapName} not found.");
                }
            }

            // Change map api handler
            public void ChangeMap(string name)
            {
                // Find map
                Map? map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                
                // Change mode
                if (map != null)
                {
                    ServerManager.ChangeMap(map, _config, _pluginState);
                }
                else
                {
                    _logger.LogWarning($"Game Mode API: Map {name} not found.");
                }
            }

            // Trigger rotation api handler
            public void TriggerRotation()
            {
                if (_plugin != null)
                {
                    ServerManager.TriggerRotation(_plugin, _config, _pluginState, _logger, _localizer);
                }
            }

            // Trigger rotation api handler
            public void SetWarmupMode(string name)
            {
                if(_plugin != null)
                {
                    // Find warmup mode
                    Mode? warmupMode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                    // Set warmup mode
                    if(warmupMode == null)
                    {
                        _pluginState.WarmupModeEnabled = false;
                        _logger.LogWarning($"Game Mode API: Warmup mode {name} not found.");   
                    } 
                    else
                    {
                        _pluginState.WarmupModeEnabled = true;
                        string _warmupConfig = _config.Warmup.Folder + "/" + warmupMode.Config;
                        _pluginState.WarmupMode = new Mode(warmupMode.Name, _warmupConfig, warmupMode.DefaultMap.Name, warmupMode.MapGroups);
                    }         
                }
            }

            // Trigger rotation api handler
            public void SetWarmupTime(float time)
            {
                _pluginState.WarmupTime = time;
            }
    }
}