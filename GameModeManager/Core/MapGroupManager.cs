// Included libraries
using System.Text;
using Gameloop.Vdf;      
using Gameloop.Vdf.Linq;
using GameModeManager.Models;
using GameModeManager.Contracts;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class MapGroupManager : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private Config _config = new Config();
        private ILogger<MapGroupManager> _logger;

        // Define class constructor
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
        
        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            // Read file content
            string fileContent = string.Empty;
            try
            {
                fileContent = File.ReadAllText(_config.GameModes.MapGroupFile, Encoding.UTF8);
            }
            catch (FileNotFoundException)
            {
                _logger.LogError($"Map group file not found: {_config.GameModes.MapGroupFile}");
                return; 
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading map group file: {ex.Message}");
                return; 
            }

            VProperty vdfObject = VdfConvert.Deserialize(fileContent);
        
            if (vdfObject == null || vdfObject.Value == null) 
            {
                throw new IOException("VDF is empty or incomplete.");
            }
            else
            {
                var _mapGroupsNode = vdfObject.Value.OfType<VProperty>()
                                                 .Where(p => p.Key.Equals("mapgroups", StringComparison.OrdinalIgnoreCase))
                                                 .Select(p => p.Value)
                                                 .FirstOrDefault();
             
                if (_mapGroupsNode != null)
                {
                    foreach (VProperty _mapGroupProperty in _mapGroupsNode.OfType<VProperty>()) 
                    {   
                        IMapGroup _group = new MapGroup(_mapGroupProperty.Key);
                        var _mapsNode = _mapGroupProperty.Value.OfType<VProperty>()
                                                             .Where(p => p.Key.Equals("maps", StringComparison.OrdinalIgnoreCase))
                                                             .Select(p => p.Value)
                                                             .FirstOrDefault();

                        if (_mapsNode != null)
                        {
                            foreach (VProperty _mapProperty in _mapsNode)
                            {
                                string _mapName = _mapProperty.Key;
                                string _mapDisplayName = _mapProperty.Value.ToString();
                                IMap? currentMap = null; // Declare a variable to hold the created map object

                                // Check if map is a workshop map
                                if (_mapName.StartsWith("workshop/"))
                                {
                                    string[] parts = _mapName.Split('/');
                                    if (parts.Length >= 2 && long.TryParse(parts[1], out long _mapWorkshopId))
                                    {
                                        string _mapNameFormatted = parts[parts.Length - 1];

                                        if (!string.IsNullOrEmpty(_mapDisplayName))
                                        {
                                            currentMap = new Map(_mapNameFormatted, _mapWorkshopId, _mapDisplayName);
                                        }
                                        else
                                        {
                                            currentMap = new Map(_mapNameFormatted, _mapWorkshopId);
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning($"Invalid workshop map format or ID: {_mapName}");
                                    }
                                }
                                else // Regular map
                                {
                                    if (!string.IsNullOrEmpty(_mapDisplayName))
                                    {
                                        currentMap = new Map(_mapName, _mapDisplayName);
                                    }
                                    else
                                    {
                                        currentMap = new Map(_mapName);
                                    }
                                }

                                // If a map object was successfully created
                                if (currentMap != null)
                                {
                                    _group.Maps.Add(currentMap); // Add to map group's list

                                    _pluginState.Game.Maps.TryAdd(currentMap.Name, currentMap);

                                    if (currentMap.WorkshopId > 0)
                                    {
                                        _pluginState.Game.MapsByWorkshopId.TryAdd(currentMap.WorkshopId, currentMap);
                                    }
                                }
                            }
                            // Add map group to map group list
                            _pluginState.Game.MapGroups.TryAdd(_group.Name, _group);
                        }
                        else
                        {
                            _logger.LogError($"Mapgroup '{_mapGroupProperty.Key}' found, but the 'maps' property is missing or incomplete.");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"'mapgroups' property not found in VDF file: {_config.GameModes.MapGroupFile}. No map groups loaded.");
                }
            }

            // Set current map
            if (_pluginState.Game.Maps.TryGetValue(_config.Maps.Default, out IMap? defaultMap))
            {
                _pluginState.Game.CurrentMap = defaultMap;
            }
            else
            {
                _pluginState.Game.CurrentMap = PluginState.GameController.DefaultMap;
                _logger.LogWarning($"Default map '{_config.Maps.Default}' not found in loaded maps. Using game controller's default map.");
            }
        }

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            _pluginState.Game.CurrentMap = _pluginState.Game.Maps.TryGetValue(map, out IMap? foundMap) ? foundMap : new Map(map);
        }
    }
}