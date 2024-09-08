// Included libraries
using System.Text;
using Microsoft.Extensions.Logging;

// Copyright (c) 2016 Shravan Rajinikanth
// https://github.com/shravan2x/Gameloop.Vdf/
using Gameloop.Vdf;       
using Gameloop.Vdf.Linq;
// ------------------------------------------

// Declare namespace
namespace GameModeManager
{
    // Define class
    public class MapGroupManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private PluginState _pluginState;
        private Config _config = new Config();
        private ILogger<MapGroupManager> _logger;

        // Define class instance
        public MapGroupManager(PluginState pluginState, ILogger<MapGroupManager> logger)
        {
            _logger = logger;
            _pluginState = pluginState;  
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }
        
        // Define on load behavior (parses map groups and stores them in mapgroup instances)
        public void OnLoad(Plugin plugin)
        {
            // Deserialize gamemodes_server.txt (VDF) to VProperty with GameLoop.Vdf
            VProperty vdfObject = VdfConvert.Deserialize(File.ReadAllText(_config.GameModes.MapGroupFile, Encoding.UTF8));
        
            if (vdfObject == null)
            {
                throw new IOException("VDF is empty or incomplete.");
            }
            else
            {
                // Create an array of only map groups
                var _mapGroups = vdfObject.Value.OfType<VProperty>()
                                        .Where(p => p.Key.Equals("mapgroups", StringComparison.OrdinalIgnoreCase))
                                        .Select(p => p.Value)
                                        .FirstOrDefault();

                // Parse array to populate map group list               
                if (_mapGroups != null)
                {
                    foreach (VProperty _mapGroup in _mapGroups.OfType<VProperty>()) 
                    {  
                        // Set map group name
                        MapGroup _group = new MapGroup(_mapGroup.Key);

                        // Create an array of maps
                        var _maps = _mapGroup.Value.OfType<VProperty>()
                                .Where(p => p.Key.Equals("maps", StringComparison.OrdinalIgnoreCase))
                                .Select(p => p.Value)
                                .FirstOrDefault();

                        // Parse array to add maps to map group
                        if (_maps != null)
                        {
                            foreach (VProperty _map in _maps)
                            {
                                string _mapName = _map.Key;

                                // Set display name
                                string _mapDisplayName = _map.Value.ToString();

                                if (_mapName.StartsWith("workshop/"))
                                {
                                    string[] parts = _mapName.Split('/');
                                    long _mapWorkshopId = long.Parse(parts[1]); 
                                    string _mapNameFormatted = parts[parts.Length - 1];

                                    if (!string.IsNullOrEmpty(_mapDisplayName))
                                    {
                                        _group.Maps.Add(new Map(_mapNameFormatted, _mapWorkshopId, _mapDisplayName));
                                        _pluginState.Maps.Add(new Map(_mapNameFormatted, _mapWorkshopId));
                                    }
                                    else
                                    {
                                        _group.Maps.Add(new Map(_mapNameFormatted, _mapWorkshopId));
                                        _pluginState.Maps.Add(new Map(_mapNameFormatted, _mapWorkshopId));
                                    }
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(_mapDisplayName))
                                    {
                                        _group.Maps.Add(new Map(_mapName, _mapDisplayName));
                                        _pluginState.Maps.Add(new Map(_mapName));
                                    }
                                    else
                                    {
                                        _group.Maps.Add(new Map(_mapName));
                                        _pluginState.Maps.Add(new Map(_mapName));
                                    }
                                }
                            }
                        }  
                        else
                        {
                            _logger.LogWarning("Mapgroup found, but the 'maps' property is missing or incomplete. Setting default maps.");
                            _group.Maps = PluginState.DefaultMaps;
                        }
                        // Add map group to map group list
                        _pluginState.MapGroups.Add(_group);
                    }
                }
            }

            // Set default map
            PluginState.DefaultMap = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(_config.Maps.Default, StringComparison.OrdinalIgnoreCase)) ?? PluginState.DefaultMap;
            _pluginState.CurrentMap = PluginState.DefaultMap;
        }

        // Define method to create map group lsit
        public List<MapGroup> CreateMapGroupList(List<string>mapGroups)
        {
            // Create map group list
            List<MapGroup> _mapGroups = new List<MapGroup>();

            foreach(string mapGroup in mapGroups)
            {
                MapGroup? _mapGroup = _pluginState.MapGroups.FirstOrDefault(m => m.Name.Equals(mapGroup, StringComparison.OrdinalIgnoreCase));

                if (_mapGroup != null)
                {
                    // Add mapgroup to list
                    _mapGroups.Add(_mapGroup);
                }
                else
                {
                    // Log warning
                    _logger.LogWarning($"Unable to find {mapGroup}. Make sure it is spelled correctly and listed in your gamemodes_server.txt file.");
                }

            }
            if(_mapGroups != null)
            {
                // Return list
                return _mapGroups;
            }
            else
            {
                // Log warning
                _logger.LogWarning($"Unable to create map group list. Using default list.");

                // Return default map group
                return PluginState.DefaultMapGroups;
            }
        }
    }
}