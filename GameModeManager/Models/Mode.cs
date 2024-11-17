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
            DefaultMap = Maps.FirstOrDefault(m => m.Name.Equals(defaultMap, StringComparison.OrdinalIgnoreCase) || m.DisplayName.Equals(defaultMap, StringComparison.OrdinalIgnoreCase) || m.WorkshopId.ToString().Equals(defaultMap, StringComparison.OrdinalIgnoreCase));
        }

        // Define method to generate maps from map groups
        public List<Map> CreateMapList(List<MapGroup> mapGroups)
        {
            List<Map> _maps = new List<Map>();
            List<string> uniqueMapNames = new List<string>();

            foreach (MapGroup mapGroup in mapGroups)
            {
                foreach (Map map in mapGroup.Maps)
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