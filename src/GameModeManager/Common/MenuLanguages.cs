// Included libraries
using System.Globalization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Core.Translations;

// Declare namespace
namespace GameModeManager.Common
{
    // Define class
    // The languages GameModeManager has a lang file for: one per file in lang/, the same codes as !lang. Menus are built once
    // in each of them, so each player sees their own language (!lang, or !game → Language).
    public class MenuLanguages : IPluginDependency<Plugin, Config>
    {
        // Define class properties
        public List<CultureInfo> Languages { get; private set; } = new();

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            string _folder = Path.Combine(plugin.ModuleDirectory, "lang");
            if (!Directory.Exists(_folder))
                return;

            Languages = Directory.EnumerateFiles(_folder, "*.json")
                .Select(file => FindCulture(Path.GetFileNameWithoutExtension(file)))
                .OfType<CultureInfo>()
                .OrderBy(culture => culture.NativeName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static CultureInfo? FindCulture(string name)
        {
            // !lang only accepts names .NET knows, so a file named otherwise can't be picked
            return CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        // Define class methods
        // Runs build once in each language, with the localizer in that language. Without lang files, once in the server's.
        public void ForEach(Action<CultureInfo> build)
        {
            List<CultureInfo> _cultures = Languages.Count > 0 ? Languages : new() { CultureInfo.CurrentUICulture };
            foreach (CultureInfo _culture in _cultures)
            {
                using (new WithTemporaryCulture(_culture))
                    build(_culture);
            }
        }

        // For a menu built when it opens: the localizer in the player's language while it's built
        public static WithTemporaryCulture For(CCSPlayerController player)
        {
            return new WithTemporaryCulture(player.GetLanguage());
        }
    }

    // Define class
    // One menu in each language. Opens the player's language, else the server's, else English.
    public class LocalizedMenu
    {
        // Define class properties
        private readonly Dictionary<string, IMenu> _menus = new(StringComparer.OrdinalIgnoreCase);

        // Define class methods
        public void Set(CultureInfo culture, IMenu? menu)
        {
            if (menu != null)
                _menus[culture.Name] = menu;
        }

        public IMenu? For(CCSPlayerController player)
        {
            return Find(player.GetLanguage())
                ?? Find(CultureInfo.DefaultThreadCurrentUICulture)
                ?? Find(CultureInfo.GetCultureInfo("en"))
                ?? _menus.Values.FirstOrDefault();
        }

        // "pt-BR" falls back to "pt", like the localizer
        private IMenu? Find(CultureInfo? culture)
        {
            for (CultureInfo? _culture = culture; _culture != null && _culture.Name != ""; _culture = _culture.Parent)
            {
                if (_menus.TryGetValue(_culture.Name, out IMenu? _menu))
                    return _menu;
            }
            return null;
        }

        public void Open(CCSPlayerController player)
        {
            For(player)?.Open(player);
        }
    }
}
