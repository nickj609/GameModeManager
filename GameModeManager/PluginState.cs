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
        public RTVController RTV;
        public GameController Game;
        public TimeLimitController TimeLimit;

        // Define class instance
        public PluginState()
        {
            RTV = new RTVController();
            Game = new GameController();
            TimeLimit = new TimeLimitController();
        }

        // Define RTV class
        public class RTVController()
        {
            // Define class properties
            public IMap? NextMap;
            public IMode? NextMode;
            public int Duration = 60;
            public int InCoolDown = 0;
            public int MapExtends = 0;
            public int MaxExtends = 0;
            public string Winner = "";
            public bool Enabled = false;
            public int KillsBeforeEnd = 13;
            public int RoundsBeforeEnd = 2;
            public bool DisableCommands = false;
            public bool EndOfMapVote = false;
            public int SecondsBeforeEnd = 120;
            public bool IncludeExtend = false;
            public int MaxNominationWinners = 1;
            public bool EofVoteHappened = false;
            public bool EofVoteHappening = false;
            public bool NominationEnabled = true;
            public bool ChangeImmediately = false;
            public Dictionary<string, int> Votes = new();
            public List<string> OptionsOnCoolDown = new();
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
            public static IMapGroup DefaultMapGroup = new MapGroup("mg_active", DefaultMaps);
            public static List<IMapGroup> DefaultMapGroups = new List<IMapGroup>{DefaultMapGroup};
            public static IMode DefaultMode = new Mode("Casual", "casual.cfg", DefaultMapGroups);
            public static IMode DefaultWarmup = new Mode("Deathmatch", "warmup/dm.cfg", new List<IMapGroup>());

            // Define dynamic properties
            public int MapRotations = 0;
            public List<IMode> Modes = new();
            public bool PerMapWarmup = false;
            public bool WarmupRunning = false;
            public IMap CurrentMap = DefaultMap;
            public bool RotationsEnabled = true;
            public bool WarmupScheduled = false;
            public bool CountdownRunning = false;
            public IMode CurrentMode = DefaultMode;
            public List<IMode> WarmupModes = new();
            public List<ISetting> Settings = new();
            public IMode WarmupMode = DefaultWarmup;
            public List<IMapGroup> MapGroups = new();
            public List<IMap> Maps = [.. DefaultMaps];
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
            public bool Scheduled = false;
            public bool CustomLimit = false;
            public float CustomStartTime = 0f;
        }   
    } 
}