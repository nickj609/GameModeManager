// Included libraries
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager
{
    // Define settings classes
    public class ScheduleEntry
    {
        public object? TimerState { get; set; }
        public string Time { get; set; } = "10:00"; // Time of the rotation (e.g., "10:00")
        public string Mode { get; set; } = "Casual"; // Optional: Game mode to rotate to (if specified)
    }

    public class Schedule
    {
        public List<ScheduleEntry> Entries { get; set; } = new List<ScheduleEntry>()
        {
            new ScheduleEntry() { Time = "10:00", Mode = "Casual" },
            new ScheduleEntry() { Time = "15:00", Mode = "Practice" },
            new ScheduleEntry() { Time = "17:00", Mode = "Competitive" }
        };
    }

    public class RTVSettings
    {
        public bool Enabled { get; set; } = false; // Enable RTV Compatibility
        public string Plugin { get; set; } = "addons/counterstrikesharp/plugins/RockTheVote/RockTheVote.dll"; // RTV plugin path
        public string MapListFile { get; set; } = "addons/counterstrikesharp/plugins/RockTheVote/maplist.txt"; // Default map list file
        public bool DefaultMapFormat { get; set; } = false; // Default file format (ws:<workshop id>). When set to false, uses format <map name>:<workshop id>. 
    
    }
    public class GameSettings
    {
        public bool Enabled { get; set; } = true; // Enable game settings
        public string Folder { get; set; } = "settings"; // Default settings folder
        public string Style { get; set; } = "center"; // Changes settings menu type (i.e. "chat" or "center")
    }
    public class MapGroupSettings
    {
        public float Delay { get; set; } = 2.0f; // Map change delay in seconds
        public string Default { get; set; } = "mg_active"; // Default map group on server start
        public string DefaultMap { get; set; } =  "de_dust2"; // Default map on server start
        public string Style { get; set; } = "center"; // Changes map menu type (i.e. "chat" or "center")
        public string File { get; set; } = "gamemodes_server.txt"; // Default game modes and map groups file
    }
    public class GameModeSettings
    {
        public bool Rotation { get; set; } = true; // Enables game mode rotation
        public int Interval { get; set; } = 4; // Changes game mode every x map rotations
        public string Default { get; set; } =  "Casual"; // Default mode on server start
        public float Delay { get; set; } = 2.0f; // Game mode change delay in seconds
        public string Style { get; set; } = "center"; // Changes mode menu type (i.e. "chat" or "center")
        public Dictionary<string, Dictionary<string, List<string>>> List { get; set; } = 
        new Dictionary<string, Dictionary<string, List<string>>>()
        {
            { "Casual", new Dictionary<string, List<string>>()
            {
            { "casual.cfg", new List<string>() { "mg_active", "mg_casual" }}
            }
            },
            { "Competitive", new Dictionary<string, List<string>>()
            {
            { "comp.cfg", new List<string>() { "mg_active", "mg_comp" }}
            }
            },
            { "Wingman", new Dictionary<string, List<string>>()
            {
            { "wingman.cfg", new List<string>() { "mg_wingman"}}
            }
            },
            { "Practice", new Dictionary<string, List<string>>()
            {
            { "prac.cfg", new List<string>() { "mg_prac"}}
            }
            },
            { "Deathmatch", new Dictionary<string, List<string>>()
            {
            { "dm.cfg", new List<string>() { "mg_dm"}}
            }
            },
            { "Deathmatch Multicfg", new Dictionary<string, List<string>>()
            {
            { "dm-multicfg.cfg", new List<string>() { "mg_dm"}}
            }
            },
            { "ArmsRace", new Dictionary<string, List<string>>()
            {
            { "ar.cfg", new List<string>() { "mg_armsrace"}}
            }
            },
            { "GunGame", new Dictionary<string, List<string>>()
            {
            { "gg.cfg", new List<string>() { "mg_gg"}}
            }
            },
            { "Retakes", new Dictionary<string, List<string>>()
            {
            { "retake.cfg", new List<string>() { "mg_retakes"}}
            }
            },
            { "Executes", new Dictionary<string, List<string>>()
            {
            { "executes.cfg", new List<string>() { "mg_executes"}}
            }
            },
            { "1v1", new Dictionary<string, List<string>>()
            {
            { "1v1.cfg", new List<string>() { "mg_1v1"}}
            }
            },
            { "Aim", new Dictionary<string, List<string>>()
            {
            { "aim.cfg", new List<string>() { "mg_aim"}}
            }
            },
            { "Bhop", new Dictionary<string, List<string>>()
            {
            { "bhop.cfg", new List<string>() { "mg_bhop"}}
            }
            },
            { "Surf", new Dictionary<string, List<string>>()
            {
            { "surf.cfg", new List<string>() { "mg_surf"}}
            }
            },
            { "Kreedz", new Dictionary<string, List<string>>()
            {
            { "kz.cfg", new List<string>() { "mg_kz"}}
            }
            },
            { "Awp", new Dictionary<string, List<string>>()
            {
            { "awp.cfg", new List<string>() { "mg_awp"}}
            }
            },
            { "Course", new Dictionary<string, List<string>>()
            {
            { "course.cfg", new List<string>() { "mg_course"}}
            }
            },
            { "Hide N Seek", new Dictionary<string, List<string>>()
            {
            { "hns.cfg", new List<string>() { "mg_hns"}}
            }
            },
            { "Soccer", new Dictionary<string, List<string>>()
            {
            { "soccer.cfg", new List<string>() { "mg_soccer"}}
            }
            },
            { "Minigames", new Dictionary<string, List<string>>()
            {
            { "minigames.cfg", new List<string>() { "mg_minigames"}}
            }
            },
            
        };
        public bool ScheduleEnabled {get; set;} = false; // Enables or disables rotation schedule
        public Schedule Schedule { get; set; } = new Schedule(); // Schedule options
    }
    public class VoteSettings
    {
        public bool Enabled { get; set; } = false; // Enables CS2-CustomVotes compatibility
        public bool Map { get; set; } = false; // Enables vote to change game to a specific map in the current mode
        public bool AllMap { get; set; } = false; // Enables vote to change game to a specific map in all modes
        public bool GameMode { get; set; } = false; // Enables vote to change game mode
        public bool GameSetting { get; set; } = false; // Enables vote to change game setting
        public string Style { get; set; } = "center"; // Changes vote menu type (i.e. "chat" or "center")
    }

    public class CommandSettings
    {
        public bool Map { get; set; } = true; // Enables or disables !map command for CS2-SimpleAdmin compatibility
    }

    public class Config : IBasePluginConfig
    {
        // Create config from classes
         public int Version { get; set; } = 4;
         public RTVSettings RTV { get; set; } = new();
         public VoteSettings Votes { get; set; } = new();
         public GameSettings Settings { get; set; } = new();
         public CommandSettings Commands { get; set; } = new();
         public MapGroupSettings MapGroups { get; set; } = new();
         public GameModeSettings GameModes { get; set; } = new();
    }

    public partial class Plugin : IPluginConfig<Config>
    {   
        // Define configuration object
        public required Config Config { get; set; }

        // Parse configuration object data and perform error checking
        public void OnConfigParsed(Config _config)
        {  
            // RTV settings
            if (_config.RTV.Enabled != true && _config.RTV.Enabled != false) 
            {
                Logger.LogError("Invalid: RTV 'Enabled' should be 'true' or 'false'.");
                throw new Exception("Invalid: RTV 'Enabled' should be 'true' or 'false'.");
            }
            else if(_config.RTV.Enabled) 
            {
                // Set RTV flag
                _pluginState.RTVEnabled = _config.RTV.Enabled;

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
                if (File.Exists(Path.Join(PluginState.GameDirectory, _config.RTV.MapListFile))) 
                {
                    _config.RTV.MapListFile = Path.Join(PluginState.GameDirectory, _config.RTV.MapListFile);
                }
                else
                {
                    Logger.LogError($"Cannot find RTV 'MapListFile': {_config.RTV.MapListFile}");
                    throw new Exception($"Cannot find RTV 'MapListFile': {_config.RTV.MapListFile}");
                }

                // Check if DefaultMapFormat is true or false
                if (_config.RTV.DefaultMapFormat != true && _config.RTV.DefaultMapFormat != false)
                {
                    Logger.LogError("Invalid: RTV 'DefaultMapFormat' should be 'true' or 'false'.");
                    throw new Exception("Invalid: RTV 'DefaultMapFormat' should be 'true' or 'false'.");
                }
            }

            // Map group settings
            if (!float.TryParse(_config.MapGroups.Delay.ToString(), out _))  
            {
                Logger.LogError("Map group delay must be a number.");
                throw new Exception("Map group delay must be a number.");
            }
            if (_config.MapGroups.Default == null) 
            {
                Logger.LogError("Undefined: Default map group can not be empty.");
                throw new Exception("Undefined: Default map group can not be empty.");
            }
            if (File.Exists(Path.Join(PluginState.GameDirectory, _config.MapGroups.File)))  
            {
                _config.MapGroups.File = Path.Join(PluginState.GameDirectory, _config.MapGroups.File);
            }
            else
            {
                Logger.LogError($"Cannot find map group file: {_config.MapGroups.File}");
                throw new Exception($"Cannot find map group file: {_config.MapGroups.File}");
            }

            // Game mode settings
             if (_config.GameModes.Rotation != true && _config.GameModes.Rotation != false) 
            {
                Logger.LogError("Invalid: Game mode rotation should be 'true' or 'false'.");
                throw new Exception("Invalid: Game mode rotation should be 'true' or 'false'.");
            }
            if (!float.TryParse(_config.GameModes.Delay.ToString(), out _)) 
            {
                Logger.LogError("Game mode delay must be a number.");
                throw new Exception("Game mode delay must be a number.");
            }
            if (!int.TryParse(_config.GameModes.Interval.ToString(), out _)) 
            {
                Logger.LogError("Game mode interval must be a number.");
                throw new Exception("Game mode interval must be a number.");
            }
            if(_config.GameModes.List == null || _config.GameModes.List.Count == 0)
            {
                Logger.LogError("Undefined: Game mode list cannot be empty.");
                throw new Exception("Undefined: Game mode list cannot be empty.");
            }

            // Game Settings
            if (_config.Settings.Enabled != true && _config.Settings.Enabled != false) 
            {
                Logger.LogError("Invalid: Game setting should be 'true' or 'false'.");
                throw new Exception("Invalid: Game setting should be 'true' or 'false'.");
            }
            else if (_config.Settings.Enabled)
            {
                if (System.IO.Directory.Exists(Path.Combine(PluginState.ConfigDirectory, _config.Settings.Folder)))
                {
                    PluginState.SettingsDirectory = Path.Join(PluginState.ConfigDirectory, _config.Settings.Folder);
                }
                else
                {
                    Logger.LogError($"Cannot find 'Settings Folder': {PluginState.SettingsDirectory}");
                    throw new Exception($"Cannot find 'Settings Folder': {PluginState.SettingsDirectory}");
                }
            }

            // Vote Settings
            if (_config.Votes.Enabled != true && _config.Votes.Enabled != false) 
            {
                Logger.LogError("Invalid: votes enabled should be 'true' or 'false'.");
                throw new Exception("Invalid: votes enabled should be 'true' or 'false'.");
            }
            if (_config.Votes.GameMode != true &&_config.Votes.GameMode != false) 
            {
                Logger.LogError("Invalid: game mode votes should be 'true' or 'false'.");
                throw new Exception("Invalid: game mode vote should be 'true' or 'false'.");
            }
            if (_config.Votes.GameSetting != true && _config.Votes.GameSetting != false) 
            {
                Logger.LogError("Invalid: game setting votes should be 'true' or 'false'.");
                throw new Exception("Invalid: game setting vote should be 'true' or 'false'.");
            }

            if (_config.Votes.Map != true && _config.Votes.Map != false) 
            {
                Logger.LogError("Invalid: map vote should be 'true' or 'false'.");
                throw new Exception("Invalid: map vote should be 'true' or 'false'.");
            }

            // Config version check
            if (_config.Version < 4)
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