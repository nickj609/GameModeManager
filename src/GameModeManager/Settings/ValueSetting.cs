// Included libraries
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Settings
{
    // Define class
    // A setting with a choice of values, like a round time of 2, 5 or 10 minutes: one cfg per value in the settings folder,
    // tied together by the same "// setting: Round time" header, each with its own "// value: 5 minutes". Shown as one row with
    // a dropdown of its values.
    public class ValueSetting : IValueSetting
    {
        // Define class properties
        public string Name { get; }
        public string Permission { get; set; } = ""; // Empty = @css/cvar
        public bool InVotes { get; set; } = true;
        public string Conflicts { get; set; } = ""; // Any of its files can say it clashes with a plugin
        public string Requires { get; set; } = ""; // Any of its files can say it needs a plugin
        public string Group { get; set; } = ""; // From "// group: ..." in any of its files
        public SettingApplies Applies { get; set; } = SettingApplies.Now; // From "// applies: ..." in any of its files
        public List<SettingValue> Values { get; } = new();
        IReadOnlyList<string> IValueSetting.Values => Values.Select(value => value.Label).ToList();

        // Define class constructor
        public ValueSetting(string name)
        {
            Name = name;
        }
    }

    // Define class
    // One value of a value setting: its cfg file (relative to the settings folder) and the name shown for it
    public class SettingValue
    {
        public string File { get; }
        public string Label { get; }

        public SettingValue(string file, string label)
        {
            File = file;
            Label = label;
        }
    }
}
