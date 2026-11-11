// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.Common
{
    // Define class
    public class GameRules : IPluginDependency<Plugin, Config>
    {
        // Define class properties
        CCSGameRules? _gameRules = null;
        private bool _endRoundFailed; // The game's round-end function failed once: not offered again until a reload
        public float GameStartTime => _gameRules?.GameStartTime ?? 0;
        public bool WarmupRunning => _gameRules?.WarmupPeriod ?? false;
        public int TotalRoundsPlayed => _gameRules?.TotalRoundsPlayed ?? 0;
        public bool HasMatchStarted => _gameRules?.HasMatchStarted ?? false;
        public bool FreezePeriod => _gameRules?.FreezePeriod ?? false;
        public bool MatchPaused => _gameRules?.MatchWaitingForResume ?? false;
        public bool TimeoutRunning => (_gameRules?.TerroristTimeOutActive ?? false) || (_gameRules?.CTTimeOutActive ?? false);
        public int MaxRounds => ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>() ?? 0;

        // Ending this round wouldn't end the match: a match is running and it isn't the last round (mp_maxrounds 0 has no last one).
        // One-round modes (surf, bhop, deathmatch) set mp_maxrounds 1, so there ending the round would end the map.
        public bool CanEndRound => !_endRoundFailed && _gameRules != null && !WarmupRunning && (MaxRounds <= 0 || TotalRoundsPlayed + 1 < MaxRounds);
        
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

        // Ends the round now as a draw, like the endround command but without sv_cheats: nobody scores, players keep what they have
        // left, and it counts as a round played. False when it can't be done, or when the game's function wasn't found (after a
        // CS2 update, until CounterStrikeSharp's gamedata catches up).
        public bool EndRoundAsDraw()
        {
            if (!CanEndRound)
                return false;
            try
            {
                float delay = ConVar.Find("mp_round_restart_delay")?.GetPrimitiveValue<float>() ?? 5f;
                _gameRules!.TerminateRound(delay, RoundEndReason.RoundDraw);
                return true;
            }
            catch (Exception)
            {
                _endRoundFailed = true;
                return false;
            }
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