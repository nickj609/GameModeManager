// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin, IPluginConfig<Config>
    {   
        // Define configuration object
        public required Config Config { get; set; }

        // Define directories (Thanks Kus!)
        public static string GameDirectory = Path.Join(Server.GameDirectory + "/csgo/");
        public static string ConfigDirectory = Path.Join(GameDirectory + "cfg/");
        public static string SettingsDirectory = Path.Join(ConfigDirectory + "settings/");

        // Parse configuration object data and perform error checking
        public void OnConfigParsed(Config _config)
        {  
            // RTV settings
            if (_config.RTV.Enabled != true && _config.RTV.Enabled != false) 
            {
                Logger.LogError("Invalid: RTV 'Enabled' should be 'true' or 'false'.");
                throw new Exception("Invalid: RTV 'Enabled' should be 'true' or 'false'.");
            }
            else if(_config.RTV.Enabled == true) 
            {
                // Set RTV flag
                _RTV = _config.RTV.Enabled;

                // Check if plugin DLL exists
                if (File.Exists(Path.Join(GameDirectory, _config.RTV.Plugin)))
                {
                    _config.RTV.Plugin = Path.Join(GameDirectory, _config.RTV.Plugin);
                }
                else
                {
                    Logger.LogError($"Cannot find RTV 'Plugin': {Path.Join(GameDirectory, _config.RTV.Plugin)}");
                    throw new Exception($"Cannot find RTV 'Plugin': {Path.Join(GameDirectory, _config.RTV.Plugin)}");
                }

                // Check if maplist exists
                if (File.Exists(Path.Join(GameDirectory, _config.RTV.MapListFile))) 
                {
                    _config.RTV.MapListFile = Path.Join(GameDirectory, _config.RTV.MapListFile);
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
            if (!float.TryParse(_config.MapGroup.Delay.ToString(), out _))  
            {
                Logger.LogError("Map group delay must be a number.");
                throw new Exception("Map group delay must be a number.");
            }
            if (_config.MapGroup.Default == null) 
            {
                Logger.LogError("Undefined: Default map group can not be empty.");
                throw new Exception("Undefined: Default map group can not be empty.");
            }
            if (File.Exists(Path.Join(GameDirectory, _config.MapGroup.File)))  
            {
                _config.MapGroup.File = Path.Join(GameDirectory, _config.MapGroup.File);
            }
            else
            {
                Logger.LogError($"Cannot find map group file: {_config.MapGroup.File}");
                throw new Exception($"Cannot find map group file: {_config.MapGroup.File}");
            }

            // Game mode settings
             if (_config.GameMode.Rotation != true && _config.GameMode.Rotation != false) 
            {
                Logger.LogError("Invalid: Game mode rotation should be 'true' or 'false'.");
                throw new Exception("Invalid: Game mode rotation should be 'true' or 'false'.");
            }
            if (!float.TryParse(_config.GameMode.Delay.ToString(), out _)) 
            {
                Logger.LogError("Game mode delay must be a number.");
                throw new Exception("Game mode delay must be a number.");
            }
            if (!int.TryParse(_config.GameMode.Interval.ToString(), out _)) 
            {
                Logger.LogError("Game mode interval must be a number.");
                throw new Exception("Game mode interval must be a number.");
            }
            if (_config.GameMode.ListEnabled != true && _config.GameMode.ListEnabled != false) 
            {
                Logger.LogError("Invalid: Game mode list enabled should be 'true' or 'false'.");
                throw new Exception("Invalid: Game mode list enabled should be 'true' or 'false'.");
            }
            else if (_config.GameMode.ListEnabled == true)
            {
                if(_config.GameMode.List == null || _config.GameMode.List.Count == 0)
                {
                    Logger.LogError("Undefined: Game mode list cannot be empty.");
                    throw new Exception("Undefined: Game mode list cannot be empty.");
                }
            }

            // Game Settings
            if (_config.Settings.Enabled != true && _config.Settings.Enabled != false) 
            {
                Logger.LogError("Invalid: Game setting should be 'true' or 'false'.");
                throw new Exception("Invalid: Game setting should be 'true' or 'false'.");
            }
            else if (_config.Settings.Enabled == true)
            {
                if (System.IO.Directory.Exists(Path.Combine(ConfigDirectory, _config.Settings.Folder)))
                {
                    SettingsDirectory = Path.Join(ConfigDirectory, _config.Settings.Folder);
                }
                else
                {
                    Logger.LogError($"Cannot find 'Settings Folder': {SettingsDirectory}");
                    throw new Exception($"Cannot find 'Settings Folder': {SettingsDirectory}");
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
            if (_config.Version < 3)
            {
                Logger.LogError("Your config file is too old, please backup and remove it from addons/counterstrikesharp/configs/plugins/GameModeManager to recreate it.");
                throw new Exception("Your config file is too old, please backup and remove it from addons/counterstrikesharp/configs/plugins/GameModeManager to recreate it");
            }

            // Set config
            Config = _config;
        }
    }
    public class Config : BasePluginConfig
    {
        // Define settings classes
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
            public float Delay { get; set; } = 2.0f; // Game mode change delay in seconds
            public string Style { get; set; } = "center"; // Changes mode menu type (i.e. "chat" or "center")
            public bool ListEnabled { get; set; } = true; // Enables custom game mode list. If set to false, generated from map groups.
            public Dictionary<string, string> List { get; set; } = new Dictionary<string, string>() // Custom game mode list
            {  
                {"comp", "Competitive"}, 
                {"1v1", "1 vs 1"},
                {"aim","Aim"},
                {"awp", "AWP Only"},
                {"scoutzknivez", "ScoutzKnives"},
                {"wingman", "Wingman"},
                {"gungame", "Gun Game"},
                {"surf", "Surf"},
                {"dm", "Deathmatch"},
                {"dm-multicfg", "Deathmatch Multicfg"},
                {"course", "Course"},
                {"hns", "Hide N Seek"},
                {"kz", "Kreedz"},
                {"minigames", "Mini Games"}
            }; // Default Game Mode List
        }
        public class VoteSettings
        {
            public bool Enabled { get; set; } = false; // Enables CS2-CustomVotes compatibility
            public bool Map { get; set; } = false; // Enables vote to change game to a specific map
            public bool GameMode { get; set; } = false; // Enables vote to change game mode
            public bool GameSetting { get; set; } = false; // Enables vote to change game setting
            public string Style { get; set; } = "center"; // Changes vote menu type (i.e. "chat" or "center")

        }

        // Create config from classes
         public override int Version { get; set; } = 3;
         public RTVSettings RTV { get; set; } = new RTVSettings();
         public MapGroupSettings MapGroup { get; set; } = new MapGroupSettings();
         public GameSettings Settings { get; set; } = new GameSettings();
         public GameModeSettings GameMode { get; set; } = new GameModeSettings();
         public VoteSettings Votes { get; set; } = new VoteSettings();
    }
}