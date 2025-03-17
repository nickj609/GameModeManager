// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.CrossCutting
{
    // Define class
    public class GameRules : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        CCSGameRules? _gameRules = null;
        public float GameStartTime => _gameRules?.GameStartTime ?? 0;
        public bool WarmupRunning => _gameRules?.WarmupPeriod ?? false;
        public int TotalRoundsPlayed => _gameRules?.TotalRoundsPlayed ?? 0;
        public bool HasMatchStarted => _gameRules?.HasMatchStarted ?? false;
        
        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            SetGameRulesAsync();
            plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
            plugin.RegisterEventHandler<EventRoundAnnounceWarmup>(OnAnnounceWarmup);
        }

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            SetGameRulesAsync();
        }

        // Define methods to set game rules
        public void SetGameRules() => _gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
        public void SetGameRulesAsync()
        {
            _gameRules = null;
            new Timer(1.0F, () =>
            {
                SetGameRules();
            });
        }

        // Define event handlers
        public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            SetGameRules();
            return HookResult.Continue;
        }

        public HookResult OnAnnounceWarmup(EventRoundAnnounceWarmup @event, GameEventInfo info)
        {
            SetGameRules();
            return HookResult.Continue;
        }
    }
}