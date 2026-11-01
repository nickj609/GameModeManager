// Included libraries
using System.Reflection;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    // The members GameModeManager reaches through reflection in plugins it bundles. css_gmm_status checks them on the server, and
    // .github/scripts/check-integrations.ps1 reads these lists to check them when a bundled plugin updates. Add a member here when
    // starting to use it.
    public static class ReflectedMembers
    {
        // Define class properties
        public static readonly string[] GG1MapChooser =
        {
            "_roundEndMap", "nominatedMaps", "players", "selectedMaps", "ProposedMaps", "_playedMaps", "_recentlyNominatedMaps",
            "RememberNominatedMaps", "MapsInVote", "AllowNominate", "MinPlayers", "MaxPlayers", "MapMenuCommand", "Nominate",
            "TryNominate", "BeginMapVote", "GetVoteMenuMode", "VotingTime", "ReloadMapcycle", "GetActiveMapPool", "GetRealClientCount",
            "ExtendMapInVote", "VoteDependsOnRoundWins", "_mapChooser", "GGMC_IsVoteInProgress", "MCAPICapability", "MapIsChanging",
            "VoteSettings", "WinDrawSettings"
        };

        public static readonly string[] PMM_GG1MapChooser =
        {
            "Instance", "Maps", "Title", "Badge", "HideVoteFor", "PClass", "HudForgetSlot", "_vote", "Viewers"
        };

        // Define class methods
        public static List<string> Missing(Assembly assembly, IEnumerable<string> names)
        {
            Type?[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            HashSet<string> found = new();
            foreach (Type? type in types)
            {
                if (type == null)
                    continue;

                found.Add(type.Name);
                try
                {
                    found.UnionWith(type.GetMembers(Any).Select(member => member.Name));
                }
                catch (Exception ex) when (ex is TypeLoadException or FileNotFoundException)
                {
                    // A member's type isn't loaded: its name can't be read, so it's reported as missing
                }
            }
            return names.Where(name => !found.Contains(name)).ToList();
        }
    }
}
