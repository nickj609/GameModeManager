// Included libraries
using System.Diagnostics;

// Declare namespace
namespace GameModeManager
{
    // Define PluginState class
    public class PluginState
    {
        // Define plugin default map group, map, and map list
        public static Map DefaultMap = new Map("de_dust2");
        public static List<Map> DefaultMaps = new List<Map>()
        {
            new Map("de_dust2"),
            new Map("de_ancient"),
            new Map("de_anubis"),
            new Map("de_inferno"),
            new Map("de_mirage"),
            new Map("de_nuke"),
            new Map("de_overpass"),
            new Map("de_vertigo")
        };
        public static MapGroup DefaultMapGroup = new MapGroup("mg_casual", DefaultMaps);

        // Define dynamic objects
        public static bool RTVEnabled = false;
        public static Map? CurrentMap = DefaultMap;
        public static List<Map> Maps = DefaultMaps;
        public static MapGroup? CurrentMapGroup = DefaultMapGroup;
        public static List<MapGroup> MapGroups = new List<MapGroup>(){DefaultMapGroup};

        // Define player command list
        public static List<string> PlayerCommands = new List<string>()
        {
            "!currentmode",
            "!currentmap"
        };

        // Define map rotation parameters
        public static int mp_timelimit = 0;
        public static Stopwatch stopwatch = new Stopwatch();
        public static int CTWins = 0;
        public static int TWins = 0;
        public static int _currentIndex = -1;
        public static int _currentIndexs = -1;
        public static bool WinTeamDraw = false;
        public static bool onetime = false;
        public static bool getvalues = false;
        public static bool timeisup = false;
        public static bool timeisupTime = false;
        public static bool timeisupEnd = false;
        public static bool fiveMinsLeftPrinted = false;
        public static bool twoMinsLeftPrinted = false;
        public static bool oneMinLeftPrinted = false;
        public static bool thirtySecsLeftPrinted = false;
        public static bool fifteenSecsLeftPrinted = false;
        public static bool threeSecsLeftPrinted = false;
        public static bool twoSecsLeftPrinted = false;
        public static bool oneSecLeftPrinted = false;
        public static CounterStrikeSharp.API.Modules.Timers.Timer? Defaultmap;
        public static CounterStrikeSharp.API.Modules.Timers.Timer? RotationTimer;
        public static CounterStrikeSharp.API.Modules.Timers.Timer? RotationTimer2;
        public static CounterStrikeSharp.API.Modules.Timers.Timer? ForceEndTimer;
    } 
}