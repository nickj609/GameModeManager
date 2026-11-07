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
        // Define class constructor
        public GameModeApi(PluginState pluginState, ServerManager serverManager, WarmupManager warmupManager)
        {
            State = new GameState(pluginState);
            Control = new GameModeController(serverManager);
            Warmup = new WarmupController(warmupManager);
        }

        // Define class properties
        public IGameState State { get; }
        public IWarmupControl Warmup { get; }
        public IGameModeControl Control { get; }

        // Define child classes
        private class GameState : IGameState
        {
            // Define class dependencies
            private readonly PluginState _pluginState;

            // Define class constructor
            public GameState(PluginState pluginState) => _pluginState = pluginState;

            // Define class properties
            public IMap CurrentMap => _pluginState.Game.CurrentMap;
            public IMode WarmupMode => _pluginState.Game.WarmupMode;
            public IMode CurrentMode => _pluginState.Game.CurrentMode;
            public Dictionary<string, IMap> Maps => _pluginState.Game.Maps;
            public bool WarmupScheduled => _pluginState.Game.WarmupScheduled;
            public Dictionary<string, IMode> Modes => _pluginState.Game.Modes;
            public Dictionary<string, ISetting> Settings => _pluginState.Game.Settings;
            public Dictionary<string, IMapGroup> MapGroups => _pluginState.Game.MapGroups;
        }

        private class GameModeController : IGameModeControl
        {
            // Define class dependencies
            private readonly ServerManager _serverManager;

            // Define class constructor
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
            
            // Define class constructor
            public WarmupController(WarmupManager warmupManager) => _warmupManager = warmupManager;

            // Define class methods
            public bool ScheduleWarmup(IMode mode) => _warmupManager.ScheduleWarmup(mode.Name);
        }
    }
}