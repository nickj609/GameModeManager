// Declare namespace
namespace GameModeManager
{
    // Define class
    public class Mode : IEquatable<Mode>
    {
        // Define parameters
        public string Name { get; set; }
        public string Config { get; set; }
        public Map DefaultMap { get; set; }
        public List<Map> Maps { get; set; }
        public List<MapGroup> MapGroups { get; set; }

        // Define class instances
        public Mode(string name, string configFile, string defaultMap,  List<MapGroup> mapGroups) 
        {
            Name = name;
            Config = configFile;
            MapGroups = mapGroups;  
            Maps = CreateMapList(MapGroups);
            DefaultMap = Maps.FirstOrDefault(m => m.Name == defaultMap || m.WorkshopId.ToString() == defaultMap) ?? PluginState.DefaultMap;
        }

        // Define method to generate maps from map groups
        public List<Map> CreateMapList(List<MapGroup> mapGroups)
        {
            List<Map> _maps = new List<Map>();
            foreach (MapGroup mapGroup in mapGroups)
            {
                foreach(Map map in mapGroup.Maps)
                {
                    Map? _map = _maps.FirstOrDefault(m => m.Name == map.Name);
                    if(_map == null)
                    {
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
            return Name == other.Name && Config == other.Config && MapGroups.SequenceEqual(other.MapGroups);
        }

        // Define method to clear values
        public void Clear()
        {
            Name = "";
            Config = "";
            MapGroups = new List<MapGroup>();
        }
    }
}