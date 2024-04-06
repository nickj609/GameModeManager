// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager
{
    // Declare BasePlugin class
    public partial class Plugin : BasePlugin
    {
        // Define plugin details
        public override string ModuleName => "GameModeManager";
        public override string ModuleVersion => "1.0.3";
        public override string ModuleAuthor => "Striker-Nick";
        public override string ModuleDescription => "A simple plugin/module that dynamically updates any maplist.txt file based on the current mapgroup.";

        // Define plugin
        private BasePlugin? _plugin;

        // Construct On Load behavior
        public override void Load(bool hotReload)
        {   
            // Set plugin
            _plugin = this;

            // Parse map groups and set default map list and game modes
            try
            {
                ParseMapGroups();
            }
            catch(Exception ex)
            {
                Logger.LogError($"{ex.Message}");
            }

            // Setup mode admin menu
            try
            {
                SetupModeMenu();
            }
            catch (Exception ex)
            {
                Logger.LogError($"{ex.Message}");
            }
            
            // Enable default map cycle
            if(!Config.RTV.Enabled)
            {
                RegisterEventHandler<EventCsIntermission>(EventGameEnd);
            }
        }
    }
}