// Included libraries
using System;
using System.IO;
using System.Text;
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
        private MapGroup defaultMapGroup = new MapGroup("mg_casual", defaultMaps);

        // Define current mapgroup, new map group, and next map
        public List<MapGroup> mapGroups = new List<MapGroup>();
        public MapGroup? currentMapGroup;
        public Map? currentMap;

        private void ParseMapGroups()
        {
    
            // Parse VDF to get a list of map groups
            Logger.LogInformation($"Parsing map group file {Config.MapGroupsFile}...");
            VProperty vdfObject = VdfConvert.Deserialize(File.ReadAllText(Config.MapGroupsFile));

            var mapGroupsData = vdfObject.Value.OfType<VProperty>()
                                        .Where(p => p.Key == "mapgroups")
                                        .Select(p => p.Value)
                                        .FirstOrDefault();
            if (mapGroupsData != null)
            {
                foreach (VProperty group in mapGroupsData.OfType<VProperty>()) 
                {     
                    MapGroup newMapGroup = new MapGroup(group.Key);

                    var mapsProperty = group.Value.OfType<VProperty>()
                            .Where(p => p.Key == "maps")
                            .Select(p => p.Value)
                            .FirstOrDefault();

                    if (mapsProperty != null)
                    {
                        // Add new maps to map list
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
                        Logger.LogWarning("Mapgroup found, but the 'maps' property is missing or incomplete. Setting default maps...");
                        newMapGroup.Maps = defaultMaps;
                    }

                    mapGroups.Add(newMapGroup);
                }
            }
            else
            {
                Logger.LogWarning($"The mapgroup property in gamemodes_server.txt doesn't exist. Using default map group...");
                mapGroups.Add(defaultMapGroup);
            }

            // Set default map group from configuration file. If not found, use plugin default.
            defaultMapGroup = mapGroups.FirstOrDefault(g => g.Name == $"{Config.MapGroup}") ?? new MapGroup("mg_casual", defaultMaps);
            UpdateMapList(defaultMapGroup);

            // Setup mode admin menu
            SetupModeMenu();
        }

        // Define function to update map list
        private void UpdateMapList(MapGroup newMapGroup)
        {  
            // Update map list
            try 
            {
                using (StreamWriter writer = new StreamWriter(Config.MapListFile))
                {
                    foreach (Map map in newMapGroup.Maps)  
                    {
                        if (string.IsNullOrEmpty(map.WorkshopId))
                        {
                            writer.WriteLine(map.Name);
                        }
                        else
                        {
                            if(Config.DefaultMapFormat)
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

            if(Config.RTVEnabled)
            {
                Logger.LogInformation("Reloading RTV plugin...");
                Server.ExecuteCommand($"css_plugins reload {Config.RTVPlugin}");
            }

            // Update map menu
            try
            {
                UpdateMapMenu(newMapGroup);
            }
            catch(Exception ex)
            {
                Logger.LogError($"{ex.Message}");
            }

            return;
        }

        // Define function to change map
        private void ChangeMap(Map nextMap)
        {
            Logger.LogInformation("Changing map...");

            // If map valid, change map. 
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

            currentMap = nextMap;
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
                        Logger.LogWarning("Map not found when updating map menu. Using de_dust2 for next map..."); 
                        nextMap = new Map("de_dust2");
                    }

                    // Close menu
                    MenuManager.CloseActiveMenu(player);

                    // Change map
                    ChangeMap(nextMap);
                });
            }

            return;
        }

        // Create mode menu
        private void SetupModeMenu()
        {
            modeMenu = new CenterHtmlMenu("Game Mode List");

            if (Config.ListEnabled)
            {
                foreach (string mode in Config.GameModeList)
                {
                    modeMenu.AddMenuOption(mode, (player, option) =>
                    {
                        Server.ExecuteCommand($"exec {option.Text}.cfg");

                        // Close menu
                        MenuManager.CloseActiveMenu(player);
                    });
                }
                return;
            }
            else
            {
                // Create menu options for each map in the maplist
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
                            Server.ExecuteCommand($"exec {option.Text}.cfg");

                            // Close menu
                            MenuManager.CloseActiveMenu(player);

                        });
                    }
                }
                return;
            }
        }
    }
}