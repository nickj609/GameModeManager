// Included libraries
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.CrossCutting
{
    // Define class
    public class MenuFactory : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin? _plugin;

        // Define class instance
        public MenuFactory()
        {
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
        }

        // Define reusable method to assign menus
        public BaseMenu AssignMenu(string menuType, string menuName)
        {
            // Create base menu
            BaseMenu _baseMenu;

            // Assign chat or hud menu based on config
            if (menuType.Equals("center", StringComparison.OrdinalIgnoreCase) && _plugin != null)
            {
                _baseMenu = new CenterHtmlMenu(menuName, _plugin);
            }
            else
            {
                _baseMenu = new ChatMenu(menuName);
            }

            // Return assigned menu
            return _baseMenu;
        }

        // Define reusable method to open each type of menu
        public void OpenMenu(BaseMenu menu, CCSPlayerController player)
        {
            // Check if menu type from config is hud or chat menu
            if (_plugin != null)
            {
                switch (menu)
                {
                    case CenterHtmlMenu centerHtmlMenu:
                        MenuManager.OpenCenterHtmlMenu(_plugin, player, centerHtmlMenu);
                        break;
                    case ChatMenu chatMenu:
                        MenuManager.OpenChatMenu(player, chatMenu);
                        break;
                }
            }
        }
    }
}