// Declare namespace
namespace GameModeManager.Shared.Models
{
    // Define interface
    public interface IMapGroup
    {
        // Define interface properties
        public string Name { get; }
        public List<IMap> Maps { get; }

        // Define interface methods
        public void Clear();
        public bool Equals(IMapGroup? other);
    }
}