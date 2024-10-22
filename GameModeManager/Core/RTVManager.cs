// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class RTVManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private PluginState _pluginState;
        private Config _config = new Config();
        private ILogger<WarmupManager> _logger;

        // Define class instance
        public RTVManager(PluginState pluginState, ILogger<WarmupManager> logger)
        {
            _logger = logger;
            _pluginState = pluginState;
        }

        // Define reusable method to enable RTV
        public void EnableRTV()
        {
            _logger.LogInformation($"Enabling RTV...");
            Server.ExecuteCommand($"css_plugins load {_config.RTV.Plugin}");
            _pluginState.RTVEnabled = true;

            _logger.LogInformation($"Disabling rotations...");
            _pluginState.RotationsEnabled = false;
        }
        // Define reusable method to disable RTV
        public void DisableRTV()
        {
            _logger.LogInformation($"Disabling RTV...");
            Server.ExecuteCommand($"css_plugins unload {_config.RTV.Plugin}");
            _pluginState.RTVEnabled = false;

            _logger.LogInformation($"Enabling rotations...");
            _pluginState.RotationsEnabled = true;
        }
    }
}