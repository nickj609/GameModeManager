// Included libraries
using MenuManager;
using System.Reflection;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.CrossCutting
{
    // Define class
    // A menu row that does something straight away (picks a map, starts a vote, runs a command) instead of opening a page.
    // PanoramaMenuManager draws every plain row with a submenu chevron; one with its AddAction is drawn without it. AddAction
    // isn't in every PanoramaMenuManager version, so it's found at runtime: without it the row is a plain one, as before.
    public static class MenuActions
    {
        // Define class properties
        private static readonly Type[] Signature = { typeof(IMenu), typeof(string), typeof(Action<CCSPlayerController, ChatMenuOption>), typeof(bool) };
        private static IMenuApi? _api;
        private static MethodInfo? _addAction;

        // Define class methods
        public static ChatMenuOption? Add(IMenu? menu, string label, Action<CCSPlayerController, ChatMenuOption> onSelect, bool disabled = false)
        {
            if (menu == null)
                return null;

            IMenuApi? api = MenuFactory.Api;
            if (!ReferenceEquals(api, _api))
            {
                _api = api;
                _addAction = api?.GetType().GetMethod("AddAction", BindingFlags.Public | BindingFlags.Instance, null, Signature, null);
            }

            if (_addAction != null && api != null)
            {
                try
                {
                    return _addAction.Invoke(api, new object[] { menu, label, onSelect, disabled }) as ChatMenuOption;
                }
                catch (TargetInvocationException)
                {
                    // Falls back to a plain row
                }
            }
            return menu.AddMenuOption(label, onSelect, disabled);
        }
    }
}
