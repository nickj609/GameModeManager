// Included libraries
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin, IPluginConfig<Config>
    {   
        // Define configuration object
        public required Config Config { get; set; }

        // Parse configuration object data and perform error checking
        public void OnConfigParsed(Config _config)
        {  
            // RTV Settings
            if (_config.RTV.Enabled != true && _config.RTV.Enabled != false) 
            {
                throw new Exception($"Invalid: RTV 'Enabled' should be 'true' or 'false'.");
            }
            else if(_config.RTV.Enabled == true) 
            {
                if (!File.Exists(_config.RTV.Plugin)) 
                {
                    throw new Exception($"Cannot find RTV 'Plugin': {_config.RTV.Plugin}");
                }
                if (!File.Exists(_config.RTV.MapListFile))  
                {
                    throw new Exception($"Cannot find RTV 'MapListFile': {_config.RTV.MapListFile}");
                }
                if (_config.RTV.DefaultMapFormat != true && _config.RTV.DefaultMapFormat != false)
                {
                    throw new Exception($"Invalid: RTV 'DefaultMapFormat' should be 'true' or 'false'.");
                }
            }
            
            // Map Group Settings
            if (!float.TryParse(_config.MapGroup.Delay.ToString(), out _))  
            {
                throw new Exception("Map group delay must be a number.");
            }
            if (_config.MapGroup.Default == null) 
            {
                throw new Exception($"Undefined: Default map group can not be empty.");
            }

            if (!File.Exists(_config.MapGroup.File))  
            {
                throw new Exception($"Cannot find map group file: {_config.MapGroup.File}");
            }
            
            // Game Mode Settings
             if (_config.GameMode.Rotation != true && _config.GameMode.Rotation != false) 
            {
                throw new Exception($"Invalid: Game mode rotation should be 'true' or 'false'.");
            }
            if (!float.TryParse(_config.GameMode.Delay.ToString(), out _)) 
            {
                throw new Exception("Game mode delay must be a number.");
            }
            if (!int.TryParse(_config.GameMode.Interval.ToString(), out _)) 
            {
                throw new Exception("Game mode interval must be a number.");
            }
            if (_config.GameMode.ListEnabled != true && _config.GameMode.ListEnabled != false) 
            {
                throw new Exception("Invalid: Game mode list enabled should be 'true' or 'false'.");
            }
            else if (_config.GameMode.ListEnabled == true)
            {
                if(_config.GameMode.List == null || _config.GameMode.List.Count == 0)
                {
                    throw new Exception("Undefined: Game mode list cannot be empty.");
                }
            }
     
            if (_config.Version < 2)
            {
                throw new Exception("Your config file is too old, please delete it from addons/counterstrikesharp/configs/plugins/GameModeManager and let the plugin recreate it on load.");
            }

            // Set config
            Config = _config;
        }
    }
    public class Config : BasePluginConfig
    {
        public class RTVSettings
        {
            [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = false; // Enable RTV Compatibility
            [JsonPropertyName("Plugin")] public string Plugin { get; set; } = "/home/steam/cs2/game/csgo/addons/counterstrikesharp/plugins/RockTheVote/RockTheVote.dll"; // RTV plugin path
            [JsonPropertyName("MapListFile")] public string MapListFile { get; set; } = "/home/steam/cs2/game/csgo/addons/counterstrikesharp/plugins/RockTheVote/maplist.txt"; // Default map list file
            [JsonPropertyName("DefaultMapFormat")] public bool DefaultMapFormat { get; set; } = false; // Default file format (ws:<workshop id>). When set to false, uses format <map name>:<workshop id>. 
        
        }

        public class GameSettings
        {
            [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true; // Enable game settings
            [JsonPropertyName("Home")] public string Home { get; set; } = "/home/steam/cs2/game/csgo/cfg"; // Enable game settings
            [JsonPropertyName("Folder")] public string Folder { get; set; } = "settings"; // Default settings folder path
        }

        public class MapGroupSettings
        {
            [JsonPropertyName("Delay")] public float Delay { get; set; } = 5.0f; // Map change delay in seconds
            [JsonPropertyName("Default")] public string Default { get; set; } = "mg_active"; // Default map group on server start
            [JsonPropertyName("File")] public string File { get; set; } = "/home/steam/cs2/game/csgo/gamemodes_server.txt"; // Default game modes and map groups file
        }
        
        public class GameModeSettings
        {
            [JsonPropertyName("Rotation")] public bool Rotation { get; set; } = true; // Enables game mode rotation
            [JsonPropertyName("Interval")] public int Interval { get; set; } = 4; // Changes game mode every x map rotations
            [JsonPropertyName("Delay")] public float Delay { get; set; } = 5.0f; // Game mode change delay in seconds
            [JsonPropertyName("ListEnabled")] public bool ListEnabled { get; set; } = true; // Enables custom game mode list. If set to false, generated from map groups.
            [JsonPropertyName("List")] public List<string> List { get; set; } = new List<string> // Custom game mode list
            {  
                "comp", 
                "1v1",
                "aim",
                "awp",
                "scoutzknivez",
                "wingman",
                "gungame",
                "surf",
                "dm",
                "dm-multicfg",
                "course",
                "hns",
                "kz",
                "minigames"
            }; // Default Game Mode List
        }

        // Create config
         public override int Version { get; set; } = 2;
         [JsonPropertyName("RTV")] public RTVSettings RTV { get; set; } = new RTVSettings();
         [JsonPropertyName("MapGroup")] public MapGroupSettings MapGroup { get; set; } = new MapGroupSettings();
         [JsonPropertyName("GameSettings")] public GameSettings Settings { get; set; } = new GameSettings();
         [JsonPropertyName("GameMode")] public GameModeSettings GameMode { get; set; } = new GameModeSettings();
    }
}