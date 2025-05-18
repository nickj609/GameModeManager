// Included libraries
using GameModeManager.Core;
using WASDMenuAPI.Shared.Models;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class NominateMenus
    {
        // Define class dependencies
        public WasdMenuController WasdMenus;
        public BaseMenuController BaseMenus;

        // Define class instance
        public NominateMenus(PluginState pluginState, StringLocalizer localizer, VoteOptionManager voteOptionManager, NominateManager nominateManager, Config config)
        {
            WasdMenus = new WasdMenuController(pluginState, localizer, nominateManager, voteOptionManager, config);
            BaseMenus = new BaseMenuController(pluginState, localizer, nominateManager, voteOptionManager, config);
        }

        // Define WasdMenuController class
        public class WasdMenuController(PluginState pluginState, StringLocalizer localizer, NominateManager nominateManager, VoteOptionManager voteOptionManager, Config config)
        {
            // Define class properties
            public IWasdMenu? MainMenu;
            public IWasdMenu? MapMenu;
            public IWasdMenu? ModeMenu;
            private MenuFactory menuFactory = new MenuFactory();

            // Define load method
            public void Load()
            {
                if (pluginState.RTV.Enabled)
                {
                    if (config.RTV.Style.Equals("wasd"))
                    {
                        // Create menus
                        MainMenu = menuFactory.WasdMenus.AssignMenu("Nominate");
                        MapMenu = menuFactory.WasdMenus.AssignMenu("Nominate");
                        ModeMenu = menuFactory.WasdMenus.AssignMenu("Nominate");

                        // Get options
                        List<string> options = voteOptionManager.GetOptions();

                        // Add menu options
                        foreach (string optionName in options)
                        {
                            if (voteOptionManager.OptionExists(optionName))
                            {
                                if (voteOptionManager.OptionType(optionName) == "map")
                                {
                                    MapMenu?.Add(optionName, (player, option) =>
                                    {
                                        nominateManager.Nominate(player, optionName);
                                        menuFactory.WasdMenus.CloseMenu(player);
                                    });
                                }
                                else if (voteOptionManager.OptionType(optionName) == "mode")
                                {
                                    ModeMenu?.Add(optionName, (player, option) =>
                                    {
                                        nominateManager.Nominate(player, optionName);
                                        menuFactory.WasdMenus.CloseMenu(player);
                                    });
                                }
                            }
                        }

                        // Create sub menu options
                        MainMenu?.Add(localizer.Localize("menu.maps"), (player, option) =>
                        {
                            if (MapMenu != null)
                            {
                                MapMenu.Prev = option.Parent?.Options?.Find(option);
                                menuFactory.WasdMenus.OpenSubMenu(player, MapMenu);
                            }
                        });

                        MainMenu?.Add(localizer.Localize("menu.modes"), (player, option) =>
                        {
                            if (ModeMenu != null)
                            {
                                ModeMenu.Prev = option.Parent?.Options?.Find(option);
                                menuFactory.WasdMenus.OpenSubMenu(player, ModeMenu);
                            }
                        });
                    }
                }
            }
        }

        // Define BaseMenuController class
        public class BaseMenuController(PluginState pluginState, StringLocalizer localizer, NominateManager nominateManager, VoteOptionManager voteOptionManager, Config config)
        {
            // Define class properties
            private MenuFactory menuFactory = new MenuFactory();
            public BaseMenu MainMenu = new ChatMenu("Nominations");
            public BaseMenu MapMenu = new ChatMenu("Nominations");
            public BaseMenu ModeMenu = new ChatMenu("Nominations");

            // Define load method
            public void Load()
            {
                if (pluginState.RTV.Enabled)
                {
                    // Create menus
                    MainMenu = menuFactory.BaseMenus.AssignMenu(config.RTV.Style, "Nominate");
                    MapMenu = menuFactory.BaseMenus.AssignMenu(config.RTV.Style, "Nominate");
                    ModeMenu = menuFactory.BaseMenus.AssignMenu(config.RTV.Style, "Nominate");

                    // Get options
                    List<string> options = voteOptionManager.GetOptions();

                    // Add menu options
                    foreach (string optionName in options)
                    {
                        if (voteOptionManager.OptionExists(optionName))
                        {

                            if (voteOptionManager.OptionType(optionName) == "map")
                            {
                                MapMenu.AddMenuOption(optionName, (player, option) =>
                                {
                                    nominateManager.Nominate(player, optionName);
                                    menuFactory.BaseMenus.CloseMenu(player);
                                });
                            }
                            else if (voteOptionManager.OptionType(optionName) == "mode")
                            {
                                ModeMenu.AddMenuOption(optionName, (player, option) =>
                                {
                                    nominateManager.Nominate(player, optionName);
                                    menuFactory.BaseMenus.CloseMenu(player);
                                });
                            }
                        }
                    }

                    // Create sub menu options
                    if (pluginState.RTV.IncludeExtend && pluginState.RTV.MapExtends < pluginState.RTV.MaxExtends)
                    {
                        MainMenu.AddMenuOption("Extend", (player, option) =>
                    {
                        nominateManager.Nominate(player, "Extend");
                        menuFactory.WasdMenus.CloseMenu(player);
                    });
                    }

                    MainMenu.AddMenuOption("Map", (player, option) =>
                    {
                        MapMenu.Title = localizer.Localize("nominate.menu-title");

                        if (player != null)
                        {
                            menuFactory.BaseMenus.OpenMenu(MapMenu, player);
                        }
                    });

                    MainMenu.AddMenuOption("Mode", (player, option) =>
                    {
                        ModeMenu.Title = localizer.Localize("nominate.menu-title");

                        if (player != null)
                        {
                            menuFactory.BaseMenus.OpenMenu(ModeMenu, player);
                        }
                    });
                }
            }
        }

    }
}