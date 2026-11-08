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

        // Define class methods
        // The row "text" on the page "menu" opens a page. Does nothing when PMM_SimpleAdmin isn't loaded.
        public static void Learn(object menu, string text)
        {
            try
            {
                if (menu.GetType().GetProperty("Title")?.GetValue(menu) is not string title || Arrows() is not IDictionary<string, bool> arrows)
                    return;

                arrows[Head(Clean(title)) + "\u0001" + Clean(text)] = true;
            }
            catch
            {
                // PMM_SimpleAdmin changed: its own learning from clicks still works
            }
        }

        // PMM_SimpleAdmin's running plugin and its learned rows
        private static IDictionary<string, bool>? Arrows()
        {
            Type? type = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "PMM_SimpleAdmin")?.GetType("PMM_SimpleAdmin.Plugin");
            object? plugin = type?.GetField("Instance", AnyStatic)?.GetValue(null);
            return plugin == null ? null : type!.GetField("_arrows", AnyInstance)?.GetValue(plugin) as IDictionary<string, bool>;
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
