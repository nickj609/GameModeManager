// Included libraries
using System.Text.Json;
using System.Text.Json.Serialization;

// Declare namespace
namespace GameModeManager.Panel
{
    // Define classes
    // The web panel's agent protocol, version 1: the panel repo's docs/agent-protocol.md is the spec, and its
    // src/GmmPanel/Protocol/Messages.cs is the other side. JSON text frames with a "type", camelCase.
    public static class PanelProtocol
    {
        public const int Version = 1;
        public const int MaxOutputChars = 64 * 1024;   // A console reply is cut there

        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };
    }

    public sealed class PanelCommand
    {
        public string Type { get; set; } = "";
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public Dictionary<string, string> Args { get; set; } = new();
        public string AdminSteamId { get; set; } = "";
        public string AdminName { get; set; } = "";

        public string Arg(string key) => Args.TryGetValue(key, out string? value) ? value : "";
    }

    public sealed class PanelResult
    {
        public string Type { get; set; } = "result";
        public string Id { get; set; } = "";
        public bool Ok { get; set; }
        public string Message { get; set; } = "";
        public PanelFollowUp? FollowUp { get; set; }
        public string? Output { get; set; }
        public PanelDataPage? Data { get; set; }   // "query" only

        public static PanelResult Done(string message) => new() { Ok = true, Message = message };
        public static PanelResult Refused(string message) => new() { Ok = false, Message = message };
    }

    // What GameModeManager's menus ask after a setting change, for the panel to ask too
    public sealed class PanelFollowUp
    {
        public string Kind { get; set; } = "";        // restart or round
        public string Settings { get; set; } = "";
        public bool CanReplay { get; set; }
        public bool CanEndRound { get; set; }
    }

    public sealed class PanelStatus
    {
        public string GmmVersion { get; set; } = "";
        public string CssVersion { get; set; } = "";
        public int Players { get; set; }
        public int MaxPlayers { get; set; }
        public PanelItem Mode { get; set; } = new();
        public PanelItem Map { get; set; } = new();
        public string? NextMap { get; set; }
        public bool VoteInProgress { get; set; }
        public PanelVotes Votes { get; set; } = new();
        public List<PanelMode> Modes { get; set; } = new();
        public List<PanelItem> Maps { get; set; } = new();
        public List<PanelSetting> Settings { get; set; } = new();
        public List<PanelValueSetting> ValueSettings { get; set; } = new();
        public List<PanelAction> Actions { get; set; } = new();
        public PanelWarmup Warmup { get; set; } = new();
        public PanelRotation Rotation { get; set; } = new();
        public PanelRounds Rounds { get; set; } = new();
        public PanelMatchZy? MatchZy { get; set; }
        public PanelConsoleInfo Console { get; set; } = new();
        public List<PanelIntegration> Integrations { get; set; } = new();
        public PanelServerInfo Server { get; set; } = new();
        public List<PanelPlugin>? Plugins { get; set; }          // Null when CounterStrikeSharp's plugin list can't be read
        public List<PanelDataSource> Data { get; set; } = new(); // Installed plugins whose database the agent can read
        public List<PanelRosterEntry> Roster { get; set; } = new(); // Who's on (humans), for the panel's player history and "who's on"
    }

    // The Admin page's server settings, and the address players connect to
    public sealed class PanelServerInfo
    {
        public string Hostname { get; set; } = "";
        public string? Address { get; set; }
        public int Port { get; set; }                   // hostport: with the address the agent connects from, the panel's Connect fallback
        public string TeamCt { get; set; } = "";
        public string TeamT { get; set; } = "";
    }

    // One connected player, with the scoreboard (the panel's Who's on shows it like the in-game Tab board)
    public sealed class PanelRosterEntry
    {
        public string SteamId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Team { get; set; } = "";          // CT, T or Spectator
        public int Score { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Ping { get; set; }
    }

    public sealed class PanelPlugin
    {
        public string Name { get; set; } = "";          // The plugins/ folder: what css_plugins load takes
        public string Module { get; set; } = "";
        public string Version { get; set; } = "";
        public string Author { get; set; } = "";
        public string Description { get; set; } = "";
        public string State { get; set; } = "";         // loaded, unloaded, loading, notloaded
        public bool Protected { get; set; }             // GameModeManager itself
    }

    public sealed class PanelDataSource
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool Available { get; set; }
        public string? Reason { get; set; }
        public List<PanelDataList> Lists { get; set; } = new();
    }

    public sealed class PanelDataList
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public bool Search { get; set; }
        public string Flag { get; set; } = "@css/root";      // The admin flag reading it needs
        public List<string> Filters { get; set; } = new();
        public List<PanelDataAction> Actions { get; set; } = new();
    }

    public sealed class PanelDataAction
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public bool Row { get; set; }
        public string Flag { get; set; } = "@css/root";      // The admin flag the plugin's own command needs
        public List<string> Fields { get; set; } = new();
        public Dictionary<string, string> When { get; set; } = new();
    }

    public sealed class PanelDataPage
    {
        public List<PanelDataColumn> Columns { get; set; } = new();
        public List<Dictionary<string, string?>> Rows { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int Total { get; set; }
    }

    public sealed class PanelDataColumn
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public string Kind { get; set; } = "text";

        public PanelDataColumn(string key, string label, string kind = "text")
        {
            Key = key;
            Label = label;
            Kind = kind;
        }
    }

    public class PanelItem
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }

    public sealed class PanelAction : PanelItem
    {
        public string Flag { get; set; } = "@css/cvar";
        public string Group { get; set; } = "";         // The cfg's "// group:" or its subfolder ("Bots"); "" for none
    }

    public sealed class PanelMode : PanelItem
    {
        public List<string> Maps { get; set; } = new();
    }

    public sealed class PanelSetting : PanelItem
    {
        public string State { get; set; } = "Unknown";
        public string Applies { get; set; } = "now";
        public bool Cheats { get; set; }
        public string Flag { get; set; } = "@css/cvar";   // The admin flag its menu row needs (the cfg's "// permission:")
    }

    public sealed class PanelValueSetting : PanelItem
    {
        public string? Value { get; set; }
        public List<string> Choices { get; set; } = new();
        public List<string> CheatChoices { get; set; } = new();
        public string Flag { get; set; } = "@css/cvar";
        public string Applies { get; set; } = "now";
    }

    public sealed class PanelVotes
    {
        public bool CanStart { get; set; }
        public string? Unavailable { get; set; }
    }

    public sealed class PanelWarmup
    {
        public bool Enabled { get; set; }
        public bool Scheduled { get; set; }
        public bool Running { get; set; }
        public string? Mode { get; set; }
        public List<string> Modes { get; set; } = new();   // The warmup modes (WarmupModes), not the game modes
        public string? Unavailable { get; set; }            // Why the panel can't start one (MatchZy, Arms Race)
    }

    public sealed class PanelRotation
    {
        public bool Maps { get; set; }
        public bool Modes { get; set; }
        public int Interval { get; set; }
        public List<int> IntervalChoices { get; set; } = new() { 1, 2, 3, 4, 5, 6, 8, 10 };
        public string ModeVote { get; set; } = "random";
        public bool CountSkipped { get; set; }
        public bool Rtv { get; set; }
        public bool WhenEmpty { get; set; }
        public int EmptyMinutes { get; set; }
        public List<int> EmptyChoices { get; set; } = new() { 5, 10, 15, 20, 30, 60 };
        public bool Schedules { get; set; }
        public int MapsCounted { get; set; }
        public bool ModeChangeDue { get; set; }
    }

    public sealed class PanelRounds
    {
        public bool Enabled { get; set; }                   // Actions.Rounds in the config
        public string Flag { get; set; } = "@css/generic";   // Actions.RoundsPermission
        public string Pause { get; set; } = "Running";
        public bool PauseUnavailable { get; set; }          // In warmup or a timeout, with nothing to unpause
        public bool CanTimeout { get; set; }
        public bool TimeoutsByMatchZy { get; set; }
        public bool MatchZyLive { get; set; }
        public bool Frozen { get; set; }
        public bool CanEndRound { get; set; }
        public int CurrentRound { get; set; }
        public List<int> Backups { get; set; } = new();     // Newest first: [0] is this round, [1] the last one
    }

    public sealed class PanelMatchZy
    {
        public bool Live { get; set; }
        public bool Practice { get; set; }
        public bool Veto { get; set; }
        public bool ReadyUp { get; set; }
        public bool Paused { get; set; }
        public bool KnifeRound { get; set; }
        public bool Playout { get; set; }
        public bool Whitelist { get; set; }
    }

    public sealed class PanelConsoleInfo
    {
        public bool Available { get; set; }
        public string? Reason { get; set; }
    }

    public sealed class PanelIntegration
    {
        public string Name { get; set; } = "";
        public bool Found { get; set; }
        public string Detail { get; set; } = "";
    }

    // panel-secrets.json, next to GameModeManager.json. Never logged.
    public sealed class PanelSecrets
    {
        public string Token { get; set; } = "";
        public string AccessClientId { get; set; } = "";
        public string AccessClientSecret { get; set; } = "";
    }
}
