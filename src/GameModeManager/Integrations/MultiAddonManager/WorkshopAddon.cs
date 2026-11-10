// Included libraries
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Cvars;

// Declare namespace
namespace GameModeManager.Integrations.MultiAddonManager
{
    // Define class
    // The Workshop addon(s) with the panorama files that menus need on the players' side. Added to MultiAddonManager's list
    // with its own command, so the server's other addons and its config file stay as they are. Checked every map: the
    // list is replaced if MultiAddonManager's config runs after this.
    public class WorkshopAddon : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Config _config = new Config();
        private ILogger<WorkshopAddon> _logger;

        // Define class constructor
        public WorkshopAddon(ILogger<WorkshopAddon> logger)
        {
            _logger = logger;
        }

        // Define class properties
        private bool _warned;

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            Server.NextFrame(AddToList);
        }

        // Define class methods
        private void AddToList()
        {
            string[] ids = _config.WorkshopAddon.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (ids.Length == 0)
                return;

            var list = ConVar.Find("mm_extra_addons");
            if (list == null)
            {
                if (!_warned)
                    _logger.LogError("MultiAddonManager not found: players won't get the Workshop addon, so menus won't display. Install it and turn it on.");
                _warned = true;
                return;
            }

            var listed = list.StringValue.Split(',', StringSplitOptions.TrimEntries);
            foreach (string id in ids.Except(listed))
            {
                Server.ExecuteCommand($"mm_add_addon {id}");
                _logger.LogInformation("Added Workshop addon {Id} to MultiAddonManager", id);
            }
        }
    }
}
