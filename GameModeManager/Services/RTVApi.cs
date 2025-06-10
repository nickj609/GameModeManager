// Included libraries
using GameModeManager.Core;
using GameModeManager.Shared;
using GameModeManager.Contracts;
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Services
{
    // Define class
    public class RTVApi : IRTVApi, IPluginDependency<Plugin, Config>
    {
        // Define class constructor
        public RTVApi(PluginState pluginState, RTVManager rtvManager)
        {
            State = new RTVApiState(pluginState);
            Nomination = new RTVApiNomination(pluginState);
            Control = new RTVApiController(rtvManager, pluginState);
        }

        // Define class properties
        public IRTVState State { get; }
        public IRTVControl Control { get; }
        public IRTVNomination Nomination { get; }
        
        // Define child classes
        private class RTVApiState : IRTVState
        {
            // Define class dependencies
            private readonly PluginState _pluginState;

            // Define class constructor
            public RTVApiState(PluginState pluginState) => _pluginState = pluginState;

            // Define class properties
            public IMap? NextMap => _pluginState.RTV.NextMap;
            public int Duration => _pluginState.RTV.Duration;
            public bool Enabled => _pluginState.RTV.Enabled;
            public IMode? NextMode => _pluginState.RTV.NextMode;
            public bool EndOfMapVote => _pluginState.RTV.EndOfMapVote;
            public bool IncludeExtend => _pluginState.RTV.IncludeExtend;
            public int KillsBeforeEnd => _pluginState.RTV.KillsBeforeEnd;
            public int RoundsBeforeEnd => _pluginState.RTV.RoundsBeforeEnd;
            public bool EofVoteHappened => _pluginState.RTV.EofVoteHappened;
            public int SecondsBeforeEnd => _pluginState.RTV.SecondsBeforeEnd;
            public bool NominationEnabled => _pluginState.RTV.NominationEnabled;
            public bool EofVoteHappening => _pluginState.RTV.EofVoteHappening;
            public bool ChangeImmediately => _pluginState.RTV.ChangeImmediately;
            public int MaxNominationWinners => _pluginState.RTV.MaxNominationWinners;
        }

        private class RTVApiController : IRTVControl
        {
            // Define class dependencies
            private readonly RTVManager _rtvManager;
            private readonly PluginState _pluginState;

            // Define class constructor
            public RTVApiController(RTVManager rtvManager, PluginState pluginState)
            {
                _rtvManager = rtvManager;
                _pluginState = pluginState;
            }

            // Define class methods
            public void Enable() => _rtvManager.EnableRTV();
            public void Disable() => _rtvManager.DisableRTV();
            public IRTVControl SetDuration(int duration)
            {
                _pluginState.RTV.Duration = duration;
                return this;
            }
            public IRTVControl SetRoundsBeforeEnd(int rounds)
            {
                _pluginState.RTV.RoundsBeforeEnd = rounds;
                return this;
            }
            public IRTVControl SetKillsBeforeEnd(int kills)
            {
                _pluginState.RTV.KillsBeforeEnd = kills;
                return this;
            }
            public IRTVControl SetSecondsBeforeEnd(int seconds)
            {
                _pluginState.RTV.SecondsBeforeEnd = seconds;
                return this;
            }
            public IRTVControl SetEndOfMapVote(bool endOfMapVote)
            {
                _pluginState.RTV.EndOfMapVote = endOfMapVote;
                return this;
            }
            public IRTVControl SetIncludeExtend(bool includeExtend)
            {
                _pluginState.RTV.IncludeExtend = includeExtend;
                return this;
            }
            public IRTVControl SetChangeImmediately(bool changeImmediately)
            {
                _pluginState.RTV.ChangeImmediately = changeImmediately;
                return this;
            }
        }

        private class RTVApiNomination : IRTVNomination
        {
            // Define class dependencies
            private readonly PluginState _pluginState;

            // Define class constructor
            public RTVApiNomination(PluginState pluginState) => _pluginState = pluginState;

            // Define class methods
            public IRTVNomination SetNominationEnabled(bool nominationEnabled)
            {
                _pluginState.RTV.NominationEnabled = nominationEnabled;
                return this;
            }
            public IRTVNomination SetMaxNominationWinners(int maxNominationWinners)
            {
                _pluginState.RTV.MaxNominationWinners = maxNominationWinners;
                return this;
            }
        }
    }
}