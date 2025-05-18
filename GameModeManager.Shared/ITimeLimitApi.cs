// Declare namespace
namespace GameModeManager.Shared;

// Define interface
public interface ITimeLimitState
{
    int MaxWins { get; }
    float TimeLimit { get; }
    bool Custom { get; }
    bool Enabled { get; }
    bool Scheduled { get; }
    decimal TimePlayed { get; }
    int RemainingWins { get; }
    bool UnlimitedTime { get; }
    bool UnlimitedRounds { get; }
    decimal TimeRemaining { get; }
}

public interface ITimeLimitControl
{
    void Enforce();
    void Disable();
    void CustomLimit(int time);
}

public interface ITimeLimitMessaging
{
    string TimeLeftMessage();
}

public interface ITimeLimitApi
{
    ITimeLimitState State { get; }
    ITimeLimitControl Control { get; }
    ITimeLimitMessaging Messaging { get; }
}