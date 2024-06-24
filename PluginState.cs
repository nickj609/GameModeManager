// Included libraries
using CS2_CustomVotes.Shared;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Core.Capabilities;

// Declare namespace
namespace GameModeManager
{
    // Define class
    public class PluginState : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        public BaseMenu MapMenu;
        public BaseMenu MapsMenu;
        public BaseMenu ModeMenu;
        public BaseMenu GameMenu;
        public BaseMenu ShowMapMenu;
        public BaseMenu SettingsMenu;
        public BaseMenu ShowMapsMenu;
        public BaseMenu ShowModesMenu;
        public BaseMenu ShowSettingsMenu;
        public BaseMenu SettingsEnableMenu;
        public BaseMenu SettingsDisableMenu; 
        private readonly MenuFactory _menuFactory;

        // Define class instance
        public PluginState(MenuFactory menuFactory)
        {
            // Load menu factory
            _menuFactory = menuFactory;

            // Assign menus
            MapMenu = _menuFactory.AssignMenu("center", "Map List");
            MapsMenu = _menuFactory.AssignMenu("center", "Map List");
            ModeMenu = _menuFactory.AssignMenu("center", "Mode List");
            GameMenu = _menuFactory.AssignMenu("center", "Command List");
            ShowMapMenu = _menuFactory.AssignMenu("center", "Map List");
            ShowMapsMenu = _menuFactory.AssignMenu("center", "Map List");
            ShowModesMenu = _menuFactory.AssignMenu("center", "Mode List");
            SettingsMenu = _menuFactory.AssignMenu("center", "Setting Actions");
            ShowSettingsMenu = _menuFactory.AssignMenu("center", "Setting List");
            SettingsEnableMenu = _menuFactory.AssignMenu("center", "Setting List");
            SettingsDisableMenu = _menuFactory.AssignMenu("center", "Setting List");
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
            new Map("de_ancient", "Ancient"),
            new Map("de_anubis", "Anubis"),
            new Map("de_inferno", "Inferno"),
            new Map("de_mirage", "Mirage"),
            new Map("de_nuke", "Nuke"),
            new Map("de_vertigo", "Vertigo")
        };
        public static MapGroup DefaultMapGroup = new MapGroup("mg_active", "Active Map Pool",  DefaultMaps);
        public static Mode DefaultMode = new Mode("Casual", "casual.cfg", new List<MapGroup>{DefaultMapGroup});
        
        // Define dynamic attributes
        public int MapRotations = 0;
        public bool RTVEnabled = false;
        public Map CurrentMap = DefaultMap;
        public Mode CurrentMode = DefaultMode;
        public List<Map> Maps = new List<Map>();
        public List<Mode> Modes = new List<Mode>();
        public MapGroup CurrentMapGroup = DefaultMapGroup;
        public List<Setting> Settings = new List<Setting>();
        public List<string> PlayerCommands = new List<string>()
        {
            "!currentmode",
            "!currentmap"
        };
        public List<MapGroup> MapGroups = new List<MapGroup>();

        // Define CS2-CustomVotesApi
        public PluginCapability<ICustomVoteApi> CustomVotesApi { get; } = new("custom_votes:api");
    } 
}