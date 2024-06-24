// Included libraries
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager
{
    // Define class dependencies
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

    // Define RTV settings
    public class RTVSettings
    {
        public bool Enabled { get; set; } = false; // Enable RTV Compatibility
        public bool DefaultMapFormat { get; set; } = false; // Default file format (ws:<workshop id>). When set to false, uses format <map name>:<workshop id>. 
        public string Plugin { get; set; } = "addons/counterstrikesharp/plugins/RockTheVote/RockTheVote.dll"; // RTV plugin path
        public string MapList { get; set; } = "addons/counterstrikesharp/plugins/RockTheVote/maplist.txt"; // Default map list file
    }

    // Define map settings
    public class MapSettings
    {
        public int Cycle { get; set; } = 0; // 0 for mode maps, 1 for all maps
        public float Delay { get; set; } = 2.0f; // Map change delay in seconds
        public string Style { get; set; } = "center"; // Changes map menu type 
        public string Default { get; set; } =  "de_dust2"; // Default map on server start
    }

    // Define vote settings
    public class VoteSettings
    {
        public bool Enabled { get; set; } = false; // Enables CS2-CustomVotes compatibility
        public bool Maps { get; set; } = false; // Enables vote to change game to a specific map in the current mode
        public bool AllMaps { get; set; } = false; // Enables vote to change game to a specific map in all modes
        public bool GameModes { get; set; } = false; // Enables vote to change game mode
        public bool GameSettings { get; set; } = false; // Enables vote to change game setting
        public string Style { get; set; } = "center"; // Changes vote menu type (i.e. "chat" or "center")
    }

    // Define game settings
    public class GameSettings
    {
        public bool Enabled { get; set; } = true; // Enable game settings
        public string Style { get; set; } = "center"; // Changes settings menu type
        public string Folder { get; set; } = "settings"; // Default settings folder
    }

    // Define command settings
    public class CommandSettings
    {
        public bool Map { get; set; } = true; // Enables or disables !map command
        public bool Maps { get; set; } = true; // Enables or disables !maps command 
        public bool AllMaps { get; set; } = true; // Enables or disables !allmaps command
    }

    // Define game mode settings
    public class GameModeSettings
    {
        public float Delay { get; set; } = 2.0f; // Game mode change delay in seconds
        public int Interval { get; set; } = 4; // Changes mode every x map rotations
        public bool Rotation { get; set; } = true; // Enables game mode rotations
        public string Style { get; set; } = "center"; // Changes mode menu type 
        public string Default { get; set; } =  "Casual"; // Default mode on server start
        public string MapGroupFile { get; set; } = "gamemodes_server.txt"; // Default game modes and map groups file
        
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

    // Define configuration class
    public class Config : IBasePluginConfig
    {
        // Create config from classes
         public int Version { get; set; } = 5;
         public RTVSettings RTV { get; set; } = new();
         public MapSettings Maps { get; set; } = new();
         public VoteSettings Votes { get; set; } = new();
         public GameSettings Settings { get; set; } = new();
         public CommandSettings Commands { get; set; } = new();
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
                if (File.Exists(Path.Join(PluginState.GameDirectory, _config.RTV.MapList))) 
                {
                    _config.RTV.MapList = Path.Join(PluginState.GameDirectory, _config.RTV.MapList);
                }
                else
                {
                    Logger.LogError($"Cannot find RTV 'MapListFile': {_config.RTV.MapList}");
                    throw new Exception($"Cannot find RTV 'MapListFile': {_config.RTV.MapList}");
                }

                // Check if DefaultMapFormat is true or false
                if (_config.RTV.DefaultMapFormat != true && _config.RTV.DefaultMapFormat != false)
                {
                    Logger.LogError("Invalid: RTV 'DefaultMapFormat' should be 'true' or 'false'.");
                    throw new Exception("Invalid: RTV 'DefaultMapFormat' should be 'true' or 'false'.");
                }
            }

            // Maps settings
            if (!int.TryParse(_config.Maps.Cycle.ToString(), out _))  
            {
                Logger.LogError("Maps cycle must be a number.");
                throw new Exception("Maps cycle must be a number.");
            }
            if (!float.TryParse(_config.Maps.Delay.ToString(), out _))  
            {
                Logger.LogError("Maps delay must be a number.");
                throw new Exception("Maps delay must be a number.");
            }
            if (_config.Maps.Style.ToLower() != "center" && _config.Maps.Style.ToLower() != "chat") 
            {
                Logger.LogError("Invalid: Style must be 'center' or 'chat'");
                throw new Exception("Invalid: Style must be 'center' or 'chat'");
            }
            if (_config.Maps.Default == null) 
            {
                Logger.LogError("Invalid: Default map must not be empty.");
                throw new Exception("Invalid: Default map must not be empty.");
            }

            // Vote Settings
            if (_config.Votes.Enabled != true && _config.Votes.Enabled != false) 
            {
                Logger.LogError("Invalid: votes enabled should be 'true' or 'false'.");
                throw new Exception("Invalid: votes enabled should be 'true' or 'false'.");
            }
            if (_config.Votes.GameModes != true &&_config.Votes.GameModes != false) 
            {
                Logger.LogError("Invalid: game modes vote should be 'true' or 'false'.");
                throw new Exception("Invalid: game modes vote should be 'true' or 'false'.");
            }
            if (_config.Votes.GameSettings != true && _config.Votes.GameSettings != false) 
            {
                Logger.LogError("Invalid: game settings vote should be 'true' or 'false'.");
                throw new Exception("Invalid: game settings vote should be 'true' or 'false'.");
            }
            if (_config.Votes.Maps != true && _config.Votes.Maps != false) 
            {
                Logger.LogError("Invalid: maps vote should be 'true' or 'false'.");
                throw new Exception("Invalid: maps vote should be 'true' or 'false'.");
            }
            if (_config.Votes.AllMaps != true && _config.Votes.AllMaps != false) 
            {
                Logger.LogError("Invalid: all maps vote should be 'true' or 'false'.");
                throw new Exception("Invalid: maps vote should be 'true' or 'false'.");
            }
            if (_config.Votes.Style.ToLower() != "center" && _config.Votes.Style.ToLower() != "chat") 
            {
                Logger.LogError("Invalid: Style must be 'center' or 'chat'");
                throw new Exception("Invalid: Style must be 'center' or 'chat'");
            }

            // Game Settings
            if (_config.Settings.Enabled != true && _config.Settings.Enabled != false) 
            {
                Logger.LogError("Invalid: Game settings should be 'true' or 'false'.");
                throw new Exception("Invalid: Game settings should be 'true' or 'false'.");
            }
            else if (_config.Settings.Enabled)
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

            // Command settings
            if (_config.Commands.Map != true && _config.Commands.Map != false)
            {
                Logger.LogError("Invalid: Map command setting should be 'true' or 'false'.");
                throw new Exception("Invalid: Map command setting should be 'true' or 'false'.");
            }
            if (_config.Commands.Maps != true && _config.Commands.Maps != false)
            {
                Logger.LogError("Invalid: Maps command setting should be 'true' or 'false'.");
                throw new Exception("Invalid: Maps command setting should be 'true' or 'false'.");
            }
            if (_config.Commands.AllMaps != true && _config.Commands.AllMaps != false)
            {
                Logger.LogError("Invalid: All maps command setting should be 'true' or 'false'.");
                throw new Exception("Invalid: All maps command setting should be 'true' or 'false'.");
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
            if (_config.GameModes.Rotation != true && _config.GameModes.Rotation != false) 
            {
                Logger.LogError("Invalid: Game mode rotation should be 'true' or 'false'.");
                throw new Exception("Invalid: Game mode rotation should be 'true' or 'false'.");
            }
            else if(_config.GameModes.Rotation == true)
            {
                if (!int.TryParse(_config.GameModes.Interval.ToString(), out _)) 
                {
                    Logger.LogError("Game modes interval must be a number.");
                    throw new Exception("Game modes interval must be a number.");
                }
            }
            if (!float.TryParse(_config.GameModes.Delay.ToString(), out _)) 
            {
                Logger.LogError("Game modes delay must be a number.");
                throw new Exception("Game modes delay must be a number.");
            }
            
            if(_config.GameModes.List == null || _config.GameModes.List.Count == 0)
            {
                Logger.LogError("Undefined: Game modes list cannot be empty.");
                throw new Exception("Undefined: Game modes list cannot be empty.");
            }
            if (_config.GameModes.ScheduleEnabled != true && _config.GameModes.ScheduleEnabled != false) 
            {
                Logger.LogError("Invalid: Schedule Enabled should be 'true' or 'false'.");
                throw new Exception("Invalid: Schedule Enabled should be 'true' or 'false'.");
            }
            if (_config.GameModes.ScheduleEnabled == true && _config.GameModes.Schedule == null) 
            {
                Logger.LogError("Invalid: Schedule cannot be empty");
                throw new Exception("Invalid: Schedule cannot be empty");
            }
            if (_config.GameModes.Style.ToLower() != "center" && _config.GameModes.Style.ToLower() != "chat") 
            {
                Logger.LogError("Invalid: Style must be 'center' or 'chat'");
                throw new Exception("Invalid: Style must be 'center' or 'chat'");
            }
            if (_config.GameModes.Default == null) 
            {
                Logger.LogError("Invalid: Default game mode must not be empty.");
                throw new Exception("Invalid: Default game mode must not be empty.");
            }

            // Config version check
            if (_config.Version < 5)
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