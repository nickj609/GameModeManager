// Included libraries
using CounterStrikeSharp.API.Core;

// Declare namespace
namespace GameModeManager.Common
{
    // Define class
    // Who asked for a change: a player in game, or an admin on the web panel, who has a name and a SteamID but no player.
    // A player gets replies as before (PlayerFeedback's notification and chat line). For the panel there's nobody to show
    // them to, so the first reply (or the first refusal) is kept here and sent back as the command's answer.
    public sealed class Caller
    {
        // Define class constructor
        private Caller(string name, ulong steamId, CCSPlayerController? player)
        {
            Name = name;
            SteamId = steamId;
            Player = player;
        }

        // Define class properties
        public string Name { get; }
        public ulong SteamId { get; }
        public CCSPlayerController? Player { get; }
        public string? Reply { get; private set; }
        public bool Refused { get; private set; }

        // Define class methods
        public static Caller Of(CCSPlayerController player) => new(player.PlayerName, player.SteamID, player);

        public static Caller Panel(string name, ulong steamId) => new(name, steamId, null);

        // A refusal replaces a success reply, and the first refusal is kept: it's the reason the change didn't happen
        public void Answer(string text, bool refused)
        {
            if (Refused)
                return;
            if (Reply == null || refused)
                Reply = text;
            Refused = refused;
        }
    }
}
