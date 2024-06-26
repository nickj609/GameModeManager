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
        public List<string> MapGroups {get; set;} = new List<string>(){"mg_active", "mg_delta"};
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
        public bool TimeLeft { get; set; } = true; // Enables or disables !allmaps command
    }

    public class RotationSettings
    {
        public bool Enabled { get; set; } = true; //
        public int Cycle { get; set; } = 0; // 0 for current mode maps, 1 for all maps, 2 for specific map groups
        public List<string> MapGroups { get; set;} = new List<string>(){"mg_active", "mg_delta"};
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
        public string Style { get; set; } = "center"; // Changes mode menu type 
        public string Default { get; set; } =  "Casual"; // Default mode on server start
        public string MapGroupFile { get; set; } = "gamemodes_server.txt"; // Default game modes and map groups file
        
        public List<ModeEntry> List { get; set; } = new List<ModeEntry>()
        {
            new ModeEntry() { Name = "Casual", Config = "casual.cfg", MapGroups = new List<string>(){"mg_active", "mg_delta"} },
            new ModeEntry() { Name = "Competitive", Config = "comp.cfg", MapGroups = new List<string>(){"mg_active", "mg_delta"}},
            new ModeEntry() { Name = "Wingman", Config = "wingman.cfg", MapGroups = new List<string>(){"mg_active", "mg_delta"}},
            new ModeEntry() { Name = "Practice", Config = "prac.cfg", MapGroups = new List<string>(){"mg_prac"}},
            new ModeEntry() { Name = "Deathmatch", Config = "dm.cfg", MapGroups = new List<string>(){"mg_dm"}},
            new ModeEntry() { Name = "Deathmatch Multicfg", Config = "dm-multicfg.cfg", MapGroups = new List<string>(){"mg_dm"}},
            new ModeEntry() { Name = "ArmsRace", Config = "armsrace.cfg", MapGroups = new List<string>(){"mg_gg"}},
            new ModeEntry() { Name = "GunGame", Config = "gg.cfg", MapGroups = new List<string>(){"mg_gg"}},
            new ModeEntry() { Name = "Retakes", Config = "retake.cfg", MapGroups = new List<string>(){"mg_retake"}},
            new ModeEntry() { Name = "Executes", Config = "executes.cfg", MapGroups = new List<string>(){"mg_executes"}},
            new ModeEntry() { Name = "1v1", Config = "1v1.cfg", MapGroups = new List<string>(){"mg_1v1"}},
            new ModeEntry() { Name = "Aim", Config = "aim.cfg", MapGroups = new List<string>(){"mg_aim"}},
            new ModeEntry() { Name = "Bhop", Config = "bhop.cfg", MapGroups = new List<string>(){"mg_bhop"}},
            new ModeEntry() { Name = "Surf", Config = "surf.cfg", MapGroups = new List<string>(){"mg_surf"}},
            new ModeEntry() { Name = "KreedZ", Config = "kz.cfg", MapGroups = new List<string>(){"mg_kz"}},
            new ModeEntry() { Name = "Awp", Config = "awp.cfg", MapGroups = new List<string>(){"mg_awp"}},
            new ModeEntry() { Name = "Course", Config = "course.cfg", MapGroups = new List<string>(){"mg_course"}},
            new ModeEntry() { Name = "Hide N Seek", Config = "hns.cfg", MapGroups = new List<string>(){"mg_hns"}},
            new ModeEntry() { Name = "Soccer", Config = "soccer.cfg", MapGroups = new List<string>(){"mg_soccer"}},
            new ModeEntry() { Name = "Minigames", Config = "minigames.cfg", MapGroups = new List<string>(){"mg_minigames"}}
        };
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

                // Check if MapFormat is true or false
                if (_config.RTV.MapFormat != true && _config.RTV.MapFormat != false)
                {
                    Logger.LogError("Invalid: RTV 'MapFormat' should be 'true' or 'false'.");
                    throw new Exception("Invalid: RTV 'MapFormat' should be 'true' or 'false'.");
                }

                if (!int.TryParse(_config.RTV.Mode.ToString(), out _)) 
                {
                    Logger.LogError("RTV mode must be a number.");
                    throw new Exception("RTV mode must be a number.");
                }
            }

            // Maps settings
            if (!int.TryParse(_config.Rotation.Cycle.ToString(), out _))  
            {
                Logger.LogError("Maps cycle must be a number.");
                throw new Exception("Maps cycle must be a number.");
            }
            if (!float.TryParse(_config.Maps.Delay.ToString(), out _))  
            {
                Logger.LogError("Maps delay must be a number.");
                throw new Exception("Maps delay must be a number.");
            }
            if (!_config.Maps.Style.Equals("center", StringComparison.OrdinalIgnoreCase) && !_config.Maps.Style.Equals("chat", StringComparison.OrdinalIgnoreCase)) 
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
            if (_config.Votes.Style.Equals("center", StringComparison.OrdinalIgnoreCase) && _config.Votes.Style.Equals("chat", StringComparison.OrdinalIgnoreCase)) 
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
            if (_config.Commands.TimeLeft != true && _config.Commands.TimeLeft != false)
            {
                Logger.LogError("Invalid: All maps command setting should be 'true' or 'false'.");
                throw new Exception("Invalid: All maps command setting should be 'true' or 'false'.");
            }
            else if (_config.Commands.TimeLeft == true && !_config.RTV.Enabled == true)
            {
                Logger.LogError("Invalid: TimeLeft command cannot be enabled with RTV.");
                throw new Exception("Invalid: TimeLeft command cannot be enabled with RTV.");
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
            if (_config.GameModes.Style.Equals("center", StringComparison.OrdinalIgnoreCase) && _config.GameModes.Style.Equals("chat", StringComparison.OrdinalIgnoreCase)) 
            {
                Logger.LogError("Invalid: Style must be 'center' or 'chat'");
                throw new Exception("Invalid: Style must be 'center' or 'chat'");
            }
            if (_config.GameModes.Default == null) 
            {
                Logger.LogError("Invalid: Default game mode must not be empty.");
                throw new Exception("Invalid: Default game mode must not be empty.");
            }

            // Rotation settings
             if (_config.Rotation.Enabled != true && _config.Rotation.Enabled != false) 
            {
                Logger.LogError("Invalid: Rotation Enabled should be 'true' or 'false'.");
                throw new Exception("Invalid: Rotation Enabled should be 'true' or 'false'.");
            }
            else if (_config.Rotation.Enabled == true && _config.RTV.Enabled == true)
            {
                Logger.LogError("Invalid: RTV and Rotation cannot be both enabled.");
                throw new Exception("Invalid: RTV and Rotation cannot be both enabled.");
            }
            else if(_config.Rotation.Enabled == true)
            {
                if (_config.Rotation.Cycle != 0 && _config.Rotation.Cycle != 1 && _config.Rotation.Cycle != 2)
                {
                    Logger.LogError("Invalid: Rotation cycle must be 0, 1, or 2.");
                    throw new Exception("Invalid: Rotation cycle must be 0, 1, or 2.");
                }
                if (_config.Rotation.ModeRotation != true && _config.Rotation.ModeRotation != false)
                {
                    Logger.LogError("Invalid: Mode Rotation must be 'true' or 'false'.");
                    throw new Exception("Invalid: Mode Rotation must be 'true' or 'false'.");
                }
                else if (_config.Rotation.ModeRotation == true)
                {
                    if (!int.TryParse(_config.Rotation.ModeInterval.ToString(), out _)) 
                    {
                        Logger.LogError("Game modes interval must be a number.");
                        throw new Exception("Game modes interval must be a number.");
                    }
                }
                if (_config.Rotation.ModeSchedules != true && _config.Rotation.ModeSchedules != false) 
                {
                    Logger.LogError("Invalid: Schedule Enabled should be 'true' or 'false'.");
                    throw new Exception("Invalid: Schedule Enabled should be 'true' or 'false'.");
                }
                else if (_config.Rotation.ModeSchedules == true)
                {
                    if (_config.Rotation.Schedule == null || _config.Rotation.Schedule.Count <= 0)
                    {
                        Logger.LogError("Invalid: Schedule cannot be empty");
                        throw new Exception("Invalid: Schedule cannot be empty");
                    }
                }
                else if (_config.Rotation.ModeSchedules == true && _config.Rotation.ModeRotation == true)
                {
                    Logger.LogError("Invalid: Mode rotation and schedule cannot be both enabled.");
                    throw new Exception("Invalid: Mode rotation and schedule cannot be both enabled.");
                }
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