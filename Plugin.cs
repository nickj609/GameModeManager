﻿// Included libraries
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
        public override string ModuleDescription => "A simple plugin to help administrators manage custom game modes, settings, and map rotations.";

        // Define plugin
        private BasePlugin? _plugin;

        // Define custom vote API and signal
        public static PluginCapability<ICustomVoteApi> CustomVotesApi { get; } = new("custom_votes:api");
        private bool _isCustomVotesLoaded = false;
        private bool _RTV = false;


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
                    SetupSettingsMenu();
                }
            }
            catch(Exception ex)
            {
                Logger.LogError($"{ex.Message}");
            }

            // Register event handler
            try
            {
                _RTV = Config.RTV.Enabled;
                Logger.LogInformation($"Registering event handlers...");
                RegisterEventHandler<EventCsIntermission>(EventGameEnd);
                RegisterEventHandler<EventMapTransition>(EventMapChange);
            }
            catch(Exception ex)
            {
                Logger.LogError($"{ex.Message}");
            }
        }
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
                    RegisterCustomVotes();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"{ex.Message}");
                }
            }

            // Create game command menu
             try
            {
                UpdateGameMenu(); 
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
                        DeregisterCustomVotes();
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