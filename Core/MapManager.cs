// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Core.Attributes.Registration;

// Declare namespace
namespace GameModeManager
{
    // Define Map class
    public class Map : IEquatable<Map>
    {
        public string Name { get; set; }
        public string WorkshopId { get; set; }

        public Map(string name)
        {
            Name = name;
            WorkshopId = "";
        }
        
        public Map(string name, string workshopId)
        {
            Name = name;
            WorkshopId = workshopId;
        }

        public bool Equals(Map? other) 
        {
            if (other == null) return false;  // Handle null 

            // Implement your equality logic, e.g.;
            return Name == other.Name && WorkshopId == other.WorkshopId;
        }

        public void Clear()
        {
            Name = "";
            WorkshopId = "";
        }
    }

    // Plugin class
    public partial class Plugin : BasePlugin
    {
        // Define current map and map list
        public static Map? CurrentMap;     
        public static List<Map> Maps = new List<Map>();
       
        // Define function to update map list
        private void UpdateMapList(MapGroup _group)
        {  
            // If using RTV Plugin
            if(Config.RTV.Enabled)
            {
                // Update map list for RTV Plugin
                try 
                {
                    using (StreamWriter writer = new StreamWriter(Config.RTV.MapListFile))
                    {
                        foreach (Map _map in _group.Maps)  
                        {
                            if (string.IsNullOrEmpty(_map.WorkshopId))
                            {
                                writer.WriteLine(_map.Name);
                            }
                            else
                            {
                                if(Config.RTV.DefaultMapFormat)
                                {
                                    writer.WriteLine($"ws:{_map.WorkshopId}");
                                }
                                else
                                {
                                    writer.WriteLine($"{_map.Name}:{_map.WorkshopId}");
                                }
                            }
                        } 
                    } 
                } 
                catch (IOException ex)
                {
                    Logger.LogError("Could not update map list.");
                    Logger.LogError($"{ex.Message}");
                }

                // Reload RTV Plugin
                Server.ExecuteCommand($"css_plugins reload {Config.RTV.Plugin}");
            }
            // Update map list for map menu
            try
            {
                UpdateMapMenu(_group);
            }
            catch(Exception ex)
            {
                Logger.LogError($"{ex.Message}");
            }
        }

        // Create map menu
        private static CenterHtmlMenu _mapMenu = new CenterHtmlMenu("Map List");

        // Define update map menu function
        private void UpdateMapMenu(MapGroup _mapGroup)
        {
            _mapMenu = new CenterHtmlMenu("Map List");

            // Create menu options for each map in the new map list
            foreach (Map _map in _mapGroup.Maps)
            {
                _mapMenu.AddMenuOption(_map.Name, (player, option) =>
                {
                    Map? _nextMap = _map;

                    if (_nextMap == null)
                    {
                        Logger.LogWarning("Map not found when updating map menu. Using de_dust2 for next map."); 
                        _nextMap = new Map("de_dust2");
                    }
                    // Write to chat
                    Server.PrintToChatAll(Localizer["changemap.message", player.PlayerName, _nextMap.Name]);

                    // Change map
                    AddTimer(Config.MapGroup.Delay, () => ChangeMap(_nextMap));

                    // Close menu
                    MenuManager.CloseActiveMenu(player);
                });
            }
        }

        // Define change map function
        private void ChangeMap(Map _nextMap)
        {
            // If map valid, change map based on map type
            if (Server.IsMapValid(_nextMap.Name))
            {
                Server.ExecuteCommand($"changelevel \"{_nextMap.Name}\"");
            }
            else if (_nextMap.WorkshopId != null)
            {
                Server.ExecuteCommand($"host_workshop_map \"{_nextMap.WorkshopId}\"");
            }
            else
            {
                Server.ExecuteCommand($"ds_workshop_changelevel \"{_nextMap.Name}\"");
            }

            // Set current map
            CurrentMap = _nextMap;
        }

        // Construct EventGameEnd Handler to automatically change map at game end
        int _counter = 0;
        private HookResult EventGameEnd(EventCsIntermission @event, GameEventInfo info)
        {
            Logger.LogInformation("Game has ended. Picking random map from current map group...");
            Server.PrintToChatAll(Localizer["plugin.prefix"] + " Game has ended. Changing map...");

            if(!Config.RTV.Enabled)
            {
                if(CurrentMapGroup == null)
                {
                    CurrentMapGroup = _defaultMapGroup;
                }         

                // Use the random map ID in the server command. If divisible by x change mode
                if(Config.GameMode.Rotation && (float)_counter % Config.GameMode.Interval == 0)
                {  
                    // Get random game mode
                    Random _rnd = new Random();
                    int _randomIndex = _rnd.Next(0, MapGroups.Count); 
                    MapGroup _randomMode = MapGroups[_randomIndex];

                    // Change mode
                    Server.ExecuteCommand($"exec {_randomMode.Name}.cfg");
                }
                else
                {
                    // Get a random map
                    Random _rnd = new Random();
                    int _randomIndex = _rnd.Next(0, CurrentMapGroup.Maps.Count); 
                    Map _randomMap = CurrentMapGroup.Maps[_randomIndex];

                    // Change map
                    ChangeMap(_randomMap);
                }
            }
            _counter++;
            return HookResult.Continue;
        }
    
        // Construct change map command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "[map name] optional: [id]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_map", "Changes the map to the map specified in the command argument.")]
        public void OnMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null)
            {
                Map _newMap = new Map($"{command.ArgByIndex(1)}",$"{command.ArgByIndex(2)}");
                Map? _foundMap = Maps.FirstOrDefault(g => g.Name == $"{command.ArgByIndex(1)}");

                if (_foundMap != null)
                {
                    _newMap = _foundMap; 
                }
                // Write to chat
                Server.PrintToChatAll(Localizer["changemap.message", player.PlayerName, _newMap.Name]);
                // Change map
                AddTimer(5.0f, () => ChangeMap(_newMap));
            }
        }

        // Construct admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        [ConsoleCommand("css_maps", "Provides a list of maps for the current game mode.")]
        public void OnMapsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _plugin != null)
            {
                _mapMenu.Title = Localizer["maps.hud.menu-title"];
                MenuManager.OpenCenterHtmlMenu(_plugin, player, _mapMenu);
            }
        }
    }
}