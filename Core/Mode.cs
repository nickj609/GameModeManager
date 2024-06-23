// Declare namespace
namespace GameModeManager
{
    // Define class
    public class Mode : IEquatable<Mode>
    {
        // Define parameters
        public string Name { get; set; }
        public string Config { get; set; }
        public List<Map> Maps { get; set; }
        public List<MapGroup> MapGroups { get; set; }

        // Define class instances
        public Mode(string _name, string _configFile, List<MapGroup> _mapGroups) 
        {
            Name = _name;
            Config = _configFile;
            MapGroups = _mapGroups; 
            Maps = CreateMapList(MapGroups);

        }

        // Define method to generate maps from map groups
        public List<Map> CreateMapList(List<MapGroup> _mapgroups)
        {
            List<Map> _maps = new List<Map>();
            foreach (MapGroup _mapgroup in _mapgroups)
            {
                foreach(Map _map in _mapgroup.Maps)
                {
                    Map? _tmpMap = _maps.FirstOrDefault(m => m.Name == _map.Name);
                    if(_tmpMap == null)
                    {
                        _maps.Add(_map);
                    }
                }
            }
            return _maps;
        }

        // Define method to equate values
        public bool Equals(Mode? _other) 
        {
            if (_other == null) return false;
            return Name == _other.Name && Config == _other.Config && MapGroups.SequenceEqual(_other.MapGroups);
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