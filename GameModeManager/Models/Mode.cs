// Included libraries
using GameModeManager.Shared.Models;
// Declare namespace
namespace GameModeManager.Models
{
    // Define class
    public class Mode : IMode
    {
        // Define class properties
        public string Name { get; set; }
        public string Config { get; set; }
        public List<IMap> Maps { get; set; }
        public IMap? DefaultMap { get; set; }
        public List<IMapGroup> MapGroups { get; set; }

        // Define class instances
        public Mode(string name, string configFile, List<IMapGroup> mapGroups) 
        {
            Name = name;
            Config = configFile;
            MapGroups = mapGroups;  
            Maps = CreateMapList(MapGroups);
        }
        public Mode(string name, string configFile, string defaultMap, List<IMapGroup> mapGroups) 
        {
            Name = name;
            Config = configFile;
            MapGroups = mapGroups;  
            Maps = CreateMapList(MapGroups);
            DefaultMap = Maps.FirstOrDefault(m => m.Name.Equals(defaultMap, StringComparison.OrdinalIgnoreCase) || m.DisplayName.Equals(defaultMap, StringComparison.OrdinalIgnoreCase) || m.WorkshopId.ToString().Equals(defaultMap, StringComparison.OrdinalIgnoreCase));
        }

        // Define class methods
        public List<IMap> CreateMapList(List<IMapGroup> mapGroups)
        {
            List<IMap> _maps = new List<IMap>();
            List<string> uniqueMapNames = new List<string>();

            foreach (IMapGroup mapGroup in mapGroups)
            {
                foreach (IMap map in mapGroup.Maps)
                {
                    if (!uniqueMapNames.Any(m => m.Equals(map.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        // Only add the map if its name hasn't been encountered before (case-insensitive)
                        _maps.Add(map);
                        uniqueMapNames.Add(map.Name);
                    }
                }
            }
            return _maps;
        }

        public bool Equals(IMode? other) 
        {
            if (other == null) return false;
            return Name == other.Name && Config == other.Config && MapGroups.SequenceEqual(other.MapGroups) && DefaultMap == other.DefaultMap;
        }
        
        public void Clear()
        {
            Name = "";
            Config = "";
            DefaultMap = null;
            MapGroups = new List<IMapGroup>();
        }
    }
}