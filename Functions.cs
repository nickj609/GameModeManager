// Included libraries
using System;
using System.IO;
using System.Text;
using System.Timers;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using CounterStrikeSharp.API;
using System.Collections.Generic;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin
    {
        // Define default maps and map group
        private static List<Map> defaultMaps = new List<Map>()
        {
            new Map("de_ancient"),
            new Map("de_anubis"),
            new Map("de_inferno"),
            new Map("de_mirage"),
            new Map("de_nuke"),
            new Map("de_overpass"),
            new Map("de_vertigo"),
            new Map("ar_shoots"),
            new Map("ar_baggage"),
            new Map("de_safehouse"),
            new Map("de_lake")
        };
        private static MapGroup defaultMapGroup = new MapGroup("mg_active", defaultMaps);

        // Define current map group, current map, and map group list
        public static List<MapGroup> mapGroups = new List<MapGroup>();
        public static MapGroup currentMapGroup = defaultMapGroup;
        public static Map? currentMap;

        private void ParseMapGroups()
        {
            Logger.LogInformation($"Parsing map group file {Config.MapGroup.File}.");
            try
            {
                // Deserialize gamemodes_server.txt (VDF) to VProperty
                VProperty vdfObject = VdfConvert.Deserialize(File.ReadAllText(Config.MapGroup.File, Encoding.UTF8));
                
                if (vdfObject == null)
                {
                    Logger.LogError($"Error when parsing gamemodes_server.txt.");
                }
                else
                {
                    // Create an array of only map groups
                    var mapGroupsData = vdfObject.Value.OfType<VProperty>()
                                            .Where(p => p.Key == "mapgroups")
                                            .Select(p => p.Value)
                                            .FirstOrDefault();

                    // Parse array to populate map group list               
                    if (mapGroupsData != null)
                    {
                        foreach (VProperty group in mapGroupsData.OfType<VProperty>()) 
                        {  
                            // Create map group
                            MapGroup newMapGroup = new MapGroup(group.Key);

                            // Create an array of maps
                            var mapsProperty = group.Value.OfType<VProperty>()
                                    .Where(p => p.Key == "maps")
                                    .Select(p => p.Value)
                                    .FirstOrDefault();

                            // Parse array to add maps to map group
                            if (mapsProperty != null)
                            {
                                foreach (VProperty mapEntry in mapsProperty)
                                {
                                    string mapName = mapEntry.Key;

                                    if (mapName.StartsWith("workshop/"))
                                    {
                                        string[] parts = mapName.Split('/');
                                        string mapNameFormatted = parts[parts.Length - 1];
                                        string mapWorkshopId = parts[1]; 
                                        newMapGroup.Maps.Add(new Map(mapNameFormatted, mapWorkshopId));
                                    }
                                    else
                                    {
                                        newMapGroup.Maps.Add(new Map(mapName));
                                    }
                                }
                            }
                            else
                            {
                                Logger.LogWarning("Mapgroup found, but the 'maps' property is missing or incomplete. Setting default maps.");
                                newMapGroup.Maps = defaultMaps;
                            }
                            // Add map group to map group list
                            mapGroups.Add(newMapGroup);
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"The mapgroup property doesn't exist. Using default map group.");
                        mapGroups.Add(defaultMapGroup);
                    }
                }
                // Set default map group from configuration file. If not found, use plugin default.
                defaultMapGroup = mapGroups.FirstOrDefault(g => g.Name == $"{Config.MapGroup}") ?? new MapGroup("mg_active", defaultMaps);
                currentMapGroup = defaultMapGroup;

                // Update map list
                UpdateMapList(defaultMapGroup);
            }
            catch (Exception ex)
            {
                Logger.LogError($"{ex.Message}");
            }
        }
        // Define function to update map list
        private void UpdateMapList(MapGroup newMapGroup)
        {  
            // If using RTV Plugin
            if(Config.RTV.Enabled)
            {
                // Update map list for RTV Plugin
                Logger.LogInformation("Updating map list.");
                try 
                {
                    using (StreamWriter writer = new StreamWriter(Config.RTV.MapListFile))
                    {
                        foreach (Map map in newMapGroup.Maps)  
                        {
                            if (string.IsNullOrEmpty(map.WorkshopId))
                            {
                                writer.WriteLine(map.Name);
                            }
                            else
                            {
                                if(Config.RTV.DefaultMapFormat)
                                {
                                    writer.WriteLine($"ws:{map.WorkshopId}");
                                }
                                else
                                {
                                    writer.WriteLine($"{map.Name}:{map.WorkshopId}");
                                }
                            }
                        }
                    } 
                } 
                catch (IOException ex)
                {
                    Logger.LogError("Unable to update maplist.txt.");
                    Logger.LogError($"{ex.Message}");
                    throw;
                }

                // Reload RTV Plugin
                Logger.LogInformation("Reloading RTV plugin.");
                Server.ExecuteCommand($"css_plugins reload {Config.RTV.Plugin}");
            }
            // Update map menu
            try
            {
                Logger.LogInformation("Updating map menu.");
                UpdateMapMenu(newMapGroup);
            }
            catch(Exception ex)
            {
                Logger.LogError($"{ex.Message}");
            }
            return;
        }

        // Define menus
        private CenterHtmlMenu mapMenu = new CenterHtmlMenu("Map List");
        private CenterHtmlMenu modeMenu = new CenterHtmlMenu("Game Mode List");

        // Create map menu 
        private void UpdateMapMenu(MapGroup newMapGroup)
        {
            mapMenu = new CenterHtmlMenu("Map List");

            // Create menu options for each map in the maplist
            foreach (Map map in newMapGroup.Maps)
            {
                mapMenu.AddMenuOption(map.Name, (player, option) =>
                {
                    Map? nextMap = map;

                    if (nextMap == null)
                    {
                        Logger.LogWarning("Map not found when updating map menu. Using de_dust2 for next map."); 
                        nextMap = new Map("de_dust2");
                    }
                    // Close menu
                    MenuManager.CloseActiveMenu(player);
                    // Write to chat
                    Server.PrintToChatAll((player.PlayerName ?? "Server") + " changed the map to" + nextMap.Name);
                    // Change map
                    ChangeMap(nextMap);
                });
            }
            return;
        }
        // Create mode menu
        private void SetupModeMenu()
        {
            Logger.LogInformation("Creating mode menu.");
            modeMenu = new CenterHtmlMenu("Game Mode List");

            if (Config.GameMode.ListEnabled)
            {
                // Add menu option for each game mode in game mode list
                foreach (string mode in Config.GameMode.List)
                {
                    modeMenu.AddMenuOption(mode, (player, option) =>
                    {
                        // Execute game mode config and close menu
                        Server.ExecuteCommand($"exec {option.Text}.cfg");
                        MenuManager.CloseActiveMenu(player);
                    });
                }
                return;
            }
            else
            {
                // Create menu options for each map group parsed
                foreach (MapGroup mapGroup in mapGroups)
                {
                    string mapGroupName;

                    if(mapGroup.Name == null)
                    {   
                        mapGroupName = defaultMapGroup.Name; 
                    }
                    else
                    {
                        mapGroupName = mapGroup.Name;
                    }
                    if(mapGroupName != null)
                    {
                        modeMenu.AddMenuOption(mapGroupName, (player, option) =>
                        {
                            // Execute game mode config and close menu
                            Server.ExecuteCommand($"exec {option.Text}.cfg");
                            MenuManager.CloseActiveMenu(player);
                        });
                    }
                }
                return;
            }
        }
        // Define function to change map
        private void ChangeMap(Map nextMap)
        {
            Logger.LogInformation("Changing map...");

            // Create map change delay
            System.Timers.Timer delayTimer = new System.Timers.Timer();
            delayTimer.Interval = 5000; // Example: 5-second delay
            delayTimer.Elapsed += (sender, e) => 
            {
                delayTimer.Stop(); // Stop the timer after one execution 
                
                // If map valid, change map based on map type
                if (Server.IsMapValid(nextMap.Name))
                {
                    Server.ExecuteCommand($"changelevel \"{nextMap.Name}\"");
                }
                else if (nextMap.WorkshopId != null)
                {
                    Server.ExecuteCommand($"host_workshop_map \"{nextMap.WorkshopId}\"");
                }
                else
                {
                    Server.ExecuteCommand($"ds_workshop_changelevel \"{nextMap.Name}\"");
                }
            };
            delayTimer.Start();

            // Set current map
            currentMap = nextMap;
            return;
        }
        private HookResult EventGameEnd(EventCsIntermission @event, GameEventInfo info)
        {
            Console.WriteLine("Game has ended. Changing map...");

            if(currentMapGroup == null)
            {
                currentMapGroup = defaultMapGroup;
            }
             
            // Get a random map
            Random rnd = new Random();
            int randomIndex = rnd.Next(0, currentMapGroup.Maps.Count); 
            Map randomMap = currentMapGroup.Maps[randomIndex];

            // Use the random map ID in the server command
            ChangeMap(randomMap);

            return HookResult.Continue;
        }
    }
}