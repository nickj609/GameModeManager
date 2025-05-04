// Included libraries
using WASDMenuAPI.Shared;
using GameModeManager.Models;
using CS2_CustomVotes.Shared;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Core.Capabilities;

// Declare namespace
namespace GameModeManager
{
    // Define class
    public class PluginState : IPluginDependency<Plugin, Config>
    {
        // Define class instance
        public PluginState(){}

        // Define static directories (Thanks Kus!)
        public static string GameDirectory = Path.Join(Server.GameDirectory + "/csgo/");
        public static string ConfigDirectory = Path.Join(GameDirectory + "cfg/");
        public static string SettingsDirectory = Path.Join(ConfigDirectory + "settings/");

        // Define static properties
        public static IMap DefaultMap = new Map("de_dust2", "Dust 2");
        public static List<IMap> DefaultMaps = new List<IMap>()
        {
            new Map("de_dust2", "Dust 2"),
            new Map("de_anubis", "Anubis"),
            new Map("de_inferno", "Inferno"),
            new Map("de_mirage", "Mirage"),
            new Map("de_nuke", "Nuke"),
            new Map("de_vertigo", "Vertigo")
        };
        public static IMapGroup DefaultMapGroup = new MapGroup("mg_active", DefaultMaps);
        public static List<IMapGroup> DefaultMapGroups = new List<IMapGroup>{DefaultMapGroup};
        public static IMode DefaultMode = new Mode("Casual", "casual.cfg", DefaultMapGroups);
        public static IMode DefaultWarmup = new Mode("Deathmatch", "warmup/dm.cfg", new List<IMapGroup>());
        
        // Define dynamic properties
        public IMap? NextMap;
        public IMode? NextMode;
        public int InCoolDown = 0;
        public float TimeLimit = 120;
        public int MapExtends = 0;
        public int MaxExtends = 0;
        public int RTVDuration = 60;
        public int MapRotations = 0;
        public string RTVWinner = "";
        public bool RTVEnabled = false;
        public List<IMode> Modes = new();
        public bool IncludeExtend = false;
        public bool PerMapWarmup = false;
        public bool EndOfMapVote = false;
        public int RTVKillsBeforeEnd = 13;
        public int RTVRoundsBeforeEnd = 2;
        public bool WarmupRunning = false;
        public IMap CurrentMap = DefaultMap;
        public int MaxNominationWinners = 1;
        public bool WarmupScheduled = false;
        public bool TimeLimitCustom = false;
        public bool RotationsEnabled = true;
        public bool DisableCommands = false;
        public bool TimeLimitEnabled = false;
        public bool CountdownRunning = false;
        public bool EofVoteHappened = false;
        public bool EofVoteHappening = false;
        public int RTVSecondsBeforeEnd = 120;
        public bool NominationEnabled = true;
        public bool ChangeImmediately = false;
        public IMode CurrentMode = DefaultMode;
        public bool TimeLimitScheduled = false;
        public IMode WarmupMode = DefaultWarmup;
        public List<IMode> WarmupModes = new();
        public List<ISetting> Settings = new();
        public List<IMapGroup> MapGroups = new();
        public float CustomTimeLimitStartTime = 0f;
        public Dictionary<string, int> Votes = new();
        public List<string> OptionsOnCoolDown = new();
        public List<string> PlayerCommands = new()
        {
            "!currentmode",
            "!currentmap"
        };
        public List<IMap> Maps = [.. DefaultMaps];

        // Define APIs
        public PluginCapability<ICustomVoteApi> CustomVotesApi { get; } = new("custom_votes:api");
        public PluginCapability<IWasdMenuManager> WasdMenuManager { get; } = new("wasdmenu:manager");
    } 
}