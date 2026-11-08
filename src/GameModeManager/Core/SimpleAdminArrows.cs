// Included libraries
using System.Reflection;
using System.Text.RegularExpressions;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    // PMM_SimpleAdmin draws "›" on a row only once it has seen that row open a page: CS2-SimpleAdmin doesn't say which rows do,
    // so it learns from clicks (Plugin._arrows, by page title and row text), and forgets on a restart. GameModeManager knows
    // its own submenus, so it tells PMM_SimpleAdmin as it adds them, and their arrows show from the first look.
    // PMM_SimpleAdmin isn't changed: its learned list is filled through reflection, like GG1Internals. Its key is
    // ArrowKey(title, option) in its Panel.cs; if that changes, rows just fall back to learning from clicks.
    public static class SimpleAdminArrows
    {
        // Define class properties
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Regex Tags = new(@"\{[A-Za-z_]+\}|<[^>]{1,40}>", RegexOptions.Compiled); // PMM_SimpleAdmin's TagRx

        public static int Learned { get; private set; } // Arrows set since the plugin loaded, for css_gmm_status

        // Define class methods
        // The row "text" on the page "menu" opens a page. Does nothing when PMM_SimpleAdmin isn't loaded.
        public static void Learn(object menu, string text)
        {
            try
            {
                if (Title(menu) is not string title || Arrows() is not IDictionary<string, bool> arrows)
                    return;

                arrows[Head(Clean(title)) + "\u0001" + Clean(text)] = true;
                Learned++;
            }
            catch
            {
                // PMM_SimpleAdmin changed: its own learning from clicks still works
            }
        }

        // The page's title as the panel gets it. CS2-SimpleAdmin's API hands out its MenuBuilder, which keeps the title in _title
        // and localizes it when the page opens (GetLocalizedTitle): the menu the panel draws has that title.
        private static string? Title(object menu)
        {
            Type type = menu.GetType();
            if (type.GetMethod("GetLocalizedTitle", AnyInstance, Type.EmptyTypes) is MethodInfo localized && localized.Invoke(menu, null) is string title)
                return title;

            return type.GetField("_title", AnyInstance)?.GetValue(menu) as string ?? type.GetProperty("Title")?.GetValue(menu) as string;
        }

        // PMM_SimpleAdmin's running plugin and its learned rows. The last loaded copy with a plugin: a hot reload can leave an
        // older one loaded.
        private static IDictionary<string, bool>? Arrows()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == "PMM_SimpleAdmin").Reverse())
            {
                Type? type = assembly.GetType("PMM_SimpleAdmin.Plugin");
                object? plugin = type?.GetField("Instance", AnyStatic)?.GetValue(null);
                if (plugin != null && type!.GetField("_arrows", AnyInstance)?.GetValue(plugin) is IDictionary<string, bool> arrows)
                    return arrows;
            }
            return null;
        }

        // The same cleaning as PMM_SimpleAdmin: no colour tags or short HTML, one line
        private static string Clean(string text)
        {
            return Tags.Replace(text, "").Replace('\n', ' ').Trim();
        }

        // A title without what follows its last ": " (a player's name, a value), like PMM_SimpleAdmin's SplitTitle
        private static string Head(string title)
        {
            int colon = title.LastIndexOf(": ", StringComparison.Ordinal);
            return colon <= 0 ? title : title[..colon].Trim();
        }
    }
}
