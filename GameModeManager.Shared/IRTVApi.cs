// Included libraries
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Shared;

// Define interface
public interface IRTVState
{
    IMap? NextMap { get; }
    int Duration { get; }
    bool Enabled { get; }
    IMode? NextMode { get; }
    bool EndOfMapVote { get; }
    bool IncludeExtend { get; }
    int KillsBeforeEnd { get; }
    int RoundsBeforeEnd { get; }
    bool EofVoteHappened { get; }
    int SecondsBeforeEnd { get; }
    bool NominationEnabled { get; }
    bool EofVoteHappening { get; }
    bool ChangeImmediately { get; }
    int MaxNominationWinners { get; }
}

public interface IRTVControl
{
    void Enable();
    void Disable();
    IRTVControl SetDuration(int duration);
    IRTVControl SetRoundsBeforeEnd(int rounds);
    IRTVControl SetKillsBeforeEnd(int kills);
    IRTVControl SetSecondsBeforeEnd(int seconds);
    IRTVControl SetEndOfMapVote(bool endOfMapVote);
    IRTVControl SetIncludeExtend(bool includeExtend);
    IRTVControl SetChangeImmediately(bool changeImmediately);
}

public interface IRTVNomination
{
    IRTVNomination SetNominationEnabled(bool nominationEnabled);
    IRTVNomination SetMaxNominationWinners(int maxNominationWinners);
}

public interface IRTVApi 
{
    IRTVState State { get; }
    IRTVControl Control { get; }
    IRTVNomination Nomination { get; }
}