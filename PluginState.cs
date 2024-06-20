// Declare namespace
using CounterStrikeSharp.API.Core;

namespace GameModeManager
{
    // Define PluginState class
    public class PluginState
    {
        // Define global plugin objects
        public static Map? CurrentMap;
        public static bool RTVEnabled = false;
        public static MapGroup? CurrentMapGroup;
        public static List<Map> Maps = new List<Map>();
        public static List<MapGroup> MapGroups = new List<MapGroup>();
        public static List<string> Commands = new List<string>()
        {
            "!currentmode",
            "!currentmap"
        };

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
    }
}