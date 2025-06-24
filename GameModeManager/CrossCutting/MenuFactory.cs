// Included libraries
using MenuManagerAPI.Shared;
using GameModeManager.Menus;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core.Capabilities;

// Declare namespace
namespace GameModeManager.CrossCutting
{
    // Define class
    public class MenuFactory : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private RTVMenus _rtvMenus;
        private MapMenus _mapMenus;
        private ModeMenus _modeMenus;
        private PlayerMenu _playerMenu;
        private SettingMenus _settingMenus;
        private NominateMenus _nominateMenus;
        private readonly PluginCapability<IMenuAPI> _pluginCapability = new("menu:api");

        // Define class constructor
        public MenuFactory(RTVMenus rtvMenus, MapMenus mapMenus, ModeMenus modeMenus, PlayerMenu playerMenu, SettingMenus settingMenus, NominateMenus nominateMenus)
        {
            _rtvMenus = rtvMenus;
            _mapMenus = mapMenus;
            _modeMenus = modeMenus;
            _playerMenu = playerMenu;
            _settingMenus = settingMenus;
            _nominateMenus = nominateMenus;
        }

        // Define class properties
        public static IMenuAPI? Api;

        // Define class methods
        public void Load()
        {
            Api = _pluginCapability.Get();
        }

        public void LoadMenus()
        {
            _rtvMenus.Load();
            _mapMenus.Load();
            _modeMenus.Load();
            _playerMenu.Load();
            _settingMenus.Load();
            _nominateMenus.Load();
        }
    }
}