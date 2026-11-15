// Included libraries
using System.Reflection;
using System.Text.Json.Nodes;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Integrations.GG1MapChooser
{
    // Define class
    // GG1's map votes and map changes: extend and round-win votes, the next map, and what's happening now
    public partial class MapChooserBridge
    {
        // GG1 has these only in its config, but reads them while running. Set from mode cfgs (css_mapvote_extend,
        // css_mapvote_auto), so a mode can differ from GG1MapChooser.json. Applied again in Initialize: the first cfgs
        // run before GG1 is loaded.
        public void SetExtendInVote(bool enabled)
        {
            _extendInVote = enabled;
            _gg1.SetOption("VoteSettings", "ExtendMapInVote", enabled);
        }

        public void SetRoundVote(bool enabled)
        {
            _roundVote = enabled;
            ApplyRoundVote();
        }

        // GG1's own vote after enough round wins. On means what GG1MapChooser.json says, and it's off on a map that ends with
        // a random mode, which has no map vote.
        private void ApplyRoundVote()
        {
            if (_roundVote == null && !RandomModeDue() && _roundVoteConfigured == null)
                return;

            _roundVoteConfigured ??= _gg1.GetOption("WinDrawSettings", "VoteDependsOnRoundWins");
            _gg1.SetOption("WinDrawSettings", "VoteDependsOnRoundWins", _roundVote != false && _roundVoteConfigured != false && !RandomModeDue());
        }

        // Changes to a map of the active pool
        public void ChangeToMap(IMap map)
        {
            Server.ExecuteCommand($"ggmap {map.Name}");
        }

        public bool SetNextMap(IMap map)
        {
            if (VoteInProgress())
                return false;

            Server.ExecuteCommand($"setnextmap {map.Name}");
            _events.RaiseNextMapSet(map.DisplayName);
            return true;
        }

        // Starts GG1's vote for the next map (changes at the end of the map), unless one ran or is running
        public bool StartMapVote()
        {
            if (VoteInProgress() || NextMapChosen() || (ModesOnly ? ActivePoolMaps < 2 : _pluginState.Game.CurrentMode.Maps.Count < 2))
                return false;

            _logger.LogInformation(ModesOnly ? "Starting GG1MapChooser end-of-map vote on modes" : "Starting GG1MapChooser end-of-map vote");
            Server.ExecuteCommand("ggmc_mapvote_start");
            return true;
        }

        // Changes to the map GG1's vote picked, or a random map from the current pool when there was no vote
        public void ChangeToNextMap()
        {
            Server.ExecuteCommand("ggmc_change_nextmap");
        }

        public void HideVotePanels(CCSPlayerController player)
        {
            _gg1.HideVotePanels(player);
        }

        public bool? MapChanging()
        {
            return _gg1.MapChanging();
        }

        public bool VoteInProgress()
        {
            return _gg1.VoteInProgress();
        }

        public bool NextMapChosen()
        {
            return _gg1.NextMapChosen();
        }

        // What GG1 changes to next, as players see it: a map's display name, or a mode's name. Null when there's none yet.
        public string? NextMapName()
        {
            string? key = _gg1.NextMap();
            return key == null ? null : DisplayName(key);
        }

        // A GG1 pool entry as players see it: a map's display name, or a mode's name (a mode entry's key is the mode's name)
        public string DisplayName(string key)
        {
            return _pluginState.Game.Maps.TryGetValue(key, out IMap? map) ? map.DisplayName : key;
        }
    }
}
