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
        public override string ModuleVersion => "1.0.64";
        public override string ModuleAuthor => "Striker-Nick";
        public override string ModuleDescription => "A simple plugin to help administrators manage custom game modes, settings, and map rotations.";
        
        // Define class dependencies
        private readonly RTVApi _rtvApi;
        private readonly MenuFactory _menuFactory;
        private readonly GameModeApi _gameModeApi;
        private readonly TimeLimitApi _timeLimitApi;
        private readonly CustomVoteManager _customVoteManager;
        private readonly DependencyManager<Plugin, Config> _dependencyManager;

        // Define class constructor
        public Plugin(DependencyManager<Plugin, Config> dependencyManager, CustomVoteManager customVoteManager, GameModeApi gameModeApi, TimeLimitApi timeLimitApi, RTVApi rtvApi, MenuFactory menuFactory)
        {
            _rtvApi = rtvApi;
            _menuFactory = menuFactory;
            _gameModeApi = gameModeApi;
            _timeLimitApi = timeLimitApi;
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
                }
                catch (Exception ex)
                {
                    _isCustomVotesLoaded = false;
                    Logger.LogWarning("CS2-CustomVotes plugin not found. Custom votes will not be registered.");
                    Logger.LogDebug(ex.Message);
                }
            }

            // Check if MenuManagerApi is enabled
            try
            {
                _menuFactory.Load();
                if (MenuFactory.Api is null){}
                _menuFactory.LoadMenus();
            }
            catch (Exception ex)
            {
                Logger.LogError("MenuManager plugin not found.");
                Logger.LogError(ex.Message);
            }
            return;
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