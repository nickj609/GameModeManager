// Included libraries
using Microsoft.Extensions.Localization;

// Declare namespace
namespace GameModeManager
{
    // Define class
    public class StringLocalizer
    {
        // Define dependencies
        private readonly string _prefix;
        private IStringLocalizer _localizer;

        // Define class instances
        public StringLocalizer(IStringLocalizer localizer)
        {
            _localizer = localizer;
            _prefix = "plugin.prefix";
        }

        public StringLocalizer(IStringLocalizer localizer, string prefix)
        {
            _localizer = localizer;
            _prefix = prefix;
        }

        // Define methods for localization
        public string LocalizeWithPrefixInternal(string prefix, string key, params object[] args)
        {
            return $"{_localizer[prefix]} {Localize(key, args)}";
        }

        public string LocalizeWithPrefix(string key, params object[] args)
        {
            return LocalizeWithPrefixInternal(_prefix, key, args);
        }

        public string Localize(string key, params object[] args)
        {
            return _localizer[key, args];
        }
    }
}