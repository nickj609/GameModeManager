// Included libraries
using GameModeManager.Core;
using GameModeManager.Shared;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Services
{
    // Define class
    public class GameModeApi : IGameModeApi, IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private readonly PluginState _pluginState;
        private readonly ServerManager _serverManager;
        private readonly WarmupManager _warmupManager;

        // Define class instance
        public GameModeApi(PluginState pluginState, ServerManager serverManager, WarmupManager warmupManager)
        {
            _pluginState = pluginState;
            _serverManager = serverManager;
            _warmupManager = warmupManager;
            State = new GameState(_pluginState);
            Control = new GameModeController(_serverManager);
            Warmup = new WarmupController(_warmupManager);
        }

        // Define class properties
        public IGameState State { get; }
        public IGameModeControl Control { get; }
        public IWarmupControl Warmup { get; }

        // Define child classes
        private class GameState : IGameState
        {
            // Define class dependencies
            private readonly PluginState _pluginState;
            
            // Define class instance
            public GameState(PluginState pluginState) => _pluginState = pluginState;

            // Define class properties
            public List<IMap> Maps => _pluginState.Maps;
            public List<IMode> Modes => _pluginState.Modes;
            public IMap CurrentMap => _pluginState.CurrentMap;
            public IMode WarmupMode => _pluginState.WarmupMode;
            public IMode CurrentMode => _pluginState.CurrentMode;
            public List<ISetting> Settings => _pluginState.Settings;
            public List<IMapGroup> MapGroups => _pluginState.MapGroups;
            public bool WarmupScheduled => _pluginState.WarmupScheduled;
        }

        private class GameModeController : IGameModeControl
        {
            // Define class dependencies
            private readonly ServerManager _serverManager;

            // Define class instance
            public GameModeController(ServerManager serverManager) => _serverManager = serverManager;

            // define class methods
            public void TriggerRotation() => _serverManager.TriggerRotation();
            public void ChangeMode(IMode mode) => _serverManager.ChangeMode(mode);
            public void ChangeMap(IMap map, int delay) => _serverManager.ChangeMap(map, delay);
        }

        private class WarmupController : IWarmupControl
        {
            // Define class dependencies
            private readonly WarmupManager _warmupManager;
            
            // Define class instance
            public WarmupController(WarmupManager warmupManager) => _warmupManager = warmupManager;

            // Define class methods
            public bool ScheduleWarmup(IMode mode) => _warmupManager.ScheduleWarmup(mode.Name);
        }
    }
}