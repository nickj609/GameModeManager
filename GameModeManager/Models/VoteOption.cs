// Declare namespace
namespace GameModeManager.Models
{
    // Define class
    public class VoteOption
    {
        // Define class properties
        public string Name { get; set; }
        public long WorkshopId { get; set; }
        public string DisplayName { get; set; }
        public VoteOptionType Type { get; set; }

        // Define class constructors
        public VoteOption(string name, long workshopId, string displayName, VoteOptionType type)
        {
            Type = type;
            Name = name;
            WorkshopId = workshopId;
            DisplayName = displayName;
        }

        public VoteOption(string name, string displayName, VoteOptionType type)
        {
            Name = name;
            Type = type;
            DisplayName = displayName;
        }

        public VoteOption(string name, long workshopId, VoteOptionType type)
        {
            Name = name;
            Type = type;
            DisplayName = name;
            WorkshopId = workshopId;
        }

        public VoteOption(string name, VoteOptionType type)
        {
            Name = name;
            Type = type;
            DisplayName = name;
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Name.GetHashCode(StringComparison.OrdinalIgnoreCase), WorkshopId.GetHashCode(), Type.GetHashCode());
        }
    }
}