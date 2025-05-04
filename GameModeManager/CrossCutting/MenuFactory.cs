// Included libraries
using WASDMenuAPI.Shared;
using WASDMenuAPI.Shared.Models;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.CrossCutting
{
    // Define class
    public class MenuFactory : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Plugin? _plugin;
        private PluginState _pluginState;
        public static IWasdMenuManager? _wasdMenuManager;
        
        // Define class instance
        public MenuFactory(PluginState pluginState)
        {
            _pluginState = pluginState;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
        }

        // Define class methods
        public BaseMenu AssignMenu(string menuType, string menuName)
        {
            BaseMenu _baseMenu;

            if (menuType.Equals("center", StringComparison.OrdinalIgnoreCase) && _plugin != null)
            {
                _baseMenu = new CenterHtmlMenu(menuName, _plugin);
            }
            else
            {
                _baseMenu = new ChatMenu(menuName);
            }
            return _baseMenu;
        }

        public void OpenMenu(BaseMenu menu, CCSPlayerController player)
        {
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
                    case ConsoleMenu consoleMenu:
                        MenuManager.OpenConsoleMenu(player, consoleMenu);
                        break;
                }
            }
        }

        public IWasdMenu? AssignWasdMenu(string menuName)
        {
            IWasdMenu? menu = _pluginState.WasdMenuManager.Get()?.CreateMenu(menuName);
            return menu;
        }

        public void OpenWasdMenu(CCSPlayerController player, IWasdMenu menu)
        {
            _pluginState.WasdMenuManager.Get()?.OpenMainMenu(player, menu);
        }

        public void OpenWasdSubMenu(CCSPlayerController player, IWasdMenu menu)
        {
            _pluginState.WasdMenuManager.Get()?.OpenSubMenu(player, menu);
        }

        public void CloseWasdMenu(CCSPlayerController player)
        {
            _pluginState.WasdMenuManager.Get()?.CloseMenu(player);
        }

        public void CloseWasdSubMenu(CCSPlayerController player)
        {
            _pluginState.WasdMenuManager.Get()?.CloseSubMenu(player);
        }
    }
}