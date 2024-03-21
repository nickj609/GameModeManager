// Included libraries
using CounterStrikeSharp.API;
using System.Collections.Generic;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin
    {
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

        public class MapGroup : IEquatable<MapGroup>
        {
            public string Name { get; set; }
            public List<Map> Maps { get; set; }

            public MapGroup(string name) 
            {
                Name = name;
                Maps = new List<Map>();
            }

            public MapGroup(string name, List<Map> maps) 
            {
                Name = name;
                Maps = maps; 
            }

            public bool Equals(MapGroup? other) 
            {
                if (other == null) 
                {
                    return false;  // Handle null 
                }
                else
                {
                    return Name == other.Name && Maps.SequenceEqual(other.Maps);
                }
            }
            public void Clear()
            {
                Name = "";
                Maps = new List<Map>();
            }
        }
    }
}