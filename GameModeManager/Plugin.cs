// Included libraries
using GameModeManager.Core;
using GameModeManager.Menus;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.CrossCutting;
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
        // Define class properties
        public static Plugin? Instance;
        private bool _isCustomVotesLoaded = false;
        public override string ModuleName => "GameModeManager";
        public override string ModuleVersion => "1.0.60";
        public override string ModuleAuthor => "Striker-Nick";
        public override string ModuleDescription => "A simple plugin to help administrators manage custom game modes, settings, and map rotations.";
        
        // Define class dependencies
        private readonly MapMenus _mapMenus;
        private readonly ModeMenus _modeMenus;
        private readonly PlayerMenu _playerMenu;
        private readonly PluginState _pluginState;
        private readonly SettingMenus _settingMenus;
        private readonly NominateMenus _nominateMenus;
        private readonly CustomVoteManager _customVoteManager;
        private readonly DependencyManager<Plugin, Config> _dependencyManager;

        // Define class instance
        public Plugin(DependencyManager<Plugin, Config> dependencyManager, CustomVoteManager customVoteManager, PlayerMenu playerMenu, PluginState pluginState, MapMenus mapMenus, SettingMenus settingMenus, ModeMenus modeMenus, NominateMenus nominateMenus)
        {
            _mapMenus = mapMenus;
            _modeMenus = modeMenus;
            _playerMenu = playerMenu;
            _pluginState = pluginState;
            _settingMenus = settingMenus;
            _nominateMenus = nominateMenus;
            _customVoteManager = customVoteManager;
            _dependencyManager = dependencyManager;
        }

        // Define on load behavior
        public override void Load(bool hotReload)
        {   
            Instance = this;
            _dependencyManager.OnPluginLoad(this);
            RegisterListener<OnMapStart>(_dependencyManager.OnMapStart);
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
                    if (_pluginState.CustomVotesApi.Get() is null)
                        return;
                }
                catch (Exception)
                {
                    Logger.LogWarning("CS2-CustomVotes plugin not found. Custom votes will not be registered.");
                    return;
                }

                // Register custom votes
                _isCustomVotesLoaded = true;
                _customVoteManager.RegisterCustomVotes();
            }

            // Check if WASD menus are enabled
            if (Config.GameModes.Style.Equals("wasd") || Config.Maps.Style.Equals("wasd") || Config.Settings.Style.Equals("wasd") || Config.Votes.Style.Equals("wasd"))
            {
                try
                {
                    if (_pluginState.WasdMenuManager.Get() is null)
                        return;
                }
                catch (Exception)
                {
                    Logger.LogWarning("WASDSharedAPI plugin not found. WASD menus will not be work.");
                    return;
                }

                // Load WASD menus    
                _mapMenus.LoadWASDMenus();
                _playerMenu.LoadWASDMenu(); 
                _modeMenus.LoadWASDMenus();         
                _settingMenus.LoadWASDMenu();
                _nominateMenus.LoadWASDMenu();
            }
        }

        public override void Unload(bool hotReload)
        {
            if (_isCustomVotesLoaded)
            {
                Logger.LogInformation("Deregistering custom votes...");

                // Deregister custom votes
                try
                {
                    _customVoteManager.DeregisterCustomVotes();
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