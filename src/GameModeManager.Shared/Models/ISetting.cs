// Declare namespace
namespace GameModeManager.Shared.Models
{
    // Define interface
    public interface ISetting
    {
        // Define interface properties
        public string Name { get; }
        public string Enable { get; set; }
        public string Disable { get; set; }
        public string DisplayName { get; }

        // Define interface methods
        public void Clear();
        public int GetHashCode();
        public bool Equals(object? obj);
    }
}