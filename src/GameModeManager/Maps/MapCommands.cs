// Included libraries
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Maps
{
    // Define class
    public class MapCommands : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private MapMenus _mapMenus;
        private Config _config = new Config();
        private AdminPanel _adminPanel;


        // Define class constructor
        public MapCommands(AdminPanel adminPanel, MapMenus mapMenus)
        {
            _adminPanel = adminPanel;
            _mapMenus = mapMenus;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            // GG1MapChooser has its own css_maps; MapChooserBridge removes that one when all plugins are loaded
            if (_config.Commands.Maps)
                plugin.AddCommand("css_maps", "Displays a list of maps from the current mode.", OnMapsCommand);
        }

        // Define admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnMapsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null && !_adminPanel.Open(player, "maps"))
                _mapMenus.MainMenu?.Open(player);
        }
    }
}
