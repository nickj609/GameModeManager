// Included libraries
using Microsoft.Extensions.Localization;

// Declare namespace
namespace GameModeManager
{
    // Define StringLocalizer class
    public class StringLocalizer
    {
        private IStringLocalizer _localizer;

        private readonly string _prefix;

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