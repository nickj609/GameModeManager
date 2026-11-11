// Included libraries
using System.Reflection;
using CounterStrikeSharp.API.Core;

// Declare namespace
namespace GameModeManager.Common
{
    // Define class
    // The plugins CounterStrikeSharp has loaded now. It keeps that list to itself (Application._pluginManager), so it's read
    // through reflection; Read() is null when that changes. Plugins can be loaded and unloaded at any time (Kus's mode cfgs
    // load MatchZy with css_plugins load), so callers ask when they need to know.
    public static class LoadedPlugins
    {
        // Define class properties
        // A loaded plugin: its name, its file's name, and the plugin itself
        public record Info(string ModuleName, string FileName, IPlugin? Plugin = null)
        {
            public bool Matches(string name) => name.Equals(ModuleName, StringComparison.OrdinalIgnoreCase) || name.Equals(FileName, StringComparison.OrdinalIgnoreCase);
        }

        // MatchZy runs its own pauses, timeouts, ready-up and warmup. GameModeManager leaves those to it while it's loaded.
        public const string MatchZy = "MatchZy";

        private static List<Info>? _cached;
        private static DateTime _cachedUntil;

        // Define class methods
        public static List<Info>? Read()
        {
            const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            object? manager = typeof(Application).GetField("_pluginManager", Any)?.GetValue(Application.Instance);
            if (manager?.GetType().GetMethod("GetLoadedPlugins")?.Invoke(manager, null) is not System.Collections.IEnumerable contexts)
                return null;

            List<Info> loaded = new();
            foreach (object context in contexts)
            {
                Type type = context.GetType();
                if (type.GetProperty("State")?.GetValue(context)?.ToString() != "Loaded")
                    continue;

                IPlugin? plugin = type.GetProperty("Plugin")?.GetValue(context) as IPlugin;
                string filePath = type.GetProperty("FilePath")?.GetValue(context) as string ?? "";
                loaded.Add(new Info(plugin?.ModuleName ?? "", Path.GetFileNameWithoutExtension(filePath), plugin));
            }
            return loaded;
        }

        // Whether that plugin is loaded. Read at most once a second, since warmup checks every second. False when the list
        // can't be read.
        public static bool IsLoaded(string name) => Find(name) != null;

        // A setting or action's "// conflicts: MatchZy, Other": one of those plugins is loaded, so it's hidden from the menus
        public static bool AnyLoaded(string names)
        {
            return names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any(IsLoaded);
        }

        // That plugin, when it's loaded
        public static IPlugin? Find(string name)
        {
            if (DateTime.UtcNow >= _cachedUntil)
            {
                try
                {
                    _cached = Read();
                }
                catch (Exception)
                {
                    _cached = null;
                }
                _cachedUntil = DateTime.UtcNow.AddSeconds(1);
            }
            return _cached?.FirstOrDefault(plugin => plugin.Matches(name))?.Plugin;
        }
    }
}
