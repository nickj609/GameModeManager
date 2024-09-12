﻿// Included libraries
using GameModeManager.Core;
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
        public override string ModuleVersion => "1.0.46";
        public override string ModuleAuthor => "Striker-Nick";
        public override string ModuleDescription => "A simple plugin to help administrators manage custom game modes, settings, and map rotations.";
        
        // Define dependencies
        private readonly PluginState _pluginState;
        private readonly VoteManager _voteManager;
        private readonly MenuFactory _menuFactory;
        private readonly ModeManager _modeManager;
        private readonly DependencyManager<Plugin, Config> _dependencyManager;

        // Register dependencies
        public Plugin(DependencyManager<Plugin, Config> dependencyManager,
            VoteManager voteManager,
            MenuFactory menuFactory, 
            PluginState pluginState, ModeManager modeManager)
        {
            _voteManager = voteManager;
            _menuFactory = menuFactory;
            _pluginState = pluginState;
            _modeManager = modeManager;
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