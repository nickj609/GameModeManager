// Included libraries
using MenuManager;
using CounterStrikeSharp.API.Core.Capabilities;

// Declare namespace
namespace GameModeManager.Common
{
    // Define class
    public class MenuFactory : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private MapMenus _mapMenus;
        private ModeMenus _modeMenus;
        private WarmupMenus _warmupMenus;
        private PlayerMenu _playerMenu;
        private SettingMenus _settingMenus;
        // PanoramaMenuManager (MenuManagerCore)
        private readonly PluginCapability<IMenuApi?> _pluginCapability = new("menu:nfcore");

        // Define class constructor
        public MenuFactory(MapMenus mapMenus, WarmupMenus warmupMenus, ModeMenus modeMenus, PlayerMenu playerMenu, SettingMenus settingMenus)
        {
            _mapMenus = mapMenus;
            _warmupMenus = warmupMenus;
            _modeMenus = modeMenus;
            _playerMenu = playerMenu;
            _settingMenus = settingMenus;
        }

        // Define class properties
        public static IMenuApi? Api;

        // Define class methods
        public void Load()
        {
            Api = _pluginCapability.Get();
        }

        public void LoadMenus()
        {
            _mapMenus.Load();
            _modeMenus.Load();
            _warmupMenus.Load();
            _playerMenu.Load();
            _settingMenus.Load();
        }
    }
}