// Included libraries
using GameModeManager.Menus;
using GameModeManager.Core;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.DependencyInjection;
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
     // Define plugin class
    public partial class Plugin : BasePlugin
    {
        // Define plugin parameters
        public override string ModuleName => "GameModeManager";
        public override string ModuleVersion => "1.0.50";
        public override string ModuleAuthor => "Striker-Nick";
        public override string ModuleDescription => "A simple plugin to help administrators manage custom game modes, settings, and map rotations.";
        
        // Define dependencies
        private readonly MapMenus _mapMenus;
        private readonly ModeMenus _modeMenus;
        private readonly PlayerMenu _playerMenu;
        private readonly PluginState _pluginState;
        private readonly VoteManager _voteManager;
        private readonly SettingMenus _settingMenus;
        private readonly DependencyManager<Plugin, Config> _dependencyManager;

        // Register dependencies
        public Plugin(DependencyManager<Plugin, Config> dependencyManager,VoteManager voteManager, PlayerMenu playerMenu, PluginState pluginState, MapMenus mapMenus, SettingMenus settingMenus, ModeMenus modeMenus)
        {
            _mapMenus = mapMenus;
            _modeMenus = modeMenus;
            _playerMenu = playerMenu;
            _voteManager = voteManager;
            _pluginState = pluginState;
            _settingMenus = settingMenus;
            _dependencyManager = dependencyManager;
        }

        // Define on load behavior
        public override void Load(bool hotReload)
        {   
            // Load dependencies
            _dependencyManager.OnPluginLoad(this);

            // Register listeners
            RegisterListener<OnMapStart>(_dependencyManager.OnMapStart);
        }

        // Define custom vote API and signal
        private bool _isCustomVotesLoaded = false;

        // Define on all plugins loaded behavior
        public override void OnAllPluginsLoaded(bool hotReload)
        {
            base.OnAllPluginsLoaded(hotReload);

            if (!Config.RTV.Enabled)
            {
                Server.ExecuteCommand($"css_plugins unload {Config.RTV.Plugin}");
            }

            // Check if custom votes are enabled
            if (Config.Votes.Enabled)
            {
                // Ensure CS2-CustomVotes API is loaded
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
            
                // set unload flag
                _isCustomVotesLoaded = true;

                // Register custom votes
                _voteManager.RegisterCustomVotes();
            }

            // Check if WASDMenus are enabled
            if (Config.GameModes.Style.Equals("wasd") || Config.Maps.Style.Equals("wasd") || Config.Settings.Style.Equals("wasd") || Config.Votes.Style.Equals("wasd"))
            {
                // Ensure WASDSharedAPI is loaded
                try
                {
                    if (_pluginState.CustomVotesApi.Get() is null)
                        return;
                }
                catch (Exception)
                {
                    Logger.LogWarning("WASDSharedAPI plugin not found. WASD menus will not be work.");
                    return;
                }

                // Create WASD menus    
                _mapMenus.LoadWASDMenus();
                _playerMenu.LoadWASDMenu(); 
                _modeMenus.LoadWASDMenus();         
                _settingMenus.LoadWASDMenu();

            }
        }
        // Define method to unload plugin
        public override void Unload(bool hotReload)
        {
                // Deregister votes and game events
                if (_isCustomVotesLoaded)
                {
                    Logger.LogInformation("Deregistering custom votes...");

                    // Deregister custom votes
                    try
                    {
                        _voteManager.DeregisterCustomVotes();
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