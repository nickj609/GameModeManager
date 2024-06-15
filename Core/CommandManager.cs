// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin
    {
        // Construct server map group command handler
        [ConsoleCommand("css_mapgroup", "Sets the current mapgroup.")]
        [CommandHelper(minArgs: 1, usage: "mg_active", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnMapGroupCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) 
            {
                // Get map group
                MapGroup? _mapGroup = MapGroups.FirstOrDefault(g => g.Name == $"{command.ArgByIndex(1)}");

                if (_mapGroup == null || _mapGroup.Name == null || _mapGroup.Maps == null)
                {
                    command.ReplyToCommand($"Cannot find map group: {command.ArgByIndex(1)}");
                }
                else
                {
                    Logger.LogInformation($"Current map group is {CurrentMapGroup.Name}.");
                    Logger.LogInformation($"New map group is {_mapGroup.Name}.");

                    // Update map list and map menu
                    try
                    {
                        UpdateMapList(_mapGroup);
                    }
                    catch(Exception ex)
                    {
                        Logger.LogError($"{ex.Message}");
                    }

                    // Deregister map votes from old map group
                    try
                    {
                        DeregisterMapVotes();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"{ex.Message}");
                    } 

                    // Set new map group
                    CurrentMapGroup = _mapGroup;

                    // Register map votes for new map group
                    try
                    {
                        RegisterMapVotes();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"{ex.Message}");
                    } 
                }
            }
        }

        // Construct server rtv command handler
        [ConsoleCommand("css_rtv_enabled", "Enables or disables RTV.")]
        [CommandHelper(minArgs: 1, usage: "true|false", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnRTVCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) 
            {
               if (command.ArgByIndex(1).ToLower() == "true" && _RTV == false)
               {
                    Logger.LogInformation($"Enabling RTV...");
                    Server.ExecuteCommand($"css_plugins load {Config.RTV.Plugin}");

                    Logger.LogInformation($"Disabling game mode and map rotations...");
                    _RTV = true;
               }
               else if (command.ArgByIndex(1).ToLower() == "false" && _RTV == true)
               {
                
                    Logger.LogInformation($"Disabling RTV...");
                    Server.ExecuteCommand($"css_plugins unload {Config.RTV.Plugin}");

                    Logger.LogInformation($"Enabling game mode and map rotations...");
                    _RTV = false;
               }
               else
               {
                    command.ReplyToCommand($"Unexpected argument: {command.ArgByIndex(1)}");
               }
            }
        }

        // Construct admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_maps", "Provides a list of maps from the current game mode.")]
        public void OnMapsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null && MapMenu != null)
            {
                MapMenu.Title = Localizer["maps.menu-title"];
                OpenMenu(MapMenu, Config.GameMode.Style, player);
            }
            else
            {
                Console.Error.WriteLine("css_maps is a client only command.");
            }
        }

        // Construct admin change map command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "[map name] optional: [id]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_map", "Changes the map to the map specified in the command argument.")]
        public void OnMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null)
            {
                // Find map
                Map _newMap = new Map($"{command.ArgByIndex(1)}",$"{command.ArgByIndex(2)}");
                Map? _foundMap = Maps.FirstOrDefault(g => g.Name == $"{command.ArgByIndex(1)}");

                if (_foundMap != null)
                {
                    // Assign map
                    _newMap = _foundMap; 
                }

                // Write to chat
                Server.PrintToChatAll(Localizer["plugin.prefix"] + " " + Localizer["changemap.message", player.PlayerName, _newMap.Name]);

                // Change map
                AddTimer(Config.MapGroup.Delay, () => ChangeMap(_newMap));
            }
            else
            {
                Console.Error.WriteLine("css_map is a client only command.");
            }
        }

        // Construct admin change mode command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "[mode]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_mode", "Changes the game mode to the mode specified in the command argument.")]
        public void OnModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null)
            {
                string? _option = null;
                MapGroup? _mapGroup = MapGroups?.FirstOrDefault(g => g.Name == $"{command.ArgByIndex(1)}");
                KeyValuePair<string, string>? _mode = Config.GameMode.List?.FirstOrDefault(m => m.Key == $"{command.ArgByIndex(1)}");

                if (Config.GameMode.ListEnabled != true)
                {
                    _mapGroup = MapGroups?.FirstOrDefault(g => g.Name == $"{command.ArgByIndex(1)}");

                    if (_mapGroup != null && _mapGroup.Name != null)
                    {
                        _option = _mapGroup.Name;
                    }
                }
                else
                {
                    _mode = Config.GameMode.List?.FirstOrDefault(m => m.Key == $"{command.ArgByIndex(1)}");

                    if (_mode != null && _mode is KeyValuePair<string, string> kvp)
                    {
                        _option = kvp.Key;
                    }
                }

                // Create message
                string _message = Localizer["plugin.prefix"] + " " + Localizer["changemode.message", player.PlayerName, command.ArgByIndex(1)];

                // Write to chat
                Server.PrintToChatAll(_message);

                // Change game mode
                if (_option != null)
                {
                    _option = _option.ToLower();
                    AddTimer(Config.GameMode.Delay, () => Server.ExecuteCommand($"exec {_option}.cfg"));
                }
                else
                {
                    command.ReplyToCommand($"Can't find mode: {command.ArgByIndex(1)}");
                }
            }
            else
            {
                Console.Error.WriteLine("css_mode is a client only command.");
            }
        }

        // Construct admin mode menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_modes", "Provides a list of game modes.")]
        public void OnModesCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null && ModeMenu != null)
            {
                ModeMenu.Title = Localizer["modes.menu-title"];
                OpenMenu(ModeMenu, Config.GameMode.Style, player);
            }
            else
            {
                Console.Error.WriteLine("css_modes is a client only command.");
            }
        }

        // Construct admin change setting command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 2, usage: "[enable/disable] [setting name]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_setting", "Changes the game setting specified.")]
        public void OnSettingCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null)
            {
                // Get args
                string _status = $"{command.ArgByIndex(1).ToLower()}";
                string _settingName = $"{command.ArgByIndex(2)}";

                // Find game setting
                Setting? _option = Settings.FirstOrDefault(s => s.Name == _settingName);

                if(_option != null) 
                {
                    if (_status == "enable")
                    {
                        // Create message
                        string _message = Localizer["plugin.prefix"] + " " + Localizer["enable.changesetting.message", player.PlayerName, _settingName];

                        // Write to chat
                        Server.PrintToChatAll(_message);

                        // Change game setting
                        Server.ExecuteCommand($"exec {Config.Settings.Folder}/{_option.Enable}");
                    }
                    else if (_status == "disable")
                    {
                        // Create message
                        string _message = Localizer["plugin.prefix"] + " " + Localizer["disable.changesetting.message", player.PlayerName, _settingName];

                        // Write to chat
                        Server.PrintToChatAll(_message);

                        // Change game setting
                        Server.ExecuteCommand($"exec {Config.Settings.Folder}/{_option.Disable}");
                    }
                    else
                    {
                        command.ReplyToCommand($"Unexpected argument: {_status}");
                    }  
                }
                else
                {
                    command.ReplyToCommand($"Can't find setting: {_settingName}");
                }
            }
            else
            {
                Console.Error.WriteLine("css_settings is a client only command.");
            }
        }

        // Construct admin setting menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_settings", "Provides a list of game settings.")]
        public void OnSettingsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null && SettingsMenu != null)
            {
                // Open menu
                SettingsMenu.Title = Localizer["settings.menu-actions"];
                OpenMenu(SettingsMenu, Config.Settings.Style, player);
            }
            else
            {
                Console.Error.WriteLine("css_settings is a client only command.");
            }
        }

        // Construct show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_showmaps", "Provides a list of maps from current mode.")]
        public void OnShowMapsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null && ShowMapsMenu != null)
            {
                // Open menu
                ShowMapsMenu.Title = Localizer["maps.menu-title"];
                OpenMenu(ShowMapsMenu, Config.GameMode.Style, player);
            }
            else
            {
                Console.Error.WriteLine("css_showmaps is a client only command.");
            }
            
        }

        // Construct show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_showmodes", "Provides a list of game modes.")]
        public void OnShowModesCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null && ShowModesMenu != null)
            {
                // Open menu
                ShowModesMenu.Title = Localizer["modes.menu-title"];
                OpenMenu(ShowModesMenu, Config.GameMode.Style, player);
            }
            else
            {
                Console.Error.WriteLine("css_showmodes is a client only command.");
            }  
        }

        // Construct show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_showsettings", "Provides a list of game settings.")]
        public void OnShowSettingsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null && ShowSettingsMenu != null)
            {
                // Open menu
                ShowSettingsMenu.Title = Localizer["settings.menu-title"];
                OpenMenu(ShowSettingsMenu, Config.Settings.Style, player);
            }
            else
            {
                Console.Error.WriteLine("css_showsettings is a client only command.");
            }
        }
    }
}