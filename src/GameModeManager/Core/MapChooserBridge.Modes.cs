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
    // Which modes rotation, votes and random picks offer: rotation timing, player limits, recently played modes, the next mode
    public partial class MapChooserBridge
    {
        // Whether this map's end changes the mode. MapRotations only moves at the end of a map, so it's known all map long.
        public bool ModeChangeDue()
        {
            RotationSettings rotation = _config.Rotation;
            return _pluginState.Game.RotationsEnabled && rotation.ModeRotation && _pluginState.Game.MapRotations != 0
                && _pluginState.Game.MapRotations % rotation.ModeInterval == 0 && RotationModes().Count > 0;
        }

        // The modes a rotation can change to: not the current one, not one without maps, and not one set to InRotation: false
        public List<IMode> RotationModes()
        {
            return _pluginState.Game.Modes.Values.Where(mode => !mode.Equals(_pluginState.Game.CurrentMode) && mode.Maps.Count > 0
                && _config.GameModes.InRotation(mode.Name)).ToList();
        }

        // ModeVote "random" on the map where the mode changes: no map vote, a random mode at the end
        public bool RandomModeDue() => ModeChangeDue() && !_config.Rotation.VoteForMode;

        public bool CanBeNextMode(IMode mode)
        {
            return _pools.ContainsKey(mode.Name) && !mode.Equals(_pluginState.Game.CurrentMode);
        }

        // Modes set to InVotes: false stay out of what players and random picks can choose. An admin can still pick them by name.
        public bool InVotes(IMode mode)
        {
            return _config.GameModes.InVotes(mode.Name);
        }

        // The mode's MinPlayers and MaxPlayers allow the players here now (not bots or spectating HLTV)
        public bool FitsPlayers(IMode mode)
        {
            ModeEntry? entry = _config.GameModes.Entry(mode.Name);
            if (entry == null || (entry.MinPlayers <= 0 && entry.MaxPlayers <= 0))
                return true;

            int players = PlayerExtensions.ValidPlayerCount(false);
            return (entry.MinPlayers <= 0 || players >= entry.MinPlayers) && (entry.MaxPlayers <= 0 || players <= entry.MaxPlayers);
        }

        // "4-10", "4+" or "1-8": the players a mode is for, for messages
        public string PlayerRange(IMode mode)
        {
            ModeEntry? entry = _config.GameModes.Entry(mode.Name);
            int min = Math.Max(1, entry?.MinPlayers ?? 0);
            int max = entry?.MaxPlayers ?? 0;
            return max > 0 ? $"{min}-{max}" : $"{min}+";
        }

        // One of the last Rotation.RecentModes modes played before this one
        public bool RecentlyPlayed(IMode mode)
        {
            return _pluginState.Game.RecentModes.Take(_config.Rotation.RecentModes).Contains(mode.Name, StringComparer.OrdinalIgnoreCase);
        }

        // The modes not played recently, when at least this many are left. Else all of them, so there's always a choice.
        public List<IMode> Fresh(List<IMode> modes, int atLeast)
        {
            List<IMode> fresh = modes.Where(mode => !RecentlyPlayed(mode)).ToList();
            return fresh.Count >= atLeast ? fresh : modes;
        }

        // A random mode for a rotation or an admin's random pick: one for this many players if there is one, and not one played
        // recently if there is one. Choosing a mode by name ignores player limits. Null when the list is empty.
        public IMode? PickRandom(List<IMode> modes)
        {
            List<IMode> fitting = modes.Where(FitsPlayers).ToList();
            List<IMode> candidates = Fresh(fitting.Count > 0 ? fitting : modes, 1);
            return candidates.Count > 0 ? candidates[Random.Shared.Next(candidates.Count)] : null;
        }

        // Sets a mode as GG1's next map, the same result as a mode winning a vote
        public bool SetNextMode(IMode mode)
        {
            if (VoteInProgress() || !CanBeNextMode(mode))
                return false;

            Server.ExecuteCommand($"setnextmap {EnsureModeEntry(mode)}");
            _events.RaiseNextMapSet(mode.Name);
            return true;
        }

        // A mode picked by an admin or a player may not be the map's random one, so it's added for this map. Weight 0
        // keeps GG1's random picks away from it, and the map's next pool leaves it out again. Reloads straight away, so
        // the entry can be used in the same frame. GG1 keeps its nominations through a reload.
        private string EnsureModeEntry(IMode mode)
        {
            string key = mode.Name;
            if (_activePool == null || _activePool.ContainsKey(key))
                return key;

            AddModeEntry(_activePool, _modeEntries.First(m => m.Value.Equals(mode)).Key, mode, 0);
            if (MapPools.WriteIfChanged(MapPools.MapsFile, new JsonObject { [_activeMode] = _activePool.DeepClone() }, _logger))
                _gg1.ReloadNow();
            return key;
        }

        // Starts GG1's vote on random other modes. The winner changes the mode right after the vote.
        public bool StartModeVote(CCSPlayerController player)
        {
            return _gg1.StartVote(player, size =>
            {
                // Modes for this many players, the ones not played recently first
                var fitting = _modeEntries.Values.Where(mode => CanBeNextMode(mode) && InVotes(mode) && FitsPlayers(mode)).OrderBy(_ => Random.Shared.Next()).ToList();
                var modes = fitting.OrderBy(mode => RecentlyPlayed(mode) ? 1 : 0).Take(size).ToList();
                if (modes.Count < 2 || VoteInProgress() || NextMapChosen())
                    return null;

                return modes.Select(EnsureModeEntry).ToList();
            });
        }

        // A mode's GG1 entry is keyed by its plain name, which is what GG1 shows in chat
        private bool IsModeEntry(string key)
        {
            return _modeEntries.Values.Any(mode => mode.Name == key);
        }

        // The mode a player typed after !nominate, unless a map of the current pool has that name
        public IMode? FindMode(string name)
        {
            if (_activePool != null && _activePool.ContainsKey(name) && !IsModeEntry(name))
                return null;

            return _modeEntries.Values.FirstOrDefault(mode => mode.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        // The mode behind a mode entry's mapid, null for a real workshop map
        public IMode? FindModeEntry(string mapId)
        {
            return _modeEntries.GetValueOrDefault(mapId);
        }

        public bool IsMode(string key) => IsModeEntry(key);
    }
}
