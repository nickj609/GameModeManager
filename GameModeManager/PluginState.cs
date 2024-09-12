// Included libraries
using CS2_CustomVotes.Shared;
using CounterStrikeSharp.API;
using GameModeManager.Models;
using GameModeManager.Contracts;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Core.Capabilities;

// Declare namespace
namespace GameModeManager
{
    // Define class
    public class PluginState : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private ILogger<PluginState> _logger;
        private Config _config = new Config();

        // Define class instance
        public PluginState(ILogger<PluginState> logger)
        {
            _logger = logger;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
            RTVEnabled = _config.RTV.Enabled;
            WarmupMode = new Mode(_config.Warmup.Default.Name,_config.Warmup.Default.Config,_config.Warmup.Default.DefaultMap, new List<MapGroup>());
            WarmupTime = _config.Warmup.Time;
        }

        // Define static directories (Thanks Kus!)
        public static string GameDirectory = Path.Join(Server.GameDirectory + "/csgo/");
        public static string ConfigDirectory = Path.Join(GameDirectory + "cfg/");
        public static string SettingsDirectory = Path.Join(ConfigDirectory + "settings/");
        public static string WarmupDirectory = Path.Join(ConfigDirectory + "warmup/");

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
        public static Mode DefaultMode = new Mode("Casual", "casual.cfg", "de_dust2", DefaultMapGroups);
        public static Mode DefaultWarmup = new Mode("Deathmatch", "dm.cfg", "3070923343", new List<MapGroup>());
        
        // Define dynamic attributes
        public int MapRotations = 0;
        public float WarmupTime = 60;
        public bool RTVEnabled = false;
        public Map NextMap = DefaultMap;
        public Mode NextMode = DefaultMode;
        public bool WarmupStarted = false;
        public Map CurrentMap = DefaultMap;
        public bool WarmupModeEnabled = false;
        public Mode CurrentMode = DefaultMode;
        public Mode WarmupMode = DefaultWarmup;
        public List<Map> Maps = new List<Map>();
        public List<Mode> Modes = new List<Mode>();
        public List<Setting> Settings = new List<Setting>();
        public List<string> PlayerCommands = new List<string>()
        {
            "!currentmode",
            "!currentmap"
        };
        public List<MapGroup> MapGroups = new List<MapGroup>();

        // Define menus
        public BaseMenu MapMenu = new ChatMenu("Map List");
        public BaseMenu MapsMenu = new ChatMenu("Map List");
        public BaseMenu ModeMenu = new ChatMenu("Mode List");
        public BaseMenu ShowMapMenu = new ChatMenu("Map List");
        public BaseMenu GameMenu = new ChatMenu("Command List");
        public BaseMenu ShowMapsMenu = new ChatMenu("Map List");
        public BaseMenu ShowModesMenu = new ChatMenu("Mode List");
        public BaseMenu SettingsMenu = new ChatMenu("Setting Actions");
        public BaseMenu ShowSettingsMenu = new ChatMenu("Settings List");
        public BaseMenu SettingsEnableMenu = new ChatMenu("Settings List");
        public BaseMenu SettingsDisableMenu = new ChatMenu("Settings List");

        // Define CS2-CustomVotesApi
        public PluginCapability<ICustomVoteApi> CustomVotesApi { get; } = new("custom_votes:api");
    } 
}