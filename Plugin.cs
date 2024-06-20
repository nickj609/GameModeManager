// Included libraries
using CS2_CustomVotes.Shared;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using CounterStrikeSharp.API.Core.Capabilities;
using static CounterStrikeSharp.API.Core.Listeners;

// Declare namespace
namespace GameModeManager
{
    // Create class to locate dependencies
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
     // Define BasePlugin class
    public partial class Plugin : BasePlugin
    {
        // Define plugin details
        public override string ModuleName => "GameModeManager";
        public override string ModuleVersion => "1.0.44";
        public override string ModuleAuthor => "Striker-Nick";
        public override string ModuleDescription => "A simple plugin to help administrators manage custom game modes, settings, and map rotations.";
        
        // Define dependencies
        private readonly MapManager _mapManager;
        private readonly VoteManager _voteManager;
        private readonly MenuFactory _menuFactory;
        private readonly StringLocalizer _localizer;
        private readonly MapGroupManager _mapGroupManager;
        private readonly SettingsManager _settingsManager;
        private readonly DependencyManager<Plugin, Config> _dependencyManager;

        // Register dependencies
        public Plugin(DependencyManager<Plugin, Config> dependencyManager,
            MapManager mapManager,
            MapGroupManager mapGroupManager,
            VoteManager voteManager,
            SettingsManager settingsManager,
            MenuFactory menuFactory)
        {
            _dependencyManager = dependencyManager;
            _mapManager = mapManager;
            _mapGroupManager = mapGroupManager;
            _voteManager = voteManager;
            _settingsManager = settingsManager;
            _menuFactory = menuFactory;
            _localizer = new StringLocalizer(Localizer);
        }

        // Construct On Load behavior
        public override void Load(bool hotReload)
        {   
            // Load dependencies
            _dependencyManager.OnPluginLoad(this);

            // Error handling
            try
            {
                // Load map groups
                Logger.LogInformation($"Loading map groups...");
                MapGroupManager.Load();

                // Load settings
                if (Config.Settings.Enabled)
                {
                    Logger.LogInformation($"Loading settings...");
                    SettingsManager.Load();
                }

                // Create menus
                Logger.LogInformation($"Creating menus...");
                MenuFactory.Load();

                // Register event handlers
                Logger.LogInformation($"Registering event handlers...");
                RegisterEventHandler<EventCsWinPanelMatch>(EventGameEnd);
                RegisterEventHandler<EventMapTransition>(EventMapChange);

                // Register listeners
                RegisterListener<OnMapStart>(_dependencyManager.OnMapStart);
            }
            catch(Exception ex)
            {
                Logger.LogError($"{ex.Message}");
            }
        }

        // Define custom vote API and signal
        private bool _isCustomVotesLoaded = false;
        private PluginCapability<ICustomVoteApi> CustomVotesApi { get; } = new("custom_votes:api");

        // When all plugins are loaded, register the CS2-CustomVotes plugin if it is enabled in the config
        public override void OnAllPluginsLoaded(bool hotReload)
        {
            base.OnAllPluginsLoaded(hotReload);

            if (Config.Votes.Enabled)
            {
                // Ensure CS2-CustomVotes API is loaded
                try
                {
                    if (CustomVotesApi.Get() is null)
                        return;
                }
                catch (Exception)
                {
                    Logger.LogWarning("CS2-CustomVotes plugin not found. Custom votes will not be registered.");
                    return;
                }
                
                _isCustomVotesLoaded = true;

                // Register custom votes
                try
                {
                    VoteManager.RegisterCustomVotes();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"{ex.Message}");
                }
            }

            // Create game command menu
             try
            {
                MenuFactory.UpdateGameMenu(); 
            }
            catch(Exception ex)
            {
                Logger.LogError($"{ex.Message}");
            }
        }
        // Constuct unload behavior to deregister votes
        public override void Unload(bool hotReload)
        {
                // Deregister votes and game events
                if (_isCustomVotesLoaded)
                {
                    Logger.LogInformation("Deregistering custom votes...");

                    // Deregister custom votes
                    try
                    {
                        VoteManager.DeregisterCustomVotes();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"{ex.Message}");
                    } 
                }
                // Unload plugin
                base.Unload(hotReload);
        }
    }
}