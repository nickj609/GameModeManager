// Included libraries
using System.Text.Json;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core.Attributes;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin, IPluginConfig<Config>
    {   
        // Define configuration object
        public required Config Config { get; set; }

        // Parse configuration object data and perform error checking
        public void OnConfigParsed(Config config)
        {  
            // RTV Settings
            if (config.RTV.Enabled != true && config.RTV.Enabled != false) 
            {
                throw new Exception($"Invalid: RTV 'Enabled' should be 'true' or 'false'.");
            }
            else if(config.RTV.Enabled == true) 
            {
                if (!File.Exists(config.RTV.Plugin)) 
                {
                    throw new Exception($"Cannot find RTV 'Plugin': {config.RTV.Plugin}");
                }
                if (!File.Exists(config.RTV.MapListFile))  
                {
                    throw new Exception($"Cannot find RTV 'MapListFile': {config.RTV.MapListFile}");
                }
                if (config.RTV.DefaultMapFormat != true && config.RTV.DefaultMapFormat != false)
                {
                    throw new Exception($"Invalid: RTV 'DefaultMapFormat' should be 'true' or 'false'.");
                }
            }
            
            // Map Group Settings
            if (config.MapGroup.Default == null) 
            {
                throw new Exception($"Undefined: MapGroup 'Default' can not be empty.");
            }

            if (!File.Exists(config.MapGroup.File))  
            {
                throw new Exception($"Cannot find MapGroup 'File': {config.MapGroup.File}");
            }
            
            // Game Mode Settings
            if (config.GameMode.ListEnabled != true && config.GameMode.ListEnabled != false) 
            {
                throw new Exception($"Invalid: GameMode 'ListEnabled' should be 'true' or 'false'.");
            }
            else if (config.GameMode.ListEnabled == true)
            {
                
                if(config.GameMode.List == null || config.GameMode.List.Count == 0)
                {
                    throw new Exception($"Undefined: GameMode 'List' cannot be empty.");
                }
            }
            Config = config;
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

        public class MapGroupSettings
        {
            [JsonPropertyName("Default")] public string Default { get; set; } = "mg_active"; // Default map group on server start
            [JsonPropertyName("File")] public string File { get; set; } = "/home/steam/cs2/game/csgo/gamemodes_server.txt"; // Default game modes and map groups file
        }
        
        public class GameModeSettings
        {
            [JsonPropertyName("ListEnabled")] public bool ListEnabled { get; set; } = true; // Enables custom game mode list. Default list is generated from map groups.
            [JsonPropertyName("List")] public List<string> List { get; set; } = new List<string>
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

         [JsonPropertyName("RTV")] public RTVSettings RTV { get; set; } = new RTVSettings();
         [JsonPropertyName("MapGroup")] public MapGroupSettings MapGroup { get; set; } = new MapGroupSettings();
         [JsonPropertyName("GameMode")] public GameModeSettings GameMode { get; set; } = new GameModeSettings();
    }
}