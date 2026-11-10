// Included libraries
using System.Text.RegularExpressions;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    // Optional details at the top of a setting or action cfg, as comments, so the file still runs as a normal cfg:
    //   // name: Swap teams
    //   // permission: @css/generic
    //   // vote: true
    // Only comment lines before the file's first command are read. Keys are case-insensitive; unknown ones are ignored.
    public class CfgHeader
    {
        // Define class properties
        private static readonly Regex Line = new(@"^\s*//\s*([A-Za-z]+)\s*:\s*(.*?)\s*$", RegexOptions.Compiled);
        private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

        public string? Name => Get("name");
        public string? Permission => Get("permission");
        public string? Setting => Get("setting"); // A value setting's name: files that share it are one setting
        public string? Value => Get("value");     // This file's value of that setting, like "5 minutes"
        public bool? Vote => Get("vote") is string vote ? !vote.Equals("false", StringComparison.OrdinalIgnoreCase) && vote != "0" : null;

        // Define class methods
        public static CfgHeader Read(string path)
        {
            CfgHeader header = new();
            if (!File.Exists(path))
                return header;

            foreach (string text in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                if (!text.TrimStart().StartsWith("//"))
                    break; // The first command: the header is over

                Match match = Line.Match(text);
                if (match.Success && match.Groups[2].Value.Length > 0)
                    header._values.TryAdd(match.Groups[1].Value, match.Groups[2].Value);
            }
            return header;
        }

        private string? Get(string key)
        {
            return _values.TryGetValue(key, out string? value) ? value : null;
        }
    }
}
