// Declare namespace
namespace GameModeManager.Shared.Models
{
    // Define interface
    // A setting with a choice of values, like a round time of 2, 5 or 10 minutes: cfg files that share a "// setting: ..." header
    public interface IValueSetting
    {
        // Define interface properties
        public string Name { get; }
        public string Group { get; }                  // "" = the settings menu's first page
        public IReadOnlyList<string> Values { get; }  // The values' labels, like "5 minutes", in menu order
    }
}
