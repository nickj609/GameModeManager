// Included libraries
using WASDSharedAPI;
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

        // Define method to assign menus
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

        // Define method to open each type of menu
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