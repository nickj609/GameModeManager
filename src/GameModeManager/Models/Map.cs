// Included libraries
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Models
{
    // Define class
    public class Map : IMap
    {
        // Define class properties
        public string Name { get; set; }
        public long WorkshopId { get; set; }
        public string DisplayName { get; set; }

        // Define class constructors
        public Map(string _name)
        {
            Name = _name;
            WorkshopId = -1;
            DisplayName = _name;
        }

        public Map(string _name, long _workshopId)
        {
            Name = _name;
            DisplayName = _name;
            WorkshopId = _workshopId;
        }

        public Map(string _name, string _displayName)
        {
            Name = _name;
            WorkshopId = -1;
            DisplayName = _displayName;
        }

        public Map(string _name, long _workshopId, string _displayName)
        {
            Name = _name;
            WorkshopId = _workshopId;
            DisplayName = _displayName;
        }

        // Define class methods
        public void Clear()
        {
            Name = "";
            WorkshopId = -1;
            DisplayName = "";
        }

        public bool Equals(IMap? other)
        {
            if (other == null) return false;
            return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) && WorkshopId.Equals(other.WorkshopId);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Name.GetHashCode(StringComparison.OrdinalIgnoreCase), WorkshopId.GetHashCode());
        }
    }
}