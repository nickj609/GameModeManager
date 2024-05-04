// Included libraries
using System.Text;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;

// Copyright (c) 2016 Shravan Rajinikanth
// https://github.com/shravan2x/Gameloop.Vdf/
using Gameloop.Vdf;       
using Gameloop.Vdf.Linq;
// ------------------------------------------

// Declare namespace
namespace GameModeManager
{
     // Define map group class
    public class MapGroup : IEquatable<MapGroup>
    {
        public string Name { get; set; }
        public List<Map> Maps { get; set; }

        public MapGroup(string _name) 
        {
            Name = _name;
            Maps = new List<Map>();
        }

        public MapGroup(string _name, List<Map> _maps) 
        {
            Name = _name;
            Maps = _maps; 
        }

        public bool Equals(MapGroup? _other) 
        {
            if (_other == null) 
            {
                return false;  // Handle null 
            }
            else
            {
                return Name == _other.Name && Maps.SequenceEqual(_other.Maps);
            }
        }
        public void Clear()
        {
            Name = "";
            Maps = new List<Map>();
        }
    }

    // Define plugin class
    public partial class Plugin : BasePlugin
    {
        // Define default map group and default maps
        private static List<Map> _defaultMaps = new List<Map>()
        {
            new Map("de_ancient"),
            new Map("de_anubis"),
            new Map("de_inferno"),
            new Map("de_mirage"),
            new Map("de_nuke"),
            new Map("de_overpass"),
            new Map("de_vertigo")
        };
        private static MapGroup _defaultMapGroup = new MapGroup("mg_active", _defaultMaps);
        
        // Define current map group and map group list
        public static MapGroup CurrentMapGroup = _defaultMapGroup;
        public static List<MapGroup> MapGroups = new List<MapGroup>();
        
        // Define function to parse map groups
        private void ParseMapGroups()
        {
            try
            {
                // Deserialize gamemodes_server.txt (VDF) to VProperty with GameLoop.Vdf
                VProperty vdfObject = VdfConvert.Deserialize(File.ReadAllText(Config.MapGroup.File, Encoding.UTF8));
                
                if (vdfObject == null)
                {
                    throw new IOException("VDF is empty or incomplete.");
                }
                else
                {
                    // Create an array of only map groups
                    var _mapGroups = vdfObject.Value.OfType<VProperty>()
                                            .Where(p => p.Key == "mapgroups")
                                            .Select(p => p.Value)
                                            .FirstOrDefault();

                    // Parse array to populate map group list               
                    if (_mapGroups != null)
                    {
                        foreach (VProperty _mapGroup in _mapGroups.OfType<VProperty>()) 
                        {  
                            // Create map group
                            MapGroup _group = new MapGroup(_mapGroup.Key);

                            // Create an array of maps
                            var _maps = _mapGroup.Value.OfType<VProperty>()
                                    .Where(p => p.Key == "maps")
                                    .Select(p => p.Value)
                                    .FirstOrDefault();

                            // Parse array to add maps to map group
                            if (_maps != null)
                            {
                                foreach (VProperty _map in _maps)
                                {
                                    string _mapName = _map.Key;

                                    if (_mapName.StartsWith("workshop/"))
                                    {
                                        string[] parts = _mapName.Split('/');
                                        string _mapNameFormatted = parts[parts.Length - 1];
                                        string _mapWorkshopId = parts[1]; 
                                        _group.Maps.Add(new Map(_mapNameFormatted, _mapWorkshopId));
                                        Maps.Add(new Map(_mapNameFormatted, _mapWorkshopId));
                                    }
                                    else
                                    {
                                        _group.Maps.Add(new Map(_mapName));
                                        Maps.Add(new Map(_mapName));
                                    }
                                }
                            }
                            else
                            {
                                Logger.LogWarning("Mapgroup found, but the 'maps' property is missing or incomplete. Setting default maps.");
                                _group.Maps = _defaultMaps;
                            }
                            // Add map group to map group list
                            MapGroups.Add(_group);
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"The mapgroup property doesn't exist. Using default map group.");
                        MapGroups.Add(_defaultMapGroup);
                    }
                }
                // Set default map group from configuration file. If not found, use plugin default.
                _defaultMapGroup = MapGroups.FirstOrDefault(g => g.Name == $"{Config.MapGroup}") ?? new MapGroup("mg_active", _defaultMaps);
                CurrentMapGroup = _defaultMapGroup;

                // Update map list
                UpdateMapList(_defaultMapGroup);
            }
            catch (Exception ex)
            {
                Logger.LogError($"{ex.Message}");
            }
        }

        // Construct server map group command handler
        [ConsoleCommand("css_mapgroup", "Sets the mapgroup for the MapListUpdater plugin.")]
        [CommandHelper(minArgs: 1, usage: "mg_active", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnMapGroupCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) 
            {
                // Get map group
                MapGroup? _mapGroup = MapGroups.FirstOrDefault(g => g.Name == $"{command.ArgByIndex(1)}");

                if (_mapGroup == null || _mapGroup.Name == null || _mapGroup.Maps == null)
                {
                    Logger.LogWarning("New map group could not be found. Setting default map group.");
                    _mapGroup = _defaultMapGroup;
                }
                Logger.LogInformation($"Current map group is {CurrentMapGroup.Name}.");
                Logger.LogInformation($"New map group is {_mapGroup.Name}.");

                // Update map list and map menu
                try
                {
                    UpdateMapList(_mapGroup);
                }
                catch(Exception ex)
                {
                    Logger.LogError($"{ex.Message}");
                }
            }
        }
    }
}