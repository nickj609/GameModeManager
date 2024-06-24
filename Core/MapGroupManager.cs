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
        private ILogger _logger;
        private PluginState _pluginState;
        private Config _config = new Config();

        // Define class instance
        public MapGroupManager(PluginState pluginState, ILogger logger)
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
            VProperty vdfObject = VdfConvert.Deserialize(File.ReadAllText(_config.MapGroups.File, Encoding.UTF8));
        
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
                        // Set map group name
                        MapGroup _group = new MapGroup(_mapGroup.Key);

                        // Set display name
                        var _displayName = _mapGroup.Value.OfType<VProperty>()
                                .Where(p => p.Key == "displayname")
                                .Select(p => p.Value)
                                .FirstOrDefault();

                        if (_displayName != null)
                        {
                            _group.DisplayName = _displayName.ToString();
                        }

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
            // Set default map group from configuration file. If not found, use plugin default.
            PluginState.DefaultMapGroup = _pluginState.MapGroups.FirstOrDefault(g => g.Name == _config.MapGroups.Default) ?? PluginState.DefaultMapGroup;
            _pluginState.CurrentMapGroup = PluginState.DefaultMapGroup;

            PluginState.DefaultMap = _pluginState.Maps.FirstOrDefault(m => m.Name == _config.MapGroups.DefaultMap) ?? PluginState.DefaultMap;
            _pluginState.CurrentMap = PluginState.DefaultMap;
        }
    }
}