// Included libraries
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Models
{
    // Define class
    public class MapGroup : IMapGroup
    {
        // Define class properties
        public string Name { get; set; }
        public HashSet<IMap> Maps { get; set; }

        // Define class constructors
        public MapGroup(string _name)
        {
            Name = _name;
            Maps = new HashSet<IMap>();
        }

        public MapGroup(string _name, HashSet<IMap> _maps)
        {
            Name = _name;
            Maps = _maps;
        }

        // Define class methods
        public void Clear()
        {
            Name = "";
            Maps = new HashSet<IMap>();
        }
        public bool Equals(IMapGroup? other)
        {
            if (other == null) return false;
            return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) && Maps.SequenceEqual(other.Maps);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Name.GetHashCode(StringComparison.OrdinalIgnoreCase), Maps.GetHashCode());
        }
    }
}