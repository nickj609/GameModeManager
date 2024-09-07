// Declare namespace
namespace GameModeManager
{
    public class WarmupManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin? _plugin;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private TimeLimitManager _timeLimitManager;

        // Define class instance
        public WarmupManager(StringLocalizer localizer, PluginState pluginState, TimeLimitManager timeLimitManager)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _timeLimitManager = timeLimitManager;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Load dependencies
        public void OnLoad(Plugin plugin)
        { 
            _plugin = plugin;
            _localizer = new StringLocalizer(plugin.Localizer);
        }

        public void OnMapStart(string map)
        {
            if(_pluginState.WarmupModeEnabled == true && _plugin != null)
            {
                _timeLimitManager.EnforceTimeLimit(_plugin, true);
            }
        }
    }
}