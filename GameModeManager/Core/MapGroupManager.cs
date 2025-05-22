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
        
        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            VProperty vdfObject = VdfConvert.Deserialize(File.ReadAllText(_config.GameModes.MapGroupFile, Encoding.UTF8));
        
            if (vdfObject == null)
            {
                throw new IOException("VDF is empty or incomplete.");
            }
            else
            {
                var _mapGroups = vdfObject.Value.OfType<VProperty>()
                                        .Where(p => p.Key.Equals("mapgroups", StringComparison.OrdinalIgnoreCase))
                                        .Select(p => p.Value)
                                        .FirstOrDefault();
              
                if (_mapGroups != null)
                {
                    foreach (VProperty _mapGroup in _mapGroups.OfType<VProperty>()) 
                    {  
                        IMapGroup _group = new MapGroup(_mapGroup.Key);
                        var _maps = _mapGroup.Value.OfType<VProperty>()
                                .Where(p => p.Key.Equals("maps", StringComparison.OrdinalIgnoreCase))
                                .Select(p => p.Value)
                                .FirstOrDefault();

                        if (_maps != null)
                        {
                            foreach (VProperty _map in _maps)
                            {
                                string _mapName = _map.Key;
                                string _mapDisplayName = _map.Value.ToString();

                                // Check if map is a workshop map
                                if (_mapName.StartsWith("workshop/"))
                                {
                                    string[] parts = _mapName.Split('/');
                                    long _mapWorkshopId = long.Parse(parts[1]);
                                    string _mapNameFormatted = parts[parts.Length - 1];

                                    if (!string.IsNullOrEmpty(_mapDisplayName))
                                    {
                                        _group.Maps.Add(new Map(_mapNameFormatted, _mapWorkshopId, _mapDisplayName));
                                        
                                        // Add to all maps list only if it doesn't already exist
                                        if (!_pluginState.Game.Maps.Any(m => m.Name == _mapNameFormatted && m.WorkshopId == _mapWorkshopId))
                                        {
                                            _pluginState.Game.Maps.Add(new Map(_mapNameFormatted, _mapWorkshopId, _mapDisplayName));
                                        }
                                    }
                                    else
                                    {
                                        _group.Maps.Add(new Map(_mapNameFormatted, _mapWorkshopId));

                                        // Add to all maps list only if it doesn't already exist
                                        if (!_pluginState.Game.Maps.Any(m => m.Name == _mapNameFormatted && m.WorkshopId == _mapWorkshopId))
                                        {
                                            _pluginState.Game.Maps.Add(new Map(_mapNameFormatted, _mapWorkshopId));
                                        }
                                    }
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(_mapDisplayName))
                                    {
                                        _group.Maps.Add(new Map(_mapName, _mapDisplayName));

                                        // Add to all maps list only if it doesn't already exist
                                        if (!_pluginState.Game.Maps.Any(m => m.Name == _mapName))
                                        {
                                            _pluginState.Game.Maps.Add(new Map(_mapName, _mapDisplayName));
                                        }
                                    }
                                    else
                                    {
                                        _group.Maps.Add(new Map(_mapName));

                                        // Add to all maps list only if it doesn't already exist
                                        if (!_pluginState.Game.Maps.Any(m => m.Name == _mapName))
                                        {
                                            _pluginState.Game.Maps.Add(new Map(_mapName));
                                        }
                                    }
                                }
                            }
                            // Add map group to map group list
                            _pluginState.Game.MapGroups.Add(_group);
                        }
                        else
                        {
                            _logger.LogError($"Mapgroup {_mapGroup.Key} found, but the 'maps' property is missing or incomplete.");
                        }
                    }
                }
            }

            // Set current map
            IMap? defaultMap = _pluginState.Game.Maps.FirstOrDefault(m => m.Name.Equals(_config.Maps.Default, StringComparison.OrdinalIgnoreCase));

            if (defaultMap != null)
            {
                _pluginState.Game.CurrentMap = defaultMap;
            }
            else
            {
                _pluginState.Game.CurrentMap = PluginState.GameController.DefaultMap;
            }
        }

        // Define on map start behavior
       public void OnMapStart(string map)
        {
            IMap _map = _pluginState.Game.Maps.FirstOrDefault(m => m.Name.Equals(map, StringComparison.OrdinalIgnoreCase) || m.DisplayName.Equals(map, StringComparison.OrdinalIgnoreCase)) ?? new Map(map);
            _pluginState.Game.CurrentMap = _map;
        }
    }
}