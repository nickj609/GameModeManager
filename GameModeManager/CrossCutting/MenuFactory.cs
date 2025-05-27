// Included libraries
using WASDMenuAPI.Shared;
using WASDMenuAPI.Shared.Models;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Core.Capabilities;

// Declare namespace
namespace GameModeManager.CrossCutting
{
    // Define class
    public class MenuFactory
    {   
        // Define class properties
        public BaseMenuController BaseMenus;
        public WasdMenuController WasdMenus;
        public static PluginCapability<IWasdMenuManager> WasdMenuManager { get; } = new("wasdmenu:manager");

        // Define class instance
        public MenuFactory(Plugin? plugin)
        {
            BaseMenus = new BaseMenuController(plugin);
            WasdMenus = new WasdMenuController(WasdMenuManager);
        }

        // Define BaseMenu class
        public class BaseMenuController(Plugin? plugin)
        {
            public BaseMenu AssignMenu(string menuType, string menuName)
            {
                BaseMenu _baseMenu;

                if (menuType.Equals("center", StringComparison.OrdinalIgnoreCase) && plugin != null)
                {
                    _baseMenu = new CenterHtmlMenu(menuName, plugin);
                }
                else if (menuType.Equals("console", StringComparison.OrdinalIgnoreCase))
                {
                    _baseMenu = new ConsoleMenu(menuName);
                }
                else
                {
                    _baseMenu = new ChatMenu(menuName);
                }
                return _baseMenu;
            }

            public void OpenMenu(BaseMenu menu, CCSPlayerController player)
            {
                if (plugin != null)
                {
                    switch (menu)
                    {
                        case CenterHtmlMenu centerHtmlMenu:
                            MenuManager.OpenCenterHtmlMenu(plugin, player, centerHtmlMenu);
                            break;
                        case ChatMenu chatMenu:
                            MenuManager.OpenChatMenu(player, chatMenu);
                            break;
                        case ConsoleMenu consoleMenu:
                            MenuManager.OpenConsoleMenu(player, consoleMenu);
                            break;
                    }
                }
            }

            public void CloseMenu(CCSPlayerController player)
            {
                MenuManager.CloseActiveMenu(player);
            }
        }

        // Define WASDMenu class
        public class WasdMenuController (PluginCapability<IWasdMenuManager> wasdMenuManager)
        {
            public IWasdMenu? AssignMenu(string menuName)
            {
                IWasdMenu? menu = wasdMenuManager.Get()?.CreateMenu(menuName);
                return menu;
            }

            public void OpenMenu(CCSPlayerController player, IWasdMenu menu)
            {
                wasdMenuManager.Get()?.OpenMainMenu(player, menu);
            }

            public void OpenSubMenu(CCSPlayerController player, IWasdMenu menu)
            {
                wasdMenuManager.Get()?.OpenSubMenu(player, menu);
            }

            public void CloseMenu(CCSPlayerController player)
            {
                wasdMenuManager.Get()?.CloseMenu(player);
            }

            public void CloseSubMenu(CCSPlayerController player)
            {
                wasdMenuManager.Get()?.CloseSubMenu(player);
            }
        }
    }
}