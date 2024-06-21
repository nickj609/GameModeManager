// Included libraries
using System.Diagnostics;
using CounterStrikeSharp.API.Core;

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
        public static MapGroup DefaultMapGroup = new MapGroup("mg_active", DefaultMaps);
        public static Mode DefaultMode = new Mode("Casual", "casual.cfg", new List<MapGroup>{DefaultMapGroup});

        // Define dynamic objects
        public static bool RTVEnabled = false;
        public static Map? CurrentMap = DefaultMap;
        public static List<Map> Maps = DefaultMaps;
        public static Mode CurrentMode = DefaultMode;
        public static List<Mode> Modes = new List<Mode>();
        public static MapGroup? CurrentMapGroup = DefaultMapGroup;
        public static List<MapGroup> MapGroups = new List<MapGroup>(){DefaultMapGroup};

        // Define player command list
        public static List<string> PlayerCommands = new List<string>()
        {
            "!currentmode",
            "!currentmap"
        };
    } 
}