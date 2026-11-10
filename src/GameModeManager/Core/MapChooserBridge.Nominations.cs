// Included libraries
using System.Reflection;
using System.Text.Json.Nodes;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    // Nominations: GG1's, and the limit of one nominated mode
    public partial class MapChooserBridge
    {
        public void ShowWhyNominationsClosed(CCSPlayerController player)
        {
            _gg1.ShowWhyNominationsClosed(player);
        }

        public bool NominationsOpen()
        {
            return _gg1.NominationsOpen();
        }

        // Another player's nominated mode. The player's own can be swapped, because a new nomination replaces it.
        public bool OtherModeNominated(CCSPlayerController player)
        {
            string own = Proposal(player);
            return Nominated().Any(map => IsModeEntry(map) && map != own);
        }

        public IReadOnlyList<string> Nominated()
        {
            return _gg1.NominatedMaps() ?? new List<string>();
        }

        // The player's own nomination, "" when they have none
        public string Proposal(CCSPlayerController player)
        {
            return _gg1.Proposal(player);
        }

        // A mode that isn't in GG1's pool yet is added when it's picked, so only entries already there can be refused
        public bool CanBeNominated(string key)
        {
            return _gg1.AllowsNomination(key) ?? IsModeEntry(key);
        }

        // Runs every tick, so it does nothing until there are two nominations: only then can one be a second mode. The list
        // is found once per map and GG1 keeps the same one, so there's no reflection here after that.
        public bool HasExtraMode()
        {
            if (!_watchFound)
            {
                _watchFound = true;
                _watchedNominations = _gg1.NominatedMaps();
            }

            var nominated = _watchedNominations;
            if (nominated == null || nominated.Count < 2)
                return false;

            int modes = 0;
            foreach (string map in nominated)
            {
                if (_modeNames.Contains(map) && ++modes > 1)
                    return true;
            }
            return false;
        }

        // Keeps the first nominated mode and takes back any others, returning the players who lost theirs. The menus already
        // refuse a second mode; this catches GG1's other ways in, like its panel from a chat line starting with "yd".
        public List<int> RemoveExtraModes()
        {
            var extras = _gg1.NominatedMaps()?.Where(IsModeEntry).Skip(1).ToList();
            if (extras == null || extras.Count == 0)
                return new List<int>();

            return _gg1.RemoveNominations(extras);
        }

        public void NominateMode(CCSPlayerController player, IMode mode)
        {
            Nominate(player, EnsureModeEntry(mode));
        }

        public void Nominate(CCSPlayerController player, string key)
        {
            _gg1.Nominate(player, key);
        }
    }
}
