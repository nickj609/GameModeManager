// Included libraries
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager
{
    // Define class
    public class PluginState : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        public GameController Game;
        public TimeLimitController TimeLimit;

        // Define class constructor
        public PluginState()
        {
            Game = new GameController();
            TimeLimit = new TimeLimitController();
        }

        // Define GameMode class
        public class GameController()
        {
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
            public static IMapGroup DefaultMapGroup = new MapGroup("mg_active", DefaultMaps.ToHashSet());
            public static Dictionary<string, IMapGroup> DefaultMapGroups = new Dictionary<string, IMapGroup>(StringComparer.OrdinalIgnoreCase) { { DefaultMapGroup.Name, DefaultMapGroup } };
            public static IMode DefaultMode = new Mode("Casual", "casual.cfg", DefaultMapGroups.Values.ToHashSet());
            public static IMode DefaultWarmup = new Mode("Deathmatch", "warmup/dm.cfg", new HashSet<IMapGroup>());

            // Define dynamic properties
            public int MapRotations = 0;
            public bool PerMapWarmup = false;
            public bool WarmupRunning = false;
            public IMap CurrentMap = DefaultMap;
            // By name: only one map is loaded at a time, and the same map can be listed in two map groups with different workshop IDs
            public bool IsCurrentMap(IMap map) => CurrentMap.Name.Equals(map.Name, StringComparison.OrdinalIgnoreCase);
            public bool RotationsEnabled = true;
            public bool RtvInMode = true;
            public bool RtvEnabled(Config config) => config.MapChooser.Rtv && RtvInMode;
            public bool WarmupScheduled = false;
            public bool CountdownRunning = false;
            public IMode CurrentMode = DefaultMode;
            public IMode WarmupMode = DefaultWarmup;
            public Dictionary<string, IMode> Modes = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, IMode> WarmupModes = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, ISetting> Settings = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, IMapGroup> MapGroups = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<long, IMap> MapsByWorkshopId = new();
            public Dictionary<string, IMap> Maps = DefaultMaps.ToDictionary(m => m.Name, m => m, StringComparer.OrdinalIgnoreCase);
            public List<string> PlayerCommands = new()
            {
                "!currentmode",
                "!currentmap"
            };
        }

        // Define TimeLimitController class
        public class TimeLimitController()
        {
            // Define class properties
            public bool Enabled = false;
            public float Duration = 120;
            public bool CustomLimit = false;
            public float CustomStartTime = 0f;
        }   
    } 
}