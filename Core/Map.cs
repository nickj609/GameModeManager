// Declare namespace
namespace GameModeManager
{
    // Define Map class
    public class Map : IEquatable<Map>
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string WorkshopId { get; set; }

        public Map(string _name)
        {
            Name = _name;
            DisplayName = _name; 
            WorkshopId = "";
        }
        
        public Map(string _name, string _workshopId)
        {
            Name = _name;
            DisplayName = _name;
            WorkshopId = _workshopId;
        }
        public Map(string _name, string _workshopId, string _displayName)
        {
            Name = _name;
            DisplayName = _displayName;
            WorkshopId = _workshopId;
        }

        public bool Equals(Map? _other) 
        {
            if (_other == null) return false;  // Handle null 

            // Implement your equality logic, e.g.;
            return Name == _other.Name && WorkshopId == _other.WorkshopId && DisplayName == _other.DisplayName;
        }

        public void Clear()
        {
            Name = "";
            WorkshopId = "";
        }
    }
}