// Included libraries
using WASDSharedAPI;
using CS2_CustomVotes.Shared;
using CounterStrikeSharp.API;
using GameModeManager.Models;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Modules.Menu;
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
        public static Map DefaultMap = new Map("de_dust2", "Dust 2");
        public static List<Map> DefaultMaps = new List<Map>()
        {
            new Map("de_dust2", "Dust 2"),
            new Map("de_anubis", "Anubis"),
            new Map("de_inferno", "Inferno"),
            new Map("de_mirage", "Mirage"),
            new Map("de_nuke", "Nuke"),
            new Map("de_vertigo", "Vertigo")
        };
        public static MapGroup DefaultMapGroup = new MapGroup("mg_active", DefaultMaps);
        public static List<MapGroup> DefaultMapGroups = new List<MapGroup>{DefaultMapGroup};
        public static Mode DefaultMode = new Mode("Casual", "casual.cfg", DefaultMapGroups);
        public static Mode DefaultWarmup = new Mode("Deathmatch", "warmup/dm.cfg", new List<MapGroup>());
        
        // Define dynamic properties
        public Map? NextMap;
        public Mode? NextMode;
        public int InCoolDown = 0;
        public int TimeLimit = 120;
        public int RTVDuration = 60;
        public int MapRotations = 0;
        public string RTVWinner = "";
        public bool RTVEnabled = false;
        public List<Mode> Modes = new();
        public bool PerMapWarmup = false;
        public int RTVRoundsBeforeEnd = 2;
        public bool WarmupRunning = false;
        public Map CurrentMap = DefaultMap;
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
        public Mode CurrentMode = DefaultMode;
        public bool TimeLimitScheduled = false;
        public Mode WarmupMode = DefaultWarmup;
        public List<Mode> WarmupModes = new();
        public List<Setting> Settings = new();
        public List<MapGroup> MapGroups = new();
        public Dictionary<string, int> Votes = new();
        public List<string> OptionsOnCoolDown = new();
        public List<string> PlayerCommands = new()
        {
            "!currentmode",
            "!currentmap"
        };
        public List<Map> Maps = new List<Map>(DefaultMaps);
    
        // Define WASD menus
        public IWasdMenu? RTVWASDMenu;
        public IWasdMenu? MapWASDMenu;
        public IWasdMenu? MapsWASDMenu;
        public IWasdMenu? ModeWASDMenu;
        public IWasdMenu? GameWASDMenu;
        public IWasdMenu? VoteMapWASDMenu;
        public IWasdMenu? VoteMapsWASDMenu;
        public IWasdMenu? SettingsWASDMenu;
        public IWasdMenu? VoteModesWASDMenu;
        public IWasdMenu? NominationWASDMenu;
        public IWasdMenu? NominateMapWASDMenu;
        public IWasdMenu? NominateModeWASDMenu;
        public IWasdMenu? VoteSettingsWASDMenu;
        public IWasdMenu? SettingsEnableWASDMenu;
        public IWasdMenu? SettingsDisableWASDMenu;

        // Define base menus
        public BaseMenu RTVMenu = new ChatMenu("RTV List");
        public BaseMenu MapMenu = new ChatMenu("Map List");
        public BaseMenu MapsMenu = new ChatMenu("Map List");
        public BaseMenu ModeMenu = new ChatMenu("Mode List");
        public BaseMenu VoteMapMenu = new ChatMenu("Map List");
        public BaseMenu GameMenu = new ChatMenu("Command List");
        public BaseMenu VoteMapsMenu = new ChatMenu("Map List");
        public BaseMenu VoteModesMenu = new ChatMenu("Mode List");
        public BaseMenu NominationMenu = new ChatMenu("Nominations");
        public BaseMenu NominateMapMenu = new ChatMenu("Nominations");
        public BaseMenu NominateModeMenu = new ChatMenu("Nominations");
        public BaseMenu SettingsMenu = new ChatMenu("Setting Actions");
        public BaseMenu VoteSettingsMenu = new ChatMenu("Settings List");
        public BaseMenu SettingsEnableMenu = new ChatMenu("Settings List");
        public BaseMenu SettingsDisableMenu = new ChatMenu("Settings List");

        // Define APIs
        public PluginCapability<ICustomVoteApi> CustomVotesApi { get; } = new("custom_votes:api");
        public PluginCapability<IWasdMenuManager> WasdMenuManager { get; } = new("wasdmenu:manager");
    } 
}