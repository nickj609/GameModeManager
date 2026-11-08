// Included libraries
using GameModeManager.Core;
using GameModeManager.Shared;
using GameModeManager.Services;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Core.Capabilities;
using Microsoft.Extensions.DependencyInjection;
using static CounterStrikeSharp.API.Core.Listeners;

// Declare namespace
namespace GameModeManager
{
    // Create class to load dependencies
    public class PluginDependencyInjection : IPluginServiceCollection<Plugin>
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var di = new DependencyManager<Plugin, Config>();
            di.LoadDependencies(typeof(Plugin).Assembly);
            di.AddIt(serviceCollection);
            serviceCollection.AddScoped<StringLocalizer>();
        }
    }
     // Define plugin class
    public partial class Plugin : BasePlugin
    {
        // Define plugin properties
        public override string ModuleName => "GameModeManager";
        public override string ModuleVersion => "2.3.1";
        public override string ModuleAuthor => "Striker-Nick";
        public override string ModuleDescription => "A simple plugin to help administrators manage custom game modes, settings, and map rotations.";
        
        // Define class dependencies
        private readonly MenuFactory _menuFactory;
        private readonly GameModeApi _gameModeApi;
        private readonly VoteService _voteService;
        private readonly MapChooserBridge _mapChooser;
        private readonly AdminPanel _adminPanel;
        private readonly DependencyManager<Plugin, Config> _dependencyManager;

        // Define class constructor
        public Plugin(DependencyManager<Plugin, Config> dependencyManager, VoteService voteService, GameModeApi gameModeApi, MenuFactory menuFactory, MapChooserBridge mapChooser, AdminPanel adminPanel)
        {
            _mapChooser = mapChooser;
            _adminPanel = adminPanel;
            _menuFactory = menuFactory;
            _gameModeApi = gameModeApi;
            _voteService = voteService;
            _dependencyManager = dependencyManager;
        }

        // Define class properties
        public static Plugin? Instance;
        private readonly PluginCapability<IGameModeApi?> _gameCapability = new("game_mode:api");

        // Define on load behavior
        public override void Load(bool hotReload)
        {   
            // Define plugin instance
            Instance = this;

            // Load plugin dependencies
            _dependencyManager.OnPluginLoad(this);
            RegisterListener<OnMapStart>(_dependencyManager.OnMapStart);

            // Load services
            Capabilities.RegisterPluginCapability(_gameCapability, () => _gameModeApi);
        }

        // Define class methods
        public override void OnAllPluginsLoaded(bool hotReload)
        {
            base.OnAllPluginsLoaded(hotReload);

            // Check if PanoramaMenuManager is installed
            try
            {
                _menuFactory.Load();
                if (MenuFactory.Api is null)
                    Logger.LogError("PanoramaMenuManager (menu:nfcore) not found: GameModeManager menus are disabled. Install MenuManagerCore and shared/MenuManagerApi.");
                _menuFactory.LoadMenus();
            }
            catch (Exception ex)
            {
                Logger.LogError("PanoramaMenuManager could not be loaded (is shared/MenuManagerApi/MenuManagerApi.dll installed?)");
                Logger.LogError(ex.Message);
            }

            // GG1MapChooser: write the mode pools and select the current mode's pool
            _mapChooser.Initialize();

            // CS2-SimpleAdmin: add the Game Modes section to its admin menu
            _adminPanel.Initialize();
            return;
        }

        public override void Unload(bool hotReload)
        {
            // Close a running vote (its panel / menus) before the plugin goes away
            _voteService.Cancel();
            _adminPanel.Unload();
            base.Unload(hotReload);
        }
    }
}