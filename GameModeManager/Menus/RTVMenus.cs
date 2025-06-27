// Included libraries
using GameModeManager.Core;
using GameModeManager.Models;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class RTVMenus : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private VoteManager _voteManager;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private VoteOptionManager _voteOptionManager;

        // Define class constructor
        public RTVMenus(PluginState pluginState, StringLocalizer localizer, VoteManager voteManager, VoteOptionManager voteOptionManager)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _voteManager = voteManager;
            _voteOptionManager = voteOptionManager;
        }
        
        // Define class properties
        public IMenu? MainMenu;

        // Define load method
        public void Load()
        {
            // Load options
            _pluginState.RTV.Votes.Clear();
            List<VoteOption> options = _voteOptionManager.ScrambleOptions();

            // Create main menu
            MainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("rtv.hud.menu-title"));

            foreach (VoteOption voteOption in options)
            {
                _pluginState.RTV.Votes[voteOption] = 0;
                string _displayName = voteOption.DisplayName;

                if (voteOption.Type == VoteOptionType.Map)
                {
                    _displayName = string.Concat($"<font color='{_localizer.Localize("rtv.map.color")}'>", voteOption.DisplayName, "</font>");
                }
                else if (voteOption.Type == VoteOptionType.Mode)
                {
                    _displayName = string.Concat($"<font color='{_localizer.Localize("rtv.mode.color")}'>", voteOption.DisplayName, "</font>");
                }
                else
                {
                    _displayName = string.Concat($"<font color='{_localizer.Localize("rtv.extend.color")}'>", voteOption.DisplayName, "</font>");
                }

                MainMenu?.AddMenuOption(_displayName, (player, option) =>
                {
                    _voteManager.AddVote(player, voteOption);
                    MenuFactory.Api?.CloseMenu(player);
                });
            }
        }
    }
}