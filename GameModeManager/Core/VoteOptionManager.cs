// Included libraries
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class VoteOptionManager : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Config _config = new();
        private PluginState _pluginState;
        private NominateManager _nominateManager;

        // Define class constructor
        public VoteOptionManager(PluginState pluginState, NominateManager nominateManager)
        {
            _pluginState = pluginState;
            _nominateManager = nominateManager;
        }

        // Define class properties
        private int optionsToShow = 5;
        private List<IMap> maps = new();
        private List<IMode> modes = new();
        public const int MAX_OPTIONS_HUD_MENU = 5; 
        private HashSet<VoteOption> allOptions = new(); 
        private Dictionary<long, VoteOption> optionsById = new();
        private Dictionary<string, VoteOption> optionsByName = new(StringComparer.OrdinalIgnoreCase);

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
            optionsToShow = _config.RTV.OptionsToShow == 0 ? MAX_OPTIONS_HUD_MENU : _config.RTV.OptionsToShow;
            _pluginState.RTV.IncludeExtend = _config.RTV.IncludeExtend;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            if (_pluginState.RTV.Enabled)
            {
                LoadOptions();
            }
        }

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            if(_pluginState.RTV.Enabled)
            {
                LoadOptions();
            }
        }

        // Define class methods
        public List<VoteOption> GetOptions()
        {
            List<VoteOption> options = [.. allOptions];
            return options;
        }

        public VoteOption? GetOptionByName(string name)
        {
            return optionsByName.TryGetValue(name, out VoteOption? option) ? option : null;
        }

        public VoteOption? GetOptionById(long id)
        {
            return optionsById.TryGetValue(id, out VoteOption? option) ? option : null;
        }

        public void LoadOptions()
        {
            allOptions.Clear();
            optionsToShow = _config!.RTV.OptionsToShow == 0 ? MAX_OPTIONS_HUD_MENU : _config!.RTV.OptionsToShow;

            if (_config.Maps.Mode == 1)
            {
                maps = _pluginState.Game.Maps.Values.ToList();
            }
            else
            {
                maps = _pluginState.Game.CurrentMode.Maps.ToList();
            }

            if (_config.RTV.IncludeModes)
            {
                modes = _pluginState.Game.Modes.Values.ToList();
            }

            foreach (IMap map in maps)
            {
                VoteOption option = new VoteOption(map.Name, map.WorkshopId, map.DisplayName, VoteOptionType.Map);

                if (!option.Name.Equals(Server.MapName) && !_nominateManager.IsOptionInCooldown(option))
                {
                    allOptions.Add(option);
                    optionsByName.TryAdd(map.Name, option);

                    if (option.WorkshopId > 0)
                    {
                        optionsById.TryAdd(option.WorkshopId, option);
                    }
                }
            }

            foreach (IMode mode in modes)
            {
                VoteOption option = new VoteOption(mode.Name, VoteOptionType.Mode);

                if (!option.Name.Equals(_pluginState.Game.CurrentMode.Name) && !_nominateManager.IsOptionInCooldown(option))
                {
                    allOptions.Add(option);
                    optionsByName.TryAdd(mode.Name, option);
                }
            }
        }
        
        public List<VoteOption> ScrambleOptions()
        {
            List<VoteOption> options = new();
            List<VoteOption> optionsEllected;
            List<VoteOption> mapNominationWinners = _nominateManager.MapNominationWinners();
            List<VoteOption> modeNominationWinners = _nominateManager.ModeNominationWinners();
            List<IMap> mapsScrambled = PluginExtensions.Shuffle(new Random(), maps).ToList();
            List<IMode> modesScrambled = PluginExtensions.Shuffle(new Random(), modes).ToList();
            
            if (_pluginState.RTV.IncludeExtend && _pluginState.RTV.MapExtends < _pluginState.RTV.MaxExtends)
            {
                options.Add(new VoteOption("Extend", VoteOptionType.Extend));
                optionsToShow--;
            }
            
            if (_config.RTV.IncludeModes)
            {
                int modesToInclude = (int)((optionsToShow * (_config.RTV.ModePercentage / 100.0)) - modeNominationWinners.Count);
                int mapsToInclude = optionsToShow - modesToInclude - mapNominationWinners.Count;

                foreach (IMode mode in modesScrambled.Take(modesToInclude))
                {
                    options.Add(new VoteOption(mode.Name, VoteOptionType.Mode));
                }
                foreach (IMap map in mapsScrambled.Take(mapsToInclude))
                {
                    options.Add(new VoteOption(map.Name, map.WorkshopId, map.DisplayName, VoteOptionType.Map));
                }
                optionsEllected = modeNominationWinners.Distinct().ToList(); 
            }
            else
            {
                foreach (IMap map in mapsScrambled.Take(optionsToShow - mapNominationWinners.Count))
                {
                    options.Add(new VoteOption(map.Name, map.WorkshopId, map.DisplayName, VoteOptionType.Map));
                }
                optionsEllected = mapNominationWinners.Distinct().ToList();
            }
        
            List<VoteOption> optionsScrambled = PluginExtensions.Shuffle(new Random(), options).ToList();
            optionsEllected = optionsEllected.Concat(optionsScrambled).Distinct().ToList();

            return optionsEllected;
        }
    }
}