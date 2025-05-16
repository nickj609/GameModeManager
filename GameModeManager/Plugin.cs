// Included libraries
using GameModeManager.Core;
using GameModeManager.Menus;
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
        public override string ModuleVersion => "1.0.78";
        public override string ModuleAuthor => "Striker-Nick";
        public override string ModuleDescription => "A simple plugin to help administrators manage custom game modes, settings, and map rotations.";
        
        // Define class dependencies
        private readonly RTVApi _rtvApi;
        private readonly MapMenus _mapMenus;
        private readonly ModeMenus _modeMenus;
        private readonly PlayerMenu _playerMenu;
        private readonly PluginState _pluginState;
        private readonly GameModeApi _gameModeApi; 
        private readonly TimeLimitApi _timeLimitApi;
        private readonly SettingMenus _settingMenus;
        private readonly NominateMenus _nominateMenus;
        private readonly CustomVoteManager _customVoteManager;
        private readonly DependencyManager<Plugin, Config> _dependencyManager;

        // Define class instance
        public Plugin(DependencyManager<Plugin, Config> dependencyManager, CustomVoteManager customVoteManager, PlayerMenu playerMenu, PluginState pluginState, 
        MapMenus mapMenus, SettingMenus settingMenus, ModeMenus modeMenus, NominateMenus nominateMenus, GameModeApi gameModeApi, TimeLimitApi timeLimitApi, RTVApi rtvApi)
        {
            _rtvApi = rtvApi;
            _mapMenus = mapMenus;
            _modeMenus = modeMenus;
            _playerMenu = playerMenu;
            _gameModeApi = gameModeApi;
            _pluginState = pluginState;
            _timeLimitApi = timeLimitApi;
            _settingMenus = settingMenus;
            _nominateMenus = nominateMenus;
            _customVoteManager = customVoteManager;
            _dependencyManager = dependencyManager;
        }

        // Define class properties
        public static Plugin? Instance;
        private bool _isCustomVotesLoaded = false;
        private readonly PluginCapability<IRTVApi?> _rtvCapability = new("rtv:api");
        private readonly PluginCapability<IGameModeApi?> _gameCapability = new("game_mode:api");
        private readonly PluginCapability<ITimeLimitApi?> _timeCapability = new("time_limit:api");

        // Define on load behavior
        public override void Load(bool hotReload)
        {   
            // Define plugin instance
            Instance = this;

            // Load plugin dependencies
            _dependencyManager.OnPluginLoad(this);
            RegisterListener<OnMapStart>(_dependencyManager.OnMapStart);

            // Load services
            Capabilities.RegisterPluginCapability(_rtvCapability, () => _rtvApi);
            Capabilities.RegisterPluginCapability(_gameCapability, () => _gameModeApi);
            Capabilities.RegisterPluginCapability(_timeCapability, () => _timeLimitApi);
        }

        // Define class methods
        public override void OnAllPluginsLoaded(bool hotReload)
        {
            base.OnAllPluginsLoaded(hotReload);

            // Check if custom votes are enabled
            if (Config.Votes.Enabled)
            {
                try
                {
                    if (CustomVoteManager.CustomVotesApi.Get() is null){}
                    _isCustomVotesLoaded = true;
                    _customVoteManager.RegisterCustomVotes();
                    return;
                }
                catch (Exception ex)
                {
                    _isCustomVotesLoaded = false;
                    Logger.LogWarning("CS2-CustomVotes plugin not found. Custom votes will not be registered.");
                    Logger.LogDebug(ex.Message);
                    return;
                }
            }

            // Check if WASD menus are enabled
            if (Config.GameModes.Style.Equals("wasd") || Config.Maps.Style.Equals("wasd") || Config.Settings.Style.Equals("wasd") || Config.Votes.Style.Equals("wasd"))
            {
                try
                {
                    if (MenuFactory.WasdMenuManager.Get() is null){}
                    _mapMenus.LoadWASDMenus();
                    _playerMenu.LoadWASDMenu(); 
                    _modeMenus.LoadWASDMenus();         
                    _settingMenus.LoadWASDMenu();
                    _nominateMenus.LoadWASDMenu();
                    return;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("WASDSharedAPI plugin not found. WASD menus will not be work.");
                    Logger.LogDebug(ex.Message);
                    return;
                }
            }
        }

        public override void Unload(bool hotReload)
        {
            if (_isCustomVotesLoaded)
            {
                try
                {
                    Logger.LogInformation("Deregistering custom votes...");
                    _customVoteManager.DeregisterCustomVotes();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("CS2-CustomVotes did not unload properly. You may need to reload the plugin.");
                    Logger.LogDebug(ex.Message);
                } 
            }
            base.Unload(hotReload);
        }
    }
}