// Included libraries
using GameModeManager.Core;
using GameModeManager.Features;
using WASDMenuAPI.Shared.Models;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class PlayerMenu
    {

        // Define class instance
        public PlayerMenu(Plugin? plugin, PluginState pluginState, StringLocalizer localizer, TimeLimitManager timeLimitManager, GameRules gameRules, VoteManager voteManager, MaxRoundsManager maxRoundsManager,
        AsyncVoteManager asyncVoteManager, MapMenus mapMenus, ModeMenus modeMenus, SettingMenus settingMenus, NominateMenus nominateMenus, Config config)
        {
            BaseMenus = new BaseMenuController(new MenuFactory(plugin), pluginState, localizer, timeLimitManager, gameRules, voteManager, maxRoundsManager, asyncVoteManager, mapMenus, modeMenus, settingMenus, nominateMenus, config);
            WasdMenus = new WasdMenuController(new MenuFactory(plugin), pluginState, localizer, timeLimitManager, gameRules, voteManager, maxRoundsManager, asyncVoteManager, mapMenus, modeMenus, settingMenus, nominateMenus, config);
            BaseMenus.Load();
            WasdMenus.Load();
        }

        // Define class properties
        public BaseMenuController BaseMenus;
        public WasdMenuController WasdMenus;

        // Define BaseMenuController class
        public class BaseMenuController(MenuFactory menuFactory, PluginState pluginState, StringLocalizer localizer, TimeLimitManager timeLimitManager, GameRules gameRules, VoteManager voteManager, MaxRoundsManager maxRoundsManager,
        AsyncVoteManager asyncVoteManager, MapMenus mapMenus, ModeMenus modeMenus, SettingMenus settingMenus, NominateMenus nominateMenus, Config config)
        {
            // Define class properties
            public BaseMenu MainMenu = new ChatMenu("Command List");

            // Define load method
            public void Load()
            {
                // Create main menu
                MainMenu = menuFactory.BaseMenus.AssignMenu(config.Settings.Style, "Game Commands");

                foreach (string _command in pluginState.Game.PlayerCommands)
                {
                    switch (_command)
                    {
                        case "!changemap":
                            MainMenu.AddMenuOption("Change Map", (player, option) =>
                            {
                                menuFactory.BaseMenus.CloseMenu(player);

                                if (player != null && config.Votes.Enabled && config.Votes.Maps)
                                {
                                    BaseMenu menu = mapMenus.BaseMenus.VoteMenu;
                                    menuFactory.BaseMenus.OpenMenu(menu, player);
                                }
                            });
                            break;
                        case "!changemode":
                            MainMenu.AddMenuOption("Change Mode", (player, option) =>
                            {
                                menuFactory.BaseMenus.CloseMenu(player);

                                if (player != null && config.Votes.Enabled && config.Votes.GameModes)
                                {
                                    BaseMenu menu = modeMenus.BaseMenus.VoteMenu;
                                    menuFactory.BaseMenus.OpenMenu(menu, player);
                                }
                            });
                            break;
                        case "!changesetting":
                            MainMenu.AddMenuOption("Change Setting", (player, option) =>
                            {
                                menuFactory.BaseMenus.CloseMenu(player);

                                if (player != null && config.Votes.Enabled && config.Votes.GameSettings)
                                {
                                    menuFactory.BaseMenus.OpenMenu(settingMenus.BaseMenus.MainMenu, player);
                                }
                            });
                            break;
                        case "!currentmode":
                            MainMenu.AddMenuOption("Current Mode", (player, option) =>
                            {
                                menuFactory.BaseMenus.CloseMenu(player);

                                if (player != null)
                                {
                                    player.PrintToChat(localizer.Localize("currentmode.message", pluginState.Game.CurrentMode.Name));
                                }
                            });
                            break;
                        case "!currentmap":
                            MainMenu.AddMenuOption("Current Map", (player, option) =>
                            {
                                menuFactory.BaseMenus.CloseMenu(player);

                                if (player != null)
                                {
                                    player.PrintToChat(localizer.Localize("currentmap.message", pluginState.Game.CurrentMap.Name));
                                }
                            });
                            break;
                        case "!nextmap":
                            MainMenu.AddMenuOption("Next Map", (player, option) =>
                            {
                                menuFactory.BaseMenus.CloseMenu(player);

                                if (player != null)
                                {
                                    if (pluginState.RTV.NextMap != null && pluginState.RTV.NextMode == null)
                                    {
                                        player.PrintToChat(localizer.Localize("rtv.nextmap.message", pluginState.RTV.NextMap.DisplayName));
                                    }
                                    else if (pluginState.RTV.NextMap == null && pluginState.RTV.NextMode != null)
                                    {
                                        player.PrintToChat(localizer.Localize("rtv.nextmap.message", "Random"));
                                    }
                                    else
                                    {
                                        player.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.no-vote"));
                                    }
                                }
                            });
                            break;
                        case "!nextmode":
                            MainMenu.AddMenuOption("Next Mode", (player, option) =>
                            {
                                menuFactory.BaseMenus.CloseMenu(player);

                                if (player != null)
                                {
                                    if (pluginState.RTV.NextMode != null)
                                    {
                                        player.PrintToChat(localizer.Localize("rtv.nextmode.message", pluginState.RTV.NextMode.Name));
                                    }
                                    else if (pluginState.RTV.NextMap != null && pluginState.RTV.NextMode == null)
                                    {
                                        player.PrintToChat(localizer.Localize("rtv.nextmode.message", pluginState.Game.CurrentMode.Name));
                                    }
                                    else
                                    {
                                        player.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.no-vote"));
                                    }
                                }
                            });
                            break;
                        case "!rtv":
                            MainMenu.AddMenuOption("RockTheVote", (player, option) =>
                            {
                                menuFactory.BaseMenus.CloseMenu(player);
                                asyncVoteManager.RTVCounter(player);

                            });
                            break;
                        case "!nominate":
                            MainMenu.AddMenuOption("Nominate", (player, option) =>
                            {
                                menuFactory.BaseMenus.CloseMenu(player);

                                if (player != null)
                                {
                                    if (pluginState.RTV.EofVoteHappened)
                                    {
                                        if (!timeLimitManager.UnlimitedTime())
                                        {
                                            string timeleft = voteManager.GetTimeLeft();
                                            player.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "rtv.schedule-change", timeleft));
                                        }
                                        else if (!maxRoundsManager.UnlimitedRounds)
                                        {
                                            string roundsleft = voteManager.GetRoundsLeft();
                                            player.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "rtv.schedule-change", roundsleft));
                                        }
                                        return;
                                    }

                                    if (pluginState.RTV.DisableCommands || !pluginState.RTV.NominationEnabled)
                                    {
                                        player.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.disabled"));
                                        return;
                                    }

                                    if (gameRules.WarmupRunning)
                                    {
                                        if (!config.RTV.EnabledInWarmup)
                                        {
                                            player.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.warmup"));
                                            return;
                                        }
                                    }
                                    else if (config.RTV.MinRounds > 0 && config.RTV.MinRounds > gameRules.TotalRoundsPlayed)
                                    {
                                        player!.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.minimum-rounds", config.RTV.MinRounds));
                                        return;
                                    }

                                    if (PlayerExtensions.ValidPlayerCount() < config!.RTV.MinPlayers)
                                    {
                                        player.PrintToChat(localizer.LocalizeWithPrefix("general.validation.minimum-players", config!.RTV.MinPlayers));
                                        return;
                                    }

                                    BaseMenu menu = nominateMenus.BaseMenus.MainMenu;
                                    menuFactory.BaseMenus.OpenMenu(menu, player);
                                }
                            });
                            break;
                        case "!timeleft":
                            MainMenu.AddMenuOption("Time Left", (player, option) =>
                            {
                                menuFactory.BaseMenus.CloseMenu(player);

                                if (player != null)
                                {
                                    player.PrintToChat(localizer.LocalizeWithPrefixInternal("timeleft.prefix", timeLimitManager.GetTimeLeftMessage()));
                                }
                            });
                            break;
                    }
                }
            }
        }

        // Define WasdMenuController class
        public class WasdMenuController(MenuFactory menuFactory, PluginState pluginState, StringLocalizer localizer, TimeLimitManager timeLimitManager, GameRules gameRules, VoteManager voteManager, MaxRoundsManager maxRoundsManager,
        AsyncVoteManager asyncVoteManager, MapMenus mapMenus, ModeMenus modeMenus, SettingMenus settingMenus, NominateMenus nominateMenus, Config config)
        {
            // Define class properties
            public IWasdMenu? MainMenu;

            // Define load method
            public void Load()
            {
                if (config.Commands.Style.Equals("wasd"))
                {
                    // Create main menu
                    MainMenu = menuFactory.WasdMenus.AssignMenu("Game Commands");

                    foreach (string _command in pluginState.Game.PlayerCommands)
                    {
                        switch (_command)
                        {
                            case "!changemap":
                                MainMenu?.Add("Change Map", (player, option) =>
                                {
                                    if (player != null && config.Votes.Enabled)
                                    {
                                        if (config.Votes.Maps)
                                        {
                                            IWasdMenu? menu = mapMenus.WasdMenus.VoteMenu;

                                            if (menu != null)
                                            {
                                                menu.Prev = option.Parent?.Options?.Find(option);
                                                menuFactory.WasdMenus.OpenSubMenu(player, menu);
                                            }
                                        }
                                    }
                                });
                                break;
                            case "!changemode":
                                MainMenu?.Add("Change Mode", (player, option) =>
                                {
                                    if (player != null && config.Votes.Enabled && config.Votes.GameModes)
                                    {
                                        IWasdMenu? menu = modeMenus.WasdMenus.VoteMenu;

                                        if (menu != null)
                                        {
                                            menu.Prev = option.Parent?.Options?.Find(option);
                                            menuFactory.WasdMenus.OpenMenu(player, menu);
                                        }
                                    }
                                });
                                break;
                            case "!changesetting":
                                MainMenu?.Add("Change Setting", (player, option) =>
                                {
                                    if (player != null && config.Votes.Enabled && config.Votes.GameSettings)
                                    {
                                        IWasdMenu? menu = settingMenus.WasdMenus.VoteMenu;

                                        if (menu != null)
                                        {
                                            menu.Prev = option.Parent?.Options?.Find(option);
                                            menuFactory.WasdMenus.OpenSubMenu(player, menu);

                                        }
                                    }
                                });
                                break;
                            case "!currentmode":
                                MainMenu?.Add("Current Mode", (player, option) =>
                                {
                                    menuFactory.WasdMenus.CloseMenu(player);

                                    if (player != null)
                                    {
                                        player.PrintToChat(localizer.Localize("currentmode.message", pluginState.Game.CurrentMode.Name));
                                    }
                                });
                                break;
                            case "!currentmap":
                                MainMenu?.Add("Current Map", (player, option) =>
                                {
                                    menuFactory.WasdMenus.CloseMenu(player);

                                    if (player != null)
                                    {
                                        player.PrintToChat(localizer.Localize("currentmap.message", pluginState.Game.CurrentMap.DisplayName));
                                    }
                                });
                                break;
                            case "!timeleft":
                                MainMenu?.Add("Time Left", (player, option) =>
                                {
                                    menuFactory.WasdMenus.CloseMenu(player);

                                    if (player != null)
                                    {
                                        player.PrintToChat(localizer.LocalizeWithPrefixInternal("timeleft.prefix", timeLimitManager.GetTimeLeftMessage()));
                                    }
                                });
                                break;
                            case "!nextmap":
                                MainMenu?.Add("Next Map", (player, option) =>
                                {
                                    menuFactory.WasdMenus.CloseMenu(player);

                                    if (player != null)
                                    {
                                        if (pluginState.RTV.NextMap != null && pluginState.RTV.NextMode == null)
                                        {
                                            player.PrintToChat(localizer.Localize("rtv.nextmap.message", pluginState.RTV.NextMap.DisplayName));
                                        }
                                        else if (pluginState.RTV.NextMap == null && pluginState.RTV.NextMode != null)
                                        {
                                            player.PrintToChat(localizer.Localize("rtv.nextmap.message", "Random"));
                                        }
                                        else
                                        {
                                            player.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.no-vote"));
                                        }
                                    }
                                });
                                break;
                            case "!nextmode":
                                MainMenu?.Add("Next Mode", (player, option) =>
                                {
                                    menuFactory.WasdMenus.CloseMenu(player);

                                    if (player != null)
                                    {
                                        if (pluginState.RTV.NextMode != null)
                                        {
                                            player.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "rtv.nextmode.message", pluginState.RTV.NextMode.Name));
                                        }
                                        else if (pluginState.RTV.NextMap != null && pluginState.RTV.NextMode == null)
                                        {
                                            player.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "rtv.nextmode.message", pluginState.Game.CurrentMode.Name));
                                        }
                                        else
                                        {
                                            player.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.no-vote"));
                                        }
                                    }
                                });
                                break;
                            case "!rtv":
                                MainMenu?.Add("RockTheVote", (player, option) =>
                                {
                                    menuFactory.WasdMenus.CloseMenu(player);
                                    asyncVoteManager.RTVCounter(player);
                                });
                                break;
                            case "!nominate":
                                MainMenu?.Add("Nominate", (player, option) =>
                                {
                                    menuFactory.WasdMenus.CloseMenu(player);

                                    if (player != null)
                                    {
                                        if (pluginState.RTV.EofVoteHappened)
                                        {
                                            if (!timeLimitManager.UnlimitedTime())
                                            {
                                                string timeleft = voteManager.GetTimeLeft();
                                                player.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "rtv.schedule-change", timeleft));
                                            }
                                            else if (!maxRoundsManager.UnlimitedRounds)
                                            {
                                                string roundsleft = voteManager.GetRoundsLeft();
                                                player.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "rtv.schedule-change", roundsleft));
                                            }
                                            return;
                                        }

                                        if (pluginState.RTV.DisableCommands || !pluginState.RTV.NominationEnabled)
                                        {
                                            player.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.disabled"));
                                            return;
                                        }

                                        if (gameRules.WarmupRunning)
                                        {
                                            if (!config.RTV.EnabledInWarmup)
                                            {
                                                player.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.warmup"));
                                                return;
                                            }
                                        }
                                        else if (config.RTV.MinRounds > 0 && config.RTV.MinRounds > gameRules.TotalRoundsPlayed)
                                        {
                                            player!.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.minimum-rounds", config.RTV.MinRounds));
                                            return;
                                        }

                                        if (PlayerExtensions.ValidPlayerCount() < config!.RTV.MinPlayers)
                                        {
                                            player.PrintToChat(localizer.LocalizeWithPrefixInternal("rtv.prefix", "general.validation.minimum-players", config!.RTV.MinPlayers));
                                            return;
                                        }
                                        IWasdMenu? menu = nominateMenus.WasdMenus.MainMenu;

                                        if (menu != null)
                                        {
                                            menu.Prev = option.Parent?.Options?.Find(option);
                                            menuFactory.WasdMenus.OpenSubMenu(player, menu);
                                        }
                                    }
                                });
                                break;
                        }
                    }
                }
            }
        }
    }
}