// Included libraries
using GameModeManager.Shared;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Api
{
    // Define class
    // The capability game_mode:api. Reads and changes go through the same code as GameModeManager's own menus and commands.
    public class GameModeApi : IGameModeApi, IPluginDependency<Plugin, Config>
    {
        // Define class constructor
        public GameModeApi(PluginState pluginState, ServerManager serverManager, WarmupManager warmupManager, SettingStates settingStates,
            SettingMenus settingMenus, ActionMenus actionMenus, RoundBackups roundBackups, MapChooserBridge mapChooser, VoteService voteService,
            GameModeEvents events)
        {
            State = new GameState(pluginState, settingStates, mapChooser, voteService);
            Control = new GameModeController(pluginState, serverManager, settingMenus, actionMenus, roundBackups, mapChooser, voteService);
            Warmup = new WarmupController(warmupManager);
            Events = events;
        }

        // Define class properties
        public IGameState State { get; }
        public IWarmupControl Warmup { get; }
        public IGameModeControl Control { get; }
        public IGameModeEvents Events { get; }

        // Define child classes
        private class GameState : IGameState
        {
            // Define class dependencies
            private readonly PluginState _pluginState;
            private readonly SettingStates _settingStates;
            private readonly MapChooserBridge _mapChooser;
            private readonly VoteService _voteService;

            // Define class constructor
            public GameState(PluginState pluginState, SettingStates settingStates, MapChooserBridge mapChooser, VoteService voteService)
            {
                _pluginState = pluginState;
                _settingStates = settingStates;
                _mapChooser = mapChooser;
                _voteService = voteService;
            }

            // Define class properties
            public IMap CurrentMap => _pluginState.Game.CurrentMap;
            public IMode WarmupMode => _pluginState.Game.WarmupMode;
            public IMode CurrentMode => _pluginState.Game.CurrentMode;
            public Dictionary<string, IMap> Maps => _pluginState.Game.Maps;
            public bool WarmupScheduled => _pluginState.Game.WarmupScheduled;
            public Dictionary<string, IMode> Modes => _pluginState.Game.Modes;
            public Dictionary<string, ISetting> Settings => _pluginState.Game.Settings;
            public Dictionary<string, IMapGroup> MapGroups => _pluginState.Game.MapGroups;
            public IReadOnlyList<IValueSetting> ValueSettings => _pluginState.Game.ValueSettings.Values.ToList();
            public IReadOnlyList<IGameAction> Actions => _pluginState.Game.Actions;
            public string? NextMap => _mapChooser.NextMapName();
            public bool VoteInProgress => _voteService.InProgress || _mapChooser.VoteInProgress();
            public bool WarmupRunning => _pluginState.Game.WarmupRunning;

            // Define class methods
            public SettingState GetSettingState(ISetting setting) => _settingStates.Get(setting);

            public string? GetValue(IValueSetting setting)
            {
                return setting is ValueSetting own && _settingStates.ActiveValue(own) is int index ? own.Values[index].Label : null;
            }
        }

        private class GameModeController : IGameModeControl
        {
            // Define class dependencies
            private readonly PluginState _pluginState;
            private readonly ServerManager _serverManager;
            private readonly SettingMenus _settingMenus;
            private readonly ActionMenus _actionMenus;
            private readonly RoundBackups _roundBackups;
            private readonly MapChooserBridge _mapChooser;
            private readonly VoteService _voteService;

            // Define class constructor
            public GameModeController(PluginState pluginState, ServerManager serverManager, SettingMenus settingMenus, ActionMenus actionMenus,
                RoundBackups roundBackups, MapChooserBridge mapChooser, VoteService voteService)
            {
                _pluginState = pluginState;
                _serverManager = serverManager;
                _settingMenus = settingMenus;
                _actionMenus = actionMenus;
                _roundBackups = roundBackups;
                _mapChooser = mapChooser;
                _voteService = voteService;
            }

            // define class methods
            public void TriggerRotation() => _serverManager.TriggerRotation();
            public void ChangeMode(IMode mode) => _serverManager.ChangeMode(mode);
            public void ChangeMap(IMap map, int delay) => _serverManager.ChangeMap(map, delay);
            public bool SetNextMap(IMap map) => _mapChooser.SetNextMap(map);
            public bool SetNextMode(IMode mode) => _mapChooser.SetNextMode(mode);
            public bool SetSetting(ISetting setting, bool enable) => _settingMenus.ApplyFromApi(setting, enable);
            public bool SetValue(IValueSetting setting, string value) => setting is ValueSetting own && _settingMenus.ApplyValueFromApi(own, value);
            public void RestartMatch() => Server.ExecuteCommand("mp_restartgame 1");

            public void RunAction(IGameAction action)
            {
                if (action is GameAction own)
                    _actionMenus.RunFromApi(own);
                else
                    Server.ExecuteCommand($"exec {action.File}");
            }

            public bool RestoreRound(int round)
            {
                RoundBackups.Backup? backup = _actionMenus.Backups().FirstOrDefault(backup => backup.Round == round);
                return backup != null && _roundBackups.Restore(backup);
            }

            public bool StartVote(CCSPlayerController caller, string question, Action onPassed)
            {
                // The other plugin's code runs inside GameModeManager's vote: a failure there is logged, not thrown
                return _voteService.StartYesNo(caller, question, () =>
                {
                    try
                    {
                        onPassed();
                    }
                    catch (Exception error)
                    {
                        Console.WriteLine($"[GameModeManager] A vote's onPassed from another plugin failed: {error.Message}");
                    }
                });
            }
        }

        private class WarmupController : IWarmupControl
        {
            // Define class dependencies
            private readonly WarmupManager _warmupManager;

            // Define class constructor
            public WarmupController(WarmupManager warmupManager) => _warmupManager = warmupManager;

            // Define class methods
            public bool ScheduleWarmup(IMode mode) => _warmupManager.ScheduleWarmup(mode.Name);
            public void EndWarmup() => _warmupManager.EndWarmup();
        }
    }
}
