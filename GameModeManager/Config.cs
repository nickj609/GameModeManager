// Included libraries
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager
{
    // Define class dependencies
    public class ScheduleEntry
    {
        public string Time { get; set; } = "10:00"; // Time of the rotation (e.g., "10:00")
        public string Mode { get; set; } = "Casual"; // Optional: Game mode to rotate to (if specified)
    }

    public class ModeEntry
    {
        public string Name { get; set; } = "Casual";
        public string Config { get; set; } = "casual.cfg";
        public string? DefaultMap { get; set; } = null;
        public List<string> MapGroups { get; set; } = new List<string>() { "mg_active", "mg_comp" };
    }

    public class WarmupModeEntry
    {
        public string Name { get; set; } = "Deathmatch";
        public string Config { get; set; } = "warmup/dm.cfg";
    }

    // Define RTV settings
    public class RTVSettings
    {
        public bool Enabled { get; set; } = true; // Enables RTv
        public bool PerMap { get; set; } = false; // Enables per map RTV configuration
        public bool HideHud { get; set; } = false; // Hides vote results hud
        public int MinRounds { get; set; } = 1; // Minimum number of rounds for RTV
        public int MinPlayers { get; set; } = 3; // Minimum number of players for RTV
        public int VoteDuration { get; set; } = 60; // Vote duration in seconds
        public int OptionsToShow { get; set; } = 6; // Number of options to show in RTV list
        public int VotePercentage { get; set; } = 51; // Vote percentage
        public ushort OptionsInCoolDown { get; set; } = 3; // Options in cool down
        public bool EndOfMapVote { get; set; } = true; // Enables end map vote
        public bool IncludeModes { get; set; } = true; // Includes modes in RTV list
        public bool IncludeExtend { get; set; } = false; // Includes extend in RTV list
        public int MaxExtends { get; set; } = 5; // Sets the max number of map extends
        public int ExtendTime { get; set; } = 15; // Sets the time interval to extend in minutes
        public int ExtendRounds { get; set; } = 5; // Sets the number of rounds to extend
        public int ModePercentage { get; set; } = 40; // Sets percent of modes in RTV list
        public bool EnabledInWarmup { get; set; } = false; // Enables RTV in warmup
        public bool HideHudAfterVote { get; set; } = false; // Hides vote results hud after voting
        public bool NominationEnabled { get; set; } = true; // Enables nomination
        public int MaxNominationWinners { get; set; } = 1; // Sets max nomination winners per option type
        public bool ChangeImmediately { get; set; } = false; // Enables change map/mode immediately after winner is selected
        public int TriggerKillsBeforeEnd { get; set; } = 13; // Kills needed to trigger end of map vote (armsrace/gungame)
        public int TriggerRoundsBeforeEnd { get; set; } = 2; // Rounds remaining to trigger end of map vote
        public int TriggerSecondsBeforeEnd { get; set; } = 120; // Seconds remaining to trigger end of map vote
    }

    // Define map settings
    public class MapSettings
    {
        public int Mode { get; set; } = 0; // 0 for current mode maps, 1 for all maps
        public int Delay { get; set; } = 5; // Map change delay in seconds
        public string Default { get; set; } = "de_dust2"; // Default map on server start
    }

    // Define vote settings
    public class VoteSettings
    {
        public bool Enabled { get; set; } = false; // Enables CS2-CustomVotes compatibility
        public bool Maps { get; set; } = false; // Enables vote to change map
        public bool GameModes { get; set; } = false; // Enables vote to change game mode
        public bool GameSettings { get; set; } = false; // Enables vote to change game setting
    }

    // Define game settings
    public class GameSettings
    {
        public bool Enabled { get; set; } = true; // Enable game settings
        public string Folder { get; set; } = "settings"; // Default settings folder
    }

    // Define command settings
    public class CommandSettings
    {
        public bool Map { get; set; } = true; // Enables or disables !map admin command
        public bool Maps { get; set; } = true; // Enables or disables !maps admin command 
        public bool Mode { get; set; } = true; // Enables or disables !mode admin command
        public bool Modes { get; set; } = true; // Enables or disables !modes admin command 
        public bool TimeLeft { get; set; } = true; // Enables or disables !timeleft admin command
        public bool TimeLimit { get; set; } = true; // Enables or disables !timelimit admin command
    }

    public class WarmupSettings
    {
        public bool Enabled { get; set; } = true; // Enables or disables warmup
        public float Time { get; set; } = 60; // Default warmup time
        public bool PerMap { get; set; } = false; // Enables or disables per map warmup
        public WarmupModeEntry Default { get; set; } = new WarmupModeEntry() { Name = "Deathmatch", Config = $"warmup/dm.cfg" }; // Default warmup mode
        public List<WarmupModeEntry> List { get; set; } = new List<WarmupModeEntry>()
        {
            new WarmupModeEntry() { Name = "Deathmatch", Config = $"warmup/dm.cfg"},
            new WarmupModeEntry() { Name = "Knives Only", Config = $"warmup/knives_only.cfg"},
            new WarmupModeEntry() { Name = "Scoutz Only", Config = $"warmup/scoutz_only.cfg"}
        };
    }
    public class RotationSettings
    {
        public bool Enabled { get; set; } = true; // Enables game rotations
        public int Cycle { get; set; } = 0; // 0 for current mode maps, 1 for all maps, 2 for specific map groups
        public List<string> MapGroups { get; set; } = new List<string>() { "mg_active", "mg_comp" }; // Map group list for cycle 2
        public bool WhenServerEmpty { get; set; } = false; // Enables rotation on server empty. 
        public int CustomTimeLimit { get; set; } = 600; // Sets custom time limit in seconds for rotation when server empty
        public bool ModeRotation { get; set; } = false; // Enables game mode rotations
        public int ModeInterval { get; set; } = 4; // Changes mode every x map rotations
        public bool ModeSchedules { get; set; } = false; // Enables or disables mode schedules
        public List<ScheduleEntry> Schedule { get; set; } = new List<ScheduleEntry>()
        {
            new ScheduleEntry() { Time = "10:00", Mode = "Casual" },
            new ScheduleEntry() { Time = "15:00", Mode = "Practice" },
            new ScheduleEntry() { Time = "17:00", Mode = "Competitive" }
        }; // Schedule options
    }

    // Define game mode settings
    public class GameModeSettings
    {
        public ModeEntry Default { get; set; } = new ModeEntry() { Name = "Casual", Config = "casual.cfg", MapGroups = new List<string>() { "mg_active", "mg_comp" } }; // Default mode on server start
        public string MapGroupFile { get; set; } = "gamemodes_server.txt"; // Default game modes and map groups file

        public List<ModeEntry> List { get; set; } = new List<ModeEntry>()
        {
            new ModeEntry() { Name = "45", Config = "45.cfg", DefaultMap = "3276886893", MapGroups = new List<string>(){"mg_45"} },
            new ModeEntry() { Name = "1v1", Config = "1v1.cfg", DefaultMap = "3070253400", MapGroups = new List<string>(){"mg_1v1"}},
            new ModeEntry() { Name = "Armsrace", Config = "ar.cfg", DefaultMap = "ar_pool_day", MapGroups = new List<string>(){"mg_gg"}},
            new ModeEntry() { Name = "Awp", Config = "awp.cfg", DefaultMap = "3142070597", MapGroups = new List<string>(){"mg_awp"}},
            new ModeEntry() { Name = "Aim", Config = "aim.cfg", DefaultMap = "3084291314", MapGroups = new List<string>(){"mg_aim"}},
            new ModeEntry() { Name = "Battle", Config = "battle.cfg", DefaultMap = "3070253400", MapGroups = new List<string>(){"mg_battle"}},
            new ModeEntry() { Name = "Battle Royale", Config = "br.cfg", DefaultMap = "3070253400", MapGroups = new List<string>(){"mg_battleroyale"}},
            new ModeEntry() { Name = "Bhop", Config = "bhop.cfg", DefaultMap = "3088973190", MapGroups = new List<string>(){"mg_bhop"}},
            new ModeEntry() { Name = "Casual", Config = "casual.cfg", DefaultMap = "de_dust2", MapGroups = new List<string>(){"mg_active", "mg_comp"} },
            new ModeEntry() { Name = "Casual 1.6", Config = "Casual-1.6.cfg", DefaultMap = "3212419403", MapGroups = new List<string>(){"mg_Casual-1.6"} },
            new ModeEntry() { Name = "Competitive", Config = "comp.cfg", DefaultMap = "de_dust2", MapGroups = new List<string>(){"mg_active", "mg_comp"}},
            new ModeEntry() { Name = "Course", Config = "course.cfg", DefaultMap = "3070455802", MapGroups = new List<string>(){"mg_course"}},
            new ModeEntry() { Name = "Deathmatch", Config = "dm.cfg", DefaultMap = "de_mirage", MapGroups = new List<string>(){"mg_dm"}},
            new ModeEntry() { Name = "Deathmatch (Valve)", Config = "dm-valve.cfg", DefaultMap = "de_mirage", MapGroups = new List<string>(){"mg_dm"}},
            new ModeEntry() { Name = "Deathrun", Config = "deathrun.cfg", DefaultMap = "3164611860", MapGroups = new List<string>(){"mg_deathrun"}},
            new ModeEntry() { Name = "Executes", Config = "executes.cfg", DefaultMap = "de_mirage", MapGroups = new List<string>(){"mg_comp"}},
            new ModeEntry() { Name = "GG", Config = "gg.cfg", DefaultMap = "ar_pool_day", MapGroups = new List<string>(){"mg_gg"}},
            new ModeEntry() { Name = "HE Only", Config = "he.cfg", DefaultMap = "3089842427", MapGroups = new List<string>(){"mg_he"}},
            new ModeEntry() { Name = "Hide N Seek", Config = "hns.cfg", DefaultMap = "3097563690", MapGroups = new List<string>(){"mg_hns"}},
            new ModeEntry() { Name = "KreedZ", Config = "kz.cfg", DefaultMap = "3086304337", MapGroups = new List<string>(){"mg_kz"}},
            new ModeEntry() { Name = "Minigames", Config = "minigames.cfg", DefaultMap = "3082120895", MapGroups = new List<string>(){"mg_minigames"}},
            new ModeEntry() { Name = "Practice", Config = "prac.cfg", DefaultMap = "de_dust2", MapGroups = new List<string>(){"mg_comp"}},
            new ModeEntry() { Name = "Prefire", Config = "prefire.cfg", DefaultMap = "de_inferno", MapGroups = new List<string>(){"mg_comp"} },
            new ModeEntry() { Name = "Retakes", Config = "retake.cfg", DefaultMap = "de_dust2", MapGroups = new List<string>(){"mg_comp"}},
            new ModeEntry() { Name = "ScoutzKnivez", Config = "scoutzknivez.cfg", DefaultMap = "3073929825", MapGroups = new List<string>(){"mg_scoutzknivez"}},
            new ModeEntry() { Name = "Surf", Config = "surf.cfg", DefaultMap = "3082548297", MapGroups = new List<string>(){"mg_surf"}},
            new ModeEntry() { Name = "Soccer", Config = "soccer.cfg", DefaultMap = "3070198374", MapGroups = new List<string>(){"mg_soccer"}},
            new ModeEntry() { Name = "Tournament", Config = "tournament.cfg", DefaultMap = "de_dust2", MapGroups = new List<string>(){"mg_active"}},
            new ModeEntry() { Name = "Wingman", Config = "wingman.cfg", DefaultMap = "de_memento", MapGroups = new List<string>(){"mg_active", "mg_comp"}},
        };
    }

    // Define configuration class
    public class Config : IBasePluginConfig
    {
        public int Version { get; set; } = 12;
        public RTVSettings RTV { get; set; } = new();
        public MapSettings Maps { get; set; } = new();
        public VoteSettings Votes { get; set; } = new();
        public GameSettings Settings { get; set; } = new();
        public WarmupSettings Warmup { get; set; } = new();
        public CommandSettings Commands { get; set; } = new();
        public RotationSettings Rotation { get; set; } = new();
        public GameModeSettings GameModes { get; set; } = new();
    }

    // Define plugin class for parsing config
    public partial class Plugin : IPluginConfig<Config>
    {
        public required Config Config { get; set; }

        // Perform error checking
        public void OnConfigParsed(Config _config)
        {
            // Maps settings
            if (String.IsNullOrEmpty(_config.Maps.Default))
            {
                Logger.LogError("Invalid: Default map must not be empty.");
                throw new Exception("Invalid: Default map must not be empty.");
            }
            if (_config.Maps.Mode != 0 && _config.Maps.Mode != 1)
            {
                Logger.LogError("Invalid: Maps Mode must be 0 or 1.");
                throw new Exception("Invalid: Maps Mode must be 0 or 1.");
            }
            if (_config.Maps.Delay < 0)
            {
                Logger.LogError("Invalid: Maps Delay cannot be negative.");
                throw new Exception("Invalid: Maps Delay cannot be negative.");
            }

            // Game Settings
            if (_config.Settings.Enabled && !Directory.Exists(Path.Combine(PluginState.GameController.ConfigDirectory, _config.Settings.Folder)))
            {
                Logger.LogError($"Cannot find 'Settings Folder': {PluginState.GameController.SettingsDirectory}");
                throw new Exception($"Cannot find 'Settings Folder': {PluginState.GameController.SettingsDirectory}");
            }

            // Game mode settings
            if (File.Exists(Path.Join(PluginState.GameController.GameDirectory, _config.GameModes.MapGroupFile)))
            {
                _config.GameModes.MapGroupFile = Path.Join(PluginState.GameController.GameDirectory, _config.GameModes.MapGroupFile);
            }
            else
            {
                Logger.LogError($"Cannot find map group file: {_config.GameModes.MapGroupFile}");
                throw new Exception($"Cannot find map group file: {_config.GameModes.MapGroupFile}");
            }

            if (_config.GameModes.List.Count == 0)
            {
                Logger.LogError("Undefined: Game modes list cannot be empty.");
                throw new Exception("Undefined: Game modes list cannot be empty.");
            }

            if (_config.GameModes.Default == null)
            {
                Logger.LogError("Invalid: Default game mode must not be empty.");
                throw new Exception("Invalid: Default game mode must not be empty.");
            }

            // Rotation settings
            if (_config.Rotation.Cycle != 0 && _config.Rotation.Cycle != 1 && _config.Rotation.Cycle != 2)
            {
                Logger.LogError("Invalid: Rotation cycle must be 0, 1, or 2.");
                throw new Exception("Invalid: Rotation cycle must be 0, 1, or 2.");
            }

            if (_config.Rotation.ModeSchedules == true)
            {
                if (_config.Rotation.Schedule.Count <= 0)
                {
                    Logger.LogError("Invalid: Schedule cannot be empty");
                    throw new Exception("Invalid: Schedule cannot be empty");
                }
            }
            if (_config.Rotation.CustomTimeLimit < 0)
            {
                Logger.LogError("Invalid: Rotation CustomTimeLimit cannot be negative.");
                throw new Exception("Invalid: Rotation CustomTimeLimit cannot be negative.");
            }
            if (_config.Rotation.ModeInterval <= 0)
            {
                Logger.LogError("Invalid: Rotation ModeInterval must be greater than 0.");
                throw new Exception("Invalid: Rotation ModeInterval must be greater than 0.");
            }

            // RTV Settings
            if (_config.RTV.MinRounds < 0) {
                Logger.LogError("Invalid: MinRounds cannot be negative.");
                throw new Exception("Invalid: MinRounds cannot be negative.");
            }

            if (_config.RTV.MinPlayers < 0) {
                Logger.LogError("Invalid: MinPlayers cannot be negative.");
                throw new Exception("Invalid: MinPlayers cannot be negative.");
            }

            if (_config.RTV.VoteDuration <= 0) {
                Logger.LogError("Invalid: VoteDuration must be greater than 0.");
                throw new Exception("Invalid: VoteDuration must be greater than 0.");
            }

            if (_config.RTV.OptionsToShow <= 0) {
                Logger.LogError("Invalid: OptionsToShow must be greater than 0.");
                throw new Exception("Invalid: OptionsToShow must be greater than 0.");
            }

            if (_config.RTV.VotePercentage <= 0 || _config.RTV.VotePercentage > 100) {
                Logger.LogError("Invalid: VotePercentage must be between 1 and 100.");
                throw new Exception("Invalid: VotePercentage must be between 1 and 100.");
            }

            if (_config.RTV.OptionsInCoolDown < 0) {
                Logger.LogError("Invalid: OptionsInCoolDown cannot be negative.");
                throw new Exception("Invalid: OptionsInCoolDown cannot be negative.");
            }

            if (_config.RTV.MaxExtends < 0) {
                Logger.LogError("Invalid: MaxExtends cannot be negative.");
                throw new Exception("Invalid: MaxExtends cannot be negative.");
            }

            if (_config.RTV.ExtendTime <= 0) {
                Logger.LogError("Invalid: ExtendTime must be greater than 0.");
                throw new Exception("Invalid: ExtendTime must be greater than 0.");
            }

            if (_config.RTV.ExtendRounds < 0) {
                Logger.LogError("Invalid: ExtendRounds cannot be negative.");
                throw new Exception("Invalid: ExtendRounds cannot be negative.");
            }

            if (_config.RTV.ModePercentage <= 0 || _config.RTV.ModePercentage > 100) {
                Logger.LogError("Invalid: ModePercentage must be between 1 and 100.");
                throw new Exception("Invalid: ModePercentage must be between 1 and 100.");
            }

            if (_config.RTV.MaxNominationWinners < 0) {
                Logger.LogError("Invalid: MaxNominationWinners cannot be negative.");
                throw new Exception("Invalid: MaxNominationWinners cannot be negative.");
            }

            if (_config.RTV.TriggerRoundsBeforeEnd < 0) {
                Logger.LogError("Invalid: TriggerRoundsBeforeEnd cannot be negative.");
                throw new Exception("Invalid: TriggerRoundsBeforeEnd cannot be negative.");
            }

            if (_config.RTV.TriggerSecondsBeforeEnd < 0) {
                Logger.LogError("Invalid: TriggerSecondsBeforeEnd cannot be negative.");
                throw new Exception("Invalid: TriggerSecondsBeforeEnd cannot be negative.");
            }

            // Warmup settings
            if (_config.Warmup.Time < 0)
            {
                Logger.LogError("Invalid: Warmup Time cannot be negative.");
                throw new Exception("Invalid: Warmup Time cannot be negative.");
            }

            // Config version check
            if (_config.Version < 12)
            {
                throw new Exception("Your config file is too old, please backup and remove it from addons/counterstrikesharp/configs/plugins/GameModeManager to recreate it");
            }

            // Set config
            Config = _config;

            // Load dependencies
            _dependencyManager.OnConfigParsed(_config);
        }
    }
}