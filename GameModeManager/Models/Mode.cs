// Declare namespace
namespace GameModeManager.Models
{
    // Define class
    public class Mode : IEquatable<Mode>
    {
        // Define parameters
        public string Name { get; set; }
        public string Config { get; set; }
        public List<Map> Maps { get; set; }
        public Map? DefaultMap { get; set; }
        public List<MapGroup> MapGroups { get; set; }

        // Define class instances
        public Mode(string name, string configFile, List<MapGroup> mapGroups) 
        {
            Name = name;
            Config = configFile;
            MapGroups = mapGroups;  
            Maps = CreateMapList(MapGroups);
        }
        public Mode(string name, string configFile, string defaultMap, List<MapGroup> mapGroups) 
        {
            Name = name;
            Config = configFile;
            MapGroups = mapGroups;  
            Maps = CreateMapList(MapGroups);
            DefaultMap = Maps.FirstOrDefault(m => m.Name.Equals(defaultMap, StringComparison.OrdinalIgnoreCase) || m.WorkshopId.ToString().Equals(defaultMap, StringComparison.OrdinalIgnoreCase));
        }

        // Define method to generate maps from map groups
        public List<Map> CreateMapList(List<MapGroup> mapGroups)
        {
            List<Map> _maps = new List<Map>();
            HashSet<string> uniqueMapNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (MapGroup mapGroup in mapGroups)
            {
                foreach (Map map in mapGroup.Maps)
                {
                    if (uniqueMapNames.Add(map.Name))
                    {
                        // Only add the map if its name hasn't been encountered before (case-insensitive)
                        _maps.Add(map);
                    }
                }
            }
            return _maps;
        }

        // Define method to equate values
        public bool Equals(Mode? other) 
        {
            if (other == null) return false;
            return Name == other.Name && Config == other.Config && MapGroups.SequenceEqual(other.MapGroups) && DefaultMap == other.DefaultMap;
        }

        // Define method to clear values
        public void Clear()
        {
            Name = "";
            Config = "";
            DefaultMap = null;
            MapGroups = new List<MapGroup>();
        }
    }
}