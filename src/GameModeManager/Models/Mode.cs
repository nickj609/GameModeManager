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
        public HashSet<IMap> Maps { get; set; }
        public IMap? DefaultMap { get; set; }
        public HashSet<IMapGroup> MapGroups { get; set; }

        // Define class constructors
        public Mode(string name, string configFile, HashSet<IMapGroup> mapGroups)
        {
            Name = name;
            Config = configFile;
            MapGroups = mapGroups;
            Maps = CreateMapList(MapGroups);
        }
        public Mode(string name, string configFile, IMap? defaultMap, HashSet<IMapGroup> mapGroups)
        {
            Name = name;
            Config = configFile;
            MapGroups = mapGroups;
            Maps = CreateMapList(MapGroups);
            DefaultMap = defaultMap;
        }

        // Define class methods
        public void Clear()
        {
            Name = "";
            Config = "";
            DefaultMap = null;
            MapGroups = new HashSet<IMapGroup>();
        }

        public bool Equals(IMode? other) 
        {
            if (other == null) return false;
            return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase)
                && Config.Equals(other.Config, StringComparison.OrdinalIgnoreCase)
                && MapGroups.Equals(other.MapGroups)
                && ((DefaultMap == null && other.DefaultMap == null) || (DefaultMap != null && DefaultMap.Equals(other.DefaultMap)));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name.GetHashCode(StringComparison.OrdinalIgnoreCase), Config.GetHashCode(StringComparison.OrdinalIgnoreCase), DefaultMap?.GetHashCode() ?? 0, MapGroups.GetHashCode());
        }

        public HashSet<IMap> CreateMapList(HashSet<IMapGroup> mapGroups)
        {
            HashSet<IMap> _maps = new HashSet<IMap>();

            foreach (IMapGroup mapGroup in mapGroups)
            {
                foreach (IMap map in mapGroup.Maps)
                    _maps.Add(map);
            }
            return _maps;
        }
    }
}