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
    // Define MapGroupManager class
    public class MapGroupManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private static Config? _config;
        private static Plugin? _plugin;
        private static ILogger? _logger;

        // Load dependencies
        public void OnLoad(Plugin plugin)
        { 
            _plugin = plugin;
            _logger = plugin.Logger;
        }
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }
        
        // Construct reusable function to parse map groups
        public static void Load()
        {
            if (_config != null && _logger != null)
            {
                try
                {
                    // Deserialize gamemodes_server.txt (VDF) to VProperty with GameLoop.Vdf
                    VProperty vdfObject = VdfConvert.Deserialize(File.ReadAllText(_config.MapGroup.File, Encoding.UTF8));
                
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
                                if (_maps != null && PluginState.Maps != null)
                                {
                                    foreach (VProperty _map in _maps)
                                    {
                                        string _mapName = _map.Key;
                                        string _mapDisplayName = _map.Value.ToString();

                                        if (_mapName.StartsWith("workshop/"))
                                        {
                                            string[] parts = _mapName.Split('/');
                                            string _mapNameFormatted = parts[parts.Length - 1];
                                            string _mapWorkshopId = parts[1]; 

                                            if (_mapDisplayName.Count() > 0)
                                            {
                                                _group.Maps.Add(new Map(_mapNameFormatted, _mapWorkshopId, _mapDisplayName));
                                                PluginState.Maps.Add(new Map(_mapNameFormatted, _mapWorkshopId));
                                            }
                                            else
                                            {
                                                _group.Maps.Add(new Map(_mapNameFormatted, _mapWorkshopId));
                                                PluginState.Maps.Add(new Map(_mapNameFormatted, _mapWorkshopId));
                                            }
                                        }
                                        else
                                        {
                                            if (_mapDisplayName.Count() > 0)
                                            {
                                                _group.Maps.Add(new Map(_mapName, "", _mapDisplayName));
                                                PluginState.Maps.Add(new Map(_mapName));
                                            }
                                            else
                                            {
                                                _group.Maps.Add(new Map(_mapName));
                                                PluginState.Maps.Add(new Map(_mapName));
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
                                PluginState.MapGroups.Add(_group);
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"The mapgroup property doesn't exist. Using default map group.");
                            PluginState.MapGroups.Add(PluginState.DefaultMapGroup);
                        }
                    }
                    // Set default map group from configuration file. If not found, use plugin default.
                    PluginState.DefaultMapGroup = PluginState.MapGroups.FirstOrDefault(g => g.Name == $"{_config.MapGroup.Default}") ?? PluginState.DefaultMapGroup;
                    PluginState.CurrentMapGroup = PluginState.DefaultMapGroup;

                    if(PluginState.Maps != null)
                    {
                        PluginState.DefaultMap = PluginState.Maps.FirstOrDefault(m => m.Name == $"{_config.MapGroup.DefaultMap}") ?? PluginState.DefaultMap;
                        PluginState.CurrentMap = PluginState.DefaultMap;
                    }

                    // Update map list
                    MapManager.UpdateMapList(PluginState.DefaultMapGroup);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{ex.Message}");
                }

            }
        }
    }
}