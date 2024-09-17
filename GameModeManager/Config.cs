// Included libraries
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager
{
    // Define class dependencies
    public class ScheduleEntry
    {
        public string Time { get; set; } = "10:00"; // Time of the rotation (e.g., "10:00")
        public string Mode { get; set; } = "Casual"; // Optional: Game mode to rotate to (if specified)
    }

    public class ModeEntry
    {
        public string Name {get; set;} = "Casual";
        public string Config {get; set;} = "casual.cfg";
        public string? DefaultMap {get; set;} = null; 
        public List<string> MapGroups {get; set;} = new List<string>(){"mg_active", "mg_comp"};
    }
    
    // Define RTV settings
    public class RTVSettings
    {
        public bool Enabled { get; set; } = false; // Enable RTV Compatibility
        public int Mode { get; set; } = 0; // 0 for current mode maps, 1 for all maps
        public bool MapFormat { get; set; } = false; // Default file format (ws:<workshop id>). When set to false, uses format <map name>:<workshop id>. 
        public string Plugin { get; set; } = "addons/counterstrikesharp/plugins/RockTheVote/RockTheVote.dll"; // RTV plugin path
        public string MapList { get; set; } = "addons/counterstrikesharp/plugins/RockTheVote/maplist.txt"; // Default map list file
        
    }

    // Define map settings
    public class MapSettings
    {
        public float Delay { get; set; } = 2.0f; // Map change delay in seconds
        public string Style { get; set; } = "center"; // Changes map menu type 
        public string Default { get; set; } =  "de_dust2"; // Default map on server start
    }

    // Define vote settings
    public class VoteSettings
    {
        public bool Enabled { get; set; } = false; // Enables CS2-CustomVotes compatibility
        public bool Maps { get; set; } = false; // Enables vote to change game to a specific map in the current mode
        public bool AllMaps { get; set; } = false; // Changes vote to change game to a specific map in all modes
        public bool GameModes { get; set; } = false; // Enables vote to change game mode
        public bool GameSettings { get; set; } = false; // Enables vote to change game setting
        public string Style { get; set; } = "center"; // Changes vote menu type (i.e. "chat" or "center")
    }

    // Define game settings
    public class GameSettings
    {
        public bool Enabled { get; set; } = true; // Enable game settings
        public string Style { get; set; } = "center"; // Changes settings menu type (i.e. "chat" or "center")
        public string Folder { get; set; } = "settings"; // Default settings folder
    }

    // Define command settings
    public class CommandSettings
    {
        public bool Map { get; set; } = true; // Enables or disables !map admin command
        public bool Maps { get; set; } = true; // Enables or disables !maps admin command 
        public bool AllMaps { get; set; } = false; // Enables or disables !allmaps admin command
        public bool TimeLeft { get; set; } = true; // Enables or disables !timeleft admin command
    }

    public class WarmupSettings
    {
        public float Time { get; set; } = 60; // Default warmup time
        public bool PerMap { get; set; } = false; // Enables or disables per map warmup
        public ModeEntry Default { get; set; } = new ModeEntry() { Name = "Knives Only", Config = $"warmup/knives_only.cfg", MapGroups = new List<string>()}; // Default warmup mode
        public List<ModeEntry> List { get; set; } = new List<ModeEntry>()
        {
            new ModeEntry() { Name = "Deathmatch", Config = $"warmup/dm.cfg", MapGroups = new List<string>() },
            new ModeEntry() { Name = "Knives Only", Config = $"warmup/knives_only.cfg", MapGroups = new List<string>()}
        };
    }
    public class RotationSettings
    {
        public bool Enabled { get; set; } = true; // Enables game rotations
        public bool WhenServerEmpty { get; set; } = false; // Enables rotation on server empty. 
        public bool EnforceCustomTimeLimit { get; set; } = false; // Enables or disables custom time limit
        public int CustomTimeLimit { get; set; } = 120; // Sets custom time limit in seconds
        public int Cycle { get; set; } = 0; // 0 for current mode maps, 1 for all maps, 2 for specific map groups
        public List<string> MapGroups { get; set;} = new List<string>(){"mg_active", "mg_comp"}; // Map group list for cycle 2
        public bool ModeRotation { get; set; } = false; // Enables game mode rotations
        public int ModeInterval { get; set; } = 4; // Changes mode every x map rotations
        public bool ModeSchedules {get; set;} = false; // Enables or disables mode schedules
        public List<ScheduleEntry> Schedule { get; set; } = new List<ScheduleEntry>()
        {
            new ScheduleEntry() { Time = "10:00", Mode = "Casual" },
            new ScheduleEntry() { Time = "15:00", Mode = "Practice" },
            new ScheduleEntry() { Time = "17:00", Mode = "Competitive" }
        }; // Schedule options
    }

    // Define game mode settings
    public class GameModeSettings
    {
        public float Delay { get; set; } = 2.0f; // Game mode change delay in seconds
        public string Style { get; set; } = "center"; // Changes mode menu type (i.e. "chat" or "center")
        public ModeEntry Default { get; set; } = new ModeEntry() { Name = "Casual", Config = "casual.cfg", MapGroups = new List<string>(){"mg_active", "mg_comp"} }; // Default mode on server start
        public string MapGroupFile { get; set; } = "gamemodes_server.txt"; // Default game modes and map groups file
        
        public List<ModeEntry> List { get; set; } = new List<ModeEntry>()
        {
            new ModeEntry() { Name = "Casual", Config = "casual.cfg", DefaultMap = "de_dust2", MapGroups = new List<string>(){"mg_active", "mg_comp"} },
            new ModeEntry() { Name = "Deathmatch", Config = "dm.cfg", DefaultMap = "de_assembly", MapGroups = new List<string>(){"mg_dm"}},
            new ModeEntry() { Name = "ArmsRace", Config = "armsrace.cfg", DefaultMap = "ar_pool_day", MapGroups = new List<string>(){"mg_gg"}},
            new ModeEntry() { Name = "Competitive", Config = "comp.cfg", DefaultMap = "de_dust2", MapGroups = new List<string>(){"mg_active", "mg_comp"}},
            new ModeEntry() { Name = "Wingman", Config = "wingman.cfg", DefaultMap = "de_memento", MapGroups = new List<string>(){"mg_active", "mg_comp"}},
            new ModeEntry() { Name = "Practice", Config = "prac.cfg", DefaultMap = "de_dust2", MapGroups = new List<string>(){"mg_prac"}},
            new ModeEntry() { Name = "Prefire", Config = "prefire.cfg", DefaultMap = "de_inferno", MapGroups = new List<string>(){"mg_comp"} },
            new ModeEntry() { Name = "Retakes", Config = "retake.cfg",  DefaultMap = "de_dust2", MapGroups = new List<string>(){"mg_retake"}},
            new ModeEntry() { Name = "Executes", Config = "executes.cfg", DefaultMap = "de_mirage", MapGroups = new List<string>(){"mg_executes"}},
            new ModeEntry() { Name = "Casual 1.6", Config = "Casual-1.6.cfg", DefaultMap = "3212419403", MapGroups = new List<string>(){"mg_active", "mg_comp"} },
            new ModeEntry() { Name = "Deathmatch Multicfg", Config = "dm-multicfg.cfg", DefaultMap = "de_mirage", MapGroups = new List<string>(){"mg_dm"}},
            new ModeEntry() { Name = "GG", Config = "gg.cfg", DefaultMap = "ar_pool_day", MapGroups = new List<string>(){"mg_gg"}},
            new ModeEntry() { Name = "45", Config = "45.cfg", DefaultMap = "3276886893", MapGroups = new List<string>(){"mg_45"} },
            new ModeEntry() { Name = "Awp", Config = "awp.cfg", DefaultMap = "3142070597", MapGroups = new List<string>(){"mg_awp"}},
            new ModeEntry() { Name = "1v1", Config = "1v1.cfg", DefaultMap = "3070253400", MapGroups = new List<string>(){"mg_1v1"}},
            new ModeEntry() { Name = "Aim", Config = "aim.cfg",  DefaultMap = "3084291314", MapGroups = new List<string>(){"mg_aim"}},
            new ModeEntry() { Name = "Bhop", Config = "bhop.cfg", DefaultMap = "3088973190", MapGroups = new List<string>(){"mg_bhop"}},
            new ModeEntry() { Name = "Surf", Config = "surf.cfg", DefaultMap = "3082548297", MapGroups = new List<string>(){"mg_surf"}},
            new ModeEntry() { Name = "KreedZ", Config = "kz.cfg", DefaultMap = "3086304337", MapGroups = new List<string>(){"mg_kz"}},
            new ModeEntry() { Name = "Hide N Seek", Config = "hns.cfg", DefaultMap = "3097563690", MapGroups = new List<string>(){"mg_hns"}},
            new ModeEntry() { Name = "Soccer", Config = "soccer.cfg", DefaultMap = "3070198374", MapGroups = new List<string>(){"mg_soccer"}},
            new ModeEntry() { Name = "Course", Config = "course.cfg", DefaultMap = "3070455802", MapGroups = new List<string>(){"mg_course"}},
            new ModeEntry() { Name = "Deathrun", Config = "deathrun.cfg", DefaultMap = "3164611860", MapGroups = new List<string>(){"mg_deathrun"}},
            new ModeEntry() { Name = "Minigames", Config = "minigames.cfg", DefaultMap = "3082120895", MapGroups = new List<string>(){"mg_minigames"}},
            new ModeEntry() { Name = "ScoutzKnivez", Config = "scoutzknivez.cfg", DefaultMap = "3073929825", MapGroups = new List<string>(){"mg_scoutzknivez"}},
        };
    }

    // Define configuration class
    public class Config : IBasePluginConfig
    {
        // Create config from classes
         public int Version { get; set; } = 7;
         public RTVSettings RTV { get; set; } = new();
         public MapSettings Maps { get; set; } = new();
         public VoteSettings Votes { get; set; } = new();
         public GameSettings Settings { get; set; } = new();
         public WarmupSettings Warmup { get; set; } = new();
         public CommandSettings Commands { get; set; } = new();
         public RotationSettings Rotation { get; set; } = new();
         public GameModeSettings GameModes { get; set; } = new();
    }

    // Define plugin class for parsing config
    public partial class Plugin : IPluginConfig<Config>
    {   
        // Define configuration object
        public required Config Config { get; set; }

        // Parse configuration object data and perform error checking
        public void OnConfigParsed(Config _config)
        {  
            if(_config.RTV.Enabled) 
            {
                // Disable game rotations
                _config.Rotation.Enabled = false;

                // Check if plugin DLL exists
                if (File.Exists(Path.Join(PluginState.GameDirectory, _config.RTV.Plugin)))
                {
                    _config.RTV.Plugin = Path.Join(PluginState.GameDirectory, _config.RTV.Plugin);
                }
                else
                {
                    Logger.LogError($"Cannot find RTV 'Plugin': {Path.Join(PluginState.GameDirectory, _config.RTV.Plugin)}");
                    throw new Exception($"Cannot find RTV 'Plugin': {Path.Join(PluginState.GameDirectory, _config.RTV.Plugin)}");
                }

                // Check if maplist exists
                if (File.Exists(Path.Join(PluginState.GameDirectory, _config.RTV.MapList))) 
                {
                    _config.RTV.MapList = Path.Join(PluginState.GameDirectory, _config.RTV.MapList);
                }
                else
                {
                    Logger.LogError($"Cannot find RTV 'MapListFile': {_config.RTV.MapList}");
                    throw new Exception($"Cannot find RTV 'MapListFile': {_config.RTV.MapList}");
                }
            }

            // Maps settings
            if (!_config.Maps.Style.Equals("center", StringComparison.OrdinalIgnoreCase) && !_config.Maps.Style.Equals("chat", StringComparison.OrdinalIgnoreCase)) 
            {
                Logger.LogError("Invalid: Style must be 'center' or 'chat'");
                throw new Exception("Invalid: Style must be 'center' or 'chat'");
            }
            if (String.IsNullOrEmpty(_config.Maps.Default)) 
            {
                Logger.LogError("Invalid: Default map must not be empty.");
                throw new Exception("Invalid: Default map must not be empty.");
            }

            // Vote Settings
            if (_config.Votes.Style.Equals("center", StringComparison.OrdinalIgnoreCase) && _config.Votes.Style.Equals("chat", StringComparison.OrdinalIgnoreCase)) 
            {
                Logger.LogError("Invalid: Style must be 'center' or 'chat'");
                throw new Exception("Invalid: Style must be 'center' or 'chat'");
            }

            // Game Settings
            if (_config.Settings.Enabled)
            {
                if (Directory.Exists(Path.Combine(PluginState.ConfigDirectory, _config.Settings.Folder)))
                {
                    PluginState.SettingsDirectory = Path.Join(PluginState.ConfigDirectory, _config.Settings.Folder);
                }
                else
                {
                    Logger.LogError($"Cannot find 'Settings Folder': {PluginState.SettingsDirectory}");
                    throw new Exception($"Cannot find 'Settings Folder': {PluginState.SettingsDirectory}");
                }
            }

            // Game mode settings
            if (File.Exists(Path.Join(PluginState.GameDirectory, _config.GameModes.MapGroupFile)))  
            {
                _config.GameModes.MapGroupFile = Path.Join(PluginState.GameDirectory, _config.GameModes.MapGroupFile);
            }
            else
            {
                Logger.LogError($"Cannot find map group file: {_config.GameModes.MapGroupFile}");
                throw new Exception($"Cannot find map group file: {_config.GameModes.MapGroupFile}");
            }
            
            if(_config.GameModes.List.Count == 0)
            {
                Logger.LogError("Undefined: Game modes list cannot be empty.");
                throw new Exception("Undefined: Game modes list cannot be empty.");
            }
            
            if (_config.GameModes.Style.Equals("center", StringComparison.OrdinalIgnoreCase) && _config.GameModes.Style.Equals("chat", StringComparison.OrdinalIgnoreCase)) 
            {
                Logger.LogError("Invalid: Style must be 'center' or 'chat'");
                throw new Exception("Invalid: Style must be 'center' or 'chat'");
            }

            if (_config.GameModes.Default.Equals(null)) 
            {
                Logger.LogError("Invalid: Default game mode must not be empty.");
                throw new Exception("Invalid: Default game mode must not be empty.");
            }
           
            // Rotation settings
            if (_config.Rotation.Cycle != 0 && _config.Rotation.Cycle != 1 && _config.Rotation.Cycle != 2)
            {
                Logger.LogError("Invalid: Rotation cycle must be 0, 1, or 2.");
                throw new Exception("Invalid: Rotation cycle must be 0, 1, or 2.");
            }

            if (_config.Rotation.ModeSchedules == true)
            {
                if (_config.Rotation.Schedule.Count <= 0)
                {
                    Logger.LogError("Invalid: Schedule cannot be empty");
                    throw new Exception("Invalid: Schedule cannot be empty");
                }
            }

            // Config version check
            if (_config.Version < 7)
            {
                throw new Exception("Your config file is too old, please backup and remove it from addons/counterstrikesharp/configs/plugins/GameModeManager to recreate it");
            }

            // Set config
            Config = _config;

            // Load dependencies
            _dependencyManager.OnConfigParsed(_config);
        }
    }
}