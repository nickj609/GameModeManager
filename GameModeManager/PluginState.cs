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
        // Define dependencies
        private Config _config = new Config();

        // Define class instance
        public PluginState()
        {

        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
            RTVEnabled = _config.RTV.Enabled;
            PerMapWarmup = _config.Warmup.PerMap;
            RotationsEnabled = _config.Rotation.Enabled; 
        }

        // Define static directories (Thanks Kus!)
        public static string GameDirectory = Path.Join(Server.GameDirectory + "/csgo/");
        public static string ConfigDirectory = Path.Join(GameDirectory + "cfg/");
        public static string SettingsDirectory = Path.Join(ConfigDirectory + "settings/");

        // Define static objects
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
        public static Mode DefaultWarmup = new Mode("Knives Only", "warmup/knives_only.cfg", new List<MapGroup>());
        
        // Define dynamic attributes
        public int TimeLimit = 120;
        public int MapRotations = 0;
        public bool RTVEnabled = false;
        public bool PerMapWarmup = false;
        public Map CurrentMap = DefaultMap;
        public bool WarmupScheduled = false;
        public bool TimeLimitEnabled = false;
        public Mode CurrentMode = DefaultMode;
        public bool TimeLimitCustom = false;
        public bool RotationsEnabled = true;
        public bool TimeLimitScheduled = false;
        public Mode WarmupMode = DefaultWarmup;
        public List<Map> Maps = new List<Map>(DefaultMaps);
        public List<Mode> Modes = new List<Mode>();
        public List<Mode> WarmupModes = new List<Mode>();
        public List<Setting> Settings = new List<Setting>();
        public List<string> PlayerCommands = new List<string>()
        {
            "!currentmode",
            "!currentmap"
        };
        public List<MapGroup> MapGroups = new List<MapGroup>();

        // Define WASD menus
        public IWasdMenu? MapWASDMenu;
        public IWasdMenu? MapsWASDMenu;
        public IWasdMenu? ModeWASDMenu;
        public IWasdMenu? GameWASDMenu;
        public IWasdMenu? VoteMapWASDMenu;
        public IWasdMenu? VoteMapsWASDMenu;
        public IWasdMenu? SettingsWASDMenu;
        public IWasdMenu? VoteModesWASDMenu;
        public IWasdMenu? VoteSettingsWASDMenu;
        public IWasdMenu? SettingsEnableWASDMenu;
        public IWasdMenu? SettingsDisableWASDMenu;

        // Define base menus
        public BaseMenu MapMenu = new ChatMenu("Map List");
        public BaseMenu MapsMenu = new ChatMenu("Map List");
        public BaseMenu ModeMenu = new ChatMenu("Mode List");
        public BaseMenu VoteMapMenu = new ChatMenu("Map List");
        public BaseMenu GameMenu = new ChatMenu("Command List");
        public BaseMenu VoteMapsMenu = new ChatMenu("Map List");
        public BaseMenu VoteModesMenu = new ChatMenu("Mode List");
        public BaseMenu SettingsMenu = new ChatMenu("Setting Actions");
        public BaseMenu VoteSettingsMenu = new ChatMenu("Settings List");
        public BaseMenu SettingsEnableMenu = new ChatMenu("Settings List");
        public BaseMenu SettingsDisableMenu = new ChatMenu("Settings List");

        // Define APIs
        public PluginCapability<ICustomVoteApi> CustomVotesApi { get; } = new("custom_votes:api");
        public PluginCapability<IWasdMenuManager> WasdMenuManager { get; } = new("wasdmenu:manager");

    } 
}