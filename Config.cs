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
            // Check if plugin exists
            if (!File.Exists(config.RTVPlugin)) 
            {
                throw new Exception($"Cannot find 'RTVPlugin': {config.RTVPlugin}");
            }
            
            // Check if map group exists
            if (config.MapGroup == null) 
            {
                throw new Exception($"Undefined: 'MapGroup': {config.MapGroup}");
            }

            // Check if MapListFile is null
            if (!File.Exists(config.MapListFile))  
            {
                throw new Exception($"Cannot find 'MapListFile': {config.MapListFile}");
            }

            // Check if MapGroupFile is null
            if (!File.Exists(config.MapGroupsFile))  
            {
                throw new Exception($"Cannot find 'MapGroupFile': {config.MapGroupsFile}");
            }

            Config = config; // After successful validation
        }
    }
    public class Config : BasePluginConfig
    {
        [JsonPropertyName("MapGroup")] public string MapGroup { get; set; } = "mg_casual"; // Default map group on server start
        [JsonPropertyName("MapGroupsFile")] public string MapGroupsFile { get; set; } = "/home/steam/cs2/game/csgo/gamemodes_server.txt"; // Default game modes and map groups file
        [JsonPropertyName("MapListFile")] public string MapListFile { get; set; } = "/home/steam/cs2/game/csgo/addons/counterstrikesharp/plugins/RockTheVote/maplist.txt"; // Default map list file
        [JsonPropertyName("DefaultMapFormat")] public bool DefaultMapFormat { get; set; } = false; // Default file format (ws:<workshop id>). When set to false, uses format <map name>:<workshop id>. 
        [JsonPropertyName("RTVEnabled")] public bool RTVEnabled { get; set; } = true; // Enable RTV Compatibility
        [JsonPropertyName("RTVPlugin")] public string RTVPlugin { get; set; } = "/home/steam/cs2/game/csgo/addons/counterstrikesharp/plugins/RockTheVote/RockTheVote.dll"; // RTV plugin path
        [JsonPropertyName("ListEnabled")] public bool ListEnabled { get; set; } = true; // Enables custom game mode list. Default list is generated from map groups.
        [JsonPropertyName("GameModeList")] public List<string> GameModeList { get; set; } = new List<string>
        { 
            "casual", 
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
            "hz",
            "minigames"
        }; // Default Game Mode List
    }
}