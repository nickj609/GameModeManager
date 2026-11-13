// Included libraries
using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Plugin;
using CounterStrikeSharp.API.Core.Plugin.Host;

// Declare namespace
namespace GameModeManager.Panel
{
    // Define class
    // The Admin page's plugin list, as css_plugins list shows it (and CS2-SimpleAdmin's plugin manager): CounterStrikeSharp's
    // loaded plugins with their versions, plus folders in plugins/ that were never loaded. CounterStrikeSharp has no public
    // way to list plugins, so its plugin manager is read from Application.Instance (one private field, checked here with a
    // clear result when it's gone). Load, unload and reload are css_plugins itself, run as the server console.
    public static class PanelPlugins
    {
        // Define class properties
        private static readonly FieldInfo? ManagerField = typeof(Application).GetField("_pluginManager", BindingFlags.Instance | BindingFlags.NonPublic);

        // Define class methods
        // On the game thread. Null when the plugin manager can't be read (a CounterStrikeSharp update renamed it).
        public static List<PanelPlugin>? List(string selfAssembly)
        {
            if (Contexts() is not { } contexts)
                return null;

            var plugins = new List<PanelPlugin>();
            foreach (PluginContext context in contexts)
            {
                string folder = Folder(context.FilePath);
                plugins.Add(new PanelPlugin
                {
                    Name = folder,
                    Module = context.Plugin?.ModuleName ?? "",
                    Version = context.Plugin?.ModuleVersion ?? "",
                    Author = context.Plugin?.ModuleAuthor ?? "",
                    Description = context.Plugin?.ModuleDescription ?? "",
                    State = context.State.ToString().ToLowerInvariant(),
                    Protected = Path.GetFileNameWithoutExtension(context.FilePath).Equals(selfAssembly, StringComparison.OrdinalIgnoreCase)
                });
            }

            // Folders in plugins/ with a <name>/<name>.dll that CounterStrikeSharp never loaded ("disabled" is CSS's own parking place)
            string root = Path.Combine(PluginDatabase.CssRoot, "plugins");
            if (Directory.Exists(root))
            {
                foreach (string directory in Directory.GetDirectories(root))
                {
                    string name = Path.GetFileName(directory);
                    if (name.Equals("disabled", StringComparison.OrdinalIgnoreCase) || plugins.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    if (File.Exists(Path.Combine(directory, name + ".dll")))
                        plugins.Add(new PanelPlugin { Name = name, State = "notloaded" });
                }

                // plugins/disabled/<name>: kept from loading at start. The panel can load one for this run; moving it out of
                // disabled (so it loads on every start) stays a file change for the owner.
                string disabled = Path.Combine(root, "disabled");
                if (Directory.Exists(disabled))
                {
                    foreach (string directory in Directory.GetDirectories(disabled))
                    {
                        string name = Path.GetFileName(directory);
                        if (!plugins.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) && File.Exists(Path.Combine(directory, name + ".dll")))
                            plugins.Add(new PanelPlugin { Name = name, State = "disabled" });
                    }
                }
            }
            return plugins;
        }

        // Whether the plugin in plugins/<folder> is loaded now
        public static bool Loaded(string folder) =>
            Contexts()?.Any(c => c.State == CounterStrikeSharp.API.Core.Plugin.PluginState.Loaded && Folder(c.FilePath).Equals(folder, StringComparison.OrdinalIgnoreCase)) == true;

        // On the game thread: css_plugins load <folder>, or unload / reload by its id (reload only takes an id or module name)
        public static PanelResult Run(string action, string folder, string selfAssembly)
        {
            List<PanelPlugin>? plugins = List(selfAssembly);
            if (plugins == null)
                return PanelResult.Refused("CounterStrikeSharp's plugin list can't be read on this version.");
            PanelPlugin? plugin = plugins.FirstOrDefault(p => p.Name.Equals(folder, StringComparison.OrdinalIgnoreCase));
            if (plugin == null)
                return PanelResult.Refused("That plugin isn't on this server.");
            if (plugin.Protected)
                return PanelResult.Refused("GameModeManager can't unload or reload itself from the panel: the link would drop. Restart the server instead.");

            PluginContext? context = Contexts()?.FirstOrDefault(c => Folder(c.FilePath).Equals(plugin.Name, StringComparison.OrdinalIgnoreCase));
            switch (action)
            {
                case "load" when plugin.State == "disabled":
                    // A path ending in .dll is taken as it is, relative to the counterstrikesharp folder
                    Server.ExecuteCommand($"css_plugins load \"plugins/disabled/{plugin.Name}/{plugin.Name}.dll\"");
                    return PanelResult.Done($"Loading {plugin.Name} from plugins/disabled, until the server restarts.");
                case "load" when plugin.State != "loaded":
                    Server.ExecuteCommand($"css_plugins load \"{plugin.Name}\"");
                    return PanelResult.Done($"Loading {plugin.Name}.");
                case "unload" when context != null && plugin.State == "loaded":
                    Server.ExecuteCommand($"css_plugins unload {context.PluginId}");
                    return PanelResult.Done($"Unloading {plugin.Name}.");
                case "reload" when context != null && plugin.State == "loaded":
                    Server.ExecuteCommand($"css_plugins reload {context.PluginId}");
                    return PanelResult.Done($"Reloading {plugin.Name}. Plugins that use Harmony need a full restart to pick up a new DLL.");
                case "load":
                    return PanelResult.Refused("It's already loaded.");
                case "unload" or "reload":
                    return PanelResult.Refused("It isn't loaded.");
                default:
                    return PanelResult.Refused("Choose load, unload or reload.");
            }
        }

        private static IEnumerable<PluginContext>? Contexts()
        {
            try
            {
                return (ManagerField?.GetValue(Application.Instance) as IPluginManager)?.GetLoadedPlugins().ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string Folder(string filePath) => new FileInfo(filePath).Directory?.Name ?? Path.GetFileNameWithoutExtension(filePath);
    }
}
