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
        // Define dependencies
        private readonly RTVManager _rtvManager;
        private readonly PluginState _pluginState;

        // Define class instance
        public RTVApi(PluginState pluginState, RTVManager rtvManager)
        {
            _rtvManager = rtvManager;
            _pluginState = pluginState;
            State = new RTVApiState(_pluginState);
            Control = new RTVApiController(_rtvManager, _pluginState);
            Nomination = new RTVApiNomination(_pluginState);
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

            // Define class instance
            public RTVApiState(PluginState pluginState) => _pluginState = pluginState;

            // Define class properties
            public IMap? NextMap => _pluginState.NextMap;
            public int Duration => _pluginState.RTVDuration;
            public bool Enabled => _pluginState.RTVEnabled;
            public IMode? NextMode => _pluginState.NextMode;
            public bool EndOfMapVote => _pluginState.EndOfMapVote;
            public bool IncludeExtend => _pluginState.IncludeExtend;
            public int KillsBeforeEnd => _pluginState.RTVKillsBeforeEnd;
            public int RoundsBeforeEnd => _pluginState.RTVRoundsBeforeEnd;
            public bool EofVoteHappened => _pluginState.EofVoteHappened;
            public int SecondsBeforeEnd => _pluginState.RTVSecondsBeforeEnd;
            public bool NominationEnabled => _pluginState.NominationEnabled;
            public bool EofVoteHappening => _pluginState.EofVoteHappening;
            public bool ChangeImmediately => _pluginState.ChangeImmediately;
            public int MaxNominationWinners => _pluginState.MaxNominationWinners;
        }

        private class RTVApiController : IRTVControl
        {
            // Define class dependencies
            private readonly RTVManager _rtvManager;
            private readonly PluginState _pluginState;

            // Define class instance
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
                _pluginState.RTVDuration = duration;
                return this;
            }
            public IRTVControl SetRoundsBeforeEnd(int rounds)
            {
                _pluginState.RTVRoundsBeforeEnd = rounds;
                return this;
            }
            public IRTVControl SetKillsBeforeEnd(int kills)
            {
                _pluginState.RTVKillsBeforeEnd = kills;
                return this;
            }
            public IRTVControl SetSecondsBeforeEnd(int seconds)
            {
                _pluginState.RTVSecondsBeforeEnd = seconds;
                return this;
            }
            public IRTVControl SetEndOfMapVote(bool endOfMapVote)
            {
                _pluginState.EndOfMapVote = endOfMapVote;
                return this;
            }
            public IRTVControl SetIncludeExtend(bool includeExtend)
            {
                _pluginState.IncludeExtend = includeExtend;
                return this;
            }
            public IRTVControl SetChangeImmediately(bool changeImmediately)
            {
                _pluginState.ChangeImmediately = changeImmediately;
                return this;
            }
        }

        private class RTVApiNomination : IRTVNomination
        {
            // Define class dependencies
            private readonly PluginState _pluginState;

            // Define class instance
            public RTVApiNomination(PluginState pluginState) => _pluginState = pluginState;

            // Define class properties
            public bool NominationEnabled => _pluginState.NominationEnabled;
            public int MaxNominationWinners => _pluginState.MaxNominationWinners;

            // Define class methods
            public IRTVNomination SetNominationEnabled(bool nominationEnabled)
            {
                _pluginState.NominationEnabled = nominationEnabled;
                return this;
            }
            public IRTVNomination SetMaxNominationWinners(int maxNominationWinners)
            {
                _pluginState.MaxNominationWinners = maxNominationWinners;
                return this;
            }
        }
    }
}