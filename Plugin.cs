// Included libraries
using CS2_CustomVotes.Shared;

using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Core.Capabilities;

// Declare namespace
namespace GameModeManager
{
    // Declare BasePlugin class
    public partial class Plugin : BasePlugin
    {
        // Define plugin details
        public override string ModuleName => "GameModeManager";
        public override string ModuleVersion => "1.0.4";
        public override string ModuleAuthor => "Striker-Nick";
        public override string ModuleDescription => "A simple plugin to manage custom game modes and map rotations.";

        // Define plugin
        private BasePlugin? _plugin;

        // Define custom vote API and signal
        public static PluginCapability<ICustomVoteApi> CustomVotesApi { get; } = new("custom_votes:api");
        private bool _isCustomVotesLoaded = false;

        // Construct On Load behavior
        public override void Load(bool hotReload)
        {   
            base.Load(hotReload);

            // Define plugin context
            _plugin = this;

            // Parse map groups and set default map list and game modes
            try
            {
                Logger.LogInformation($"Loading map groups...");
                ParseMapGroups();
            }
            catch(Exception ex)
            {
                Logger.LogError($"{ex.Message}");
            }

            // Create mode menu
            try
            {
                Logger.LogInformation($"Creating game modes...");
                SetupModeMenu();
            }
            catch (Exception ex)
            {
                Logger.LogError($"{ex.Message}");
            }

            // Create settings menu
             try
            {
                if (Config.Settings.Enabled)
                {
                    Logger.LogInformation($"Loading settings...");
                    ParseSettings();
                    Logger.LogInformation($"Creating settings menu...");
                    SetupSettingsMenu();
                }
            }
            catch(Exception ex)
            {
                Logger.LogError($"{ex.Message}");
            }

            // Register EvenGameEnd handler if RTV is not enabled to perform map and game mode rotations
            if(!Config.RTV.Enabled)
            {
                Logger.LogInformation($"Enabling game mode and map rotations...");
                RegisterEventHandler<EventCsIntermission>(EventGameEnd);
            }
        }
        // When all plugins are loaded, register the CS2-CustomVotes plugin if it is enabled in the config
        public override void OnAllPluginsLoaded(bool hotReload)
        {
            base.OnAllPluginsLoaded(hotReload);

            if (Config.Votes.Enabled)
            {
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
                
                var votes = CustomVotesApi.Get();
                _isCustomVotesLoaded = true;
                Logger.LogInformation("Registering custom votes...");
                RegisterCustomVotes();
            }
        }
        // Constuct unload behavior to deregister votes
        public override void Unload(bool hotReload)
        {
                // Deregister votes and game events
                if (_isCustomVotesLoaded)
                {
                    Logger.LogInformation("Deregistering custom votes...");
                    DeregisterCustomVotes();
                }
                base.Unload(hotReload);
        }
    }
}