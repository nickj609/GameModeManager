// Included libraries
using GameModeManager.Core;
using GameModeManager.Features;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
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
        public override string ModuleVersion => "1.0.45";
        public override string ModuleAuthor => "Striker-Nick";
        public override string ModuleDescription => "A simple plugin to help administrators manage custom game modes, settings, and map rotations.";
        
        // Define dependencies
        private readonly GameRules _gameRules;
        private readonly RTVCommand _rtvCommand;
        private readonly MapManager _mapManager;
        private readonly PluginState _pluginState;
        private readonly VoteManager _voteManager;
        private readonly MenuFactory _menuFactory;
        private readonly MapCommands _mapCommands;
        private readonly ModeCommands _modeCommands;
        private readonly StringLocalizer _localizer;
        private readonly PlayerCommands _playerCommands;
        private readonly SettingCommands _settingCommands;
        private readonly RotationManager _rotationManager;
        private readonly TimeLimitManager _timeLimitManager;
        private readonly MaxRoundsManager _maxRoundsManager;
        private readonly DependencyManager<Plugin, Config> _dependencyManager;

        // Register dependencies
        public Plugin(DependencyManager<Plugin, Config> dependencyManager,
            GameRules gameRules,
            MapManager mapManager,
            RTVCommand rtvCommand, 
            VoteManager voteManager,
            MenuFactory menuFactory, 
            PluginState pluginState, 
            MapCommands mapCommands, 
            ModeCommands modeCommands, 
            PlayerCommands playerCommands, 
            SettingCommands settingCommands,
            RotationManager rotationManager,
            TimeLimitManager timeLimitManager,
            MaxRoundsManager maxRoundsManager)
        {
            _gameRules = gameRules;
            _rtvCommand = rtvCommand;
            _mapManager = mapManager;
            _voteManager = voteManager;
            _menuFactory = menuFactory;
            _pluginState = pluginState;
            _mapCommands = mapCommands;
            _modeCommands = modeCommands;
            _playerCommands = playerCommands;
            _rotationManager= rotationManager;
            _settingCommands = settingCommands;
            _timeLimitManager = timeLimitManager;
            _maxRoundsManager = maxRoundsManager;
            _dependencyManager = dependencyManager;
            _localizer = new StringLocalizer(Localizer);
        }

        // Define method to load plugin
        public override void Load(bool hotReload)
        {   
            // Load dependencies
            _dependencyManager.OnPluginLoad(this);

            // Register listeners
            RegisterListener<OnMapStart>(_dependencyManager.OnMapStart);
        }

        // Define custom vote API and signal
        private bool _isCustomVotesLoaded = false;

        // When all plugins are loaded, register the CS2-CustomVotes plugin if it is enabled in the config
        public override void OnAllPluginsLoaded(bool hotReload)
        {
            base.OnAllPluginsLoaded(hotReload);

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
            
            // Create game menu
            _menuFactory.UpdateGameMenu(); 
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