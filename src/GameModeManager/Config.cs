// Included libraries
using GameModeManager.Core;
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
        public bool InVotes { get; set; } = true; // false = left out of votes, !nominate, !changemode and random picks. Admins can still choose it
        public bool InRotation { get; set; } = true; // false = mode rotation never picks it. Schedules can still use it
    }

    public class WarmupModeEntry
    {
        public string Name { get; set; } = "Deathmatch";
        public string Config { get; set; } = "warmup/dm.cfg";
    }

    // Define map chooser settings
    public class MapChooserSettings
    {
        public bool GeneratePools { get; set; } = true; // Writes cfg/GGMCmaps.json: one GG1 pool per mode from its map groups
        public int TriggerSecondsBeforeEnd { get; set; } = 60; // Time-limited modes: start GG1's vote this many seconds before the end
        public int TriggerKillsBeforeEnd { get; set; } = 13; // Arms race: start GG1's vote when the leader has this many kills
        public bool Rtv { get; set; } = true; // Lets players use !rtv and !nominate. Off doesn't affect end-of-map votes or rotations
        public bool ModesInVotes { get; set; } = true; // One random other mode offered in votes and !nominate, reshuffled every map
    }

    // Define admin panel settings
    public class AdminPanelSettings
    {
        public bool Enabled { get; set; } = true; // Adds a Game Modes section to CS2-SimpleAdmin's menu when it's installed, opening the !modes, !maps, !settings and !warmup menus
        public string CommandMenus { get; set; } = "default"; // What !maps, !modes, !settings and !warmup open: "default" = GameModeManager's own menus, "admin" = the same pages in CS2-SimpleAdmin's admin panel, or the default menus when it isn't loaded
    }

    // Define notification settings
    public class NotificationSettings
    {
        public bool Enabled { get; set; } = true; // Shows a PanoramaMenuManager notification, in the player's language, for messages about their own action (language set, vote cast, can't vote, map not found). Needs "Notifications": true in MenuManagerCore.json
        public bool Chat { get; set; } = true; // Also prints those messages in chat. PanoramaMenuManager can't tell when a notification isn't shown (turned off in !menu, menu files not loaded yet), so turn this off only if every player sees them
    }

    // Define map settings
    public class MapSettings
    {
        public int Delay { get; set; } = 5; // Map change delay in seconds
        public string Default { get; set; } = "de_dust2"; // Default map on server start
    }

    // Define vote settings
    public class VoteSettings
    {
        public bool Enabled { get; set; } = false; // Enables player votes (!changemode, !changesetting)
        public bool GameModes { get; set; } = false; // Enables vote to change game mode
        public bool GameSettings { get; set; } = false; // Enables vote to change game setting
        public string Style { get; set; } = "menu"; // "menu" = PanoramaMenu Yes/No menu, "native" = the game's F1/F2 vote panel
        public int Percentage { get; set; } = 51; // Percent of eligible players that must vote yes
        public int Duration { get; set; } = 30; // Vote duration in seconds
        public int MinPlayers { get; set; } = 1; // Minimum eligible players to start a vote
        public int Cooldown { get; set; } = 60; // Seconds before the same player can start another vote
        public bool AllowSpectators { get; set; } = false; // Spectators can vote (and count towards the percentage)
        public bool AllowInWarmup { get; set; } = false; // Allows votes during warmup
        public string NativeTitleToken { get; set; } = ""; // Native panel title from platform_english.txt on the server and every player's PC. Empty = the game's "Change current level to X?"
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
        public bool Maps { get; set; } = true; // Enables or disables !maps admin command
        public bool Mode { get; set; } = true; // Enables or disables !mode admin command
        public bool Modes { get; set; } = true; // Enables or disables !modes admin command 
        public bool TimeLimit { get; set; } = true; // Enables or disables !timelimit admin command
    }

    public class WarmupSettings
    {
        public bool Enabled { get; set; } = true; // Enables or disables warmup
        public float Time { get; set; } = 60; // Warmup length in seconds. 0 = whatever mp_warmuptime is
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

        public bool InVotes(string mode) => List.FirstOrDefault(entry => entry.Name.Equals(mode, StringComparison.OrdinalIgnoreCase))?.InVotes != false;
        public bool InRotation(string mode) => List.FirstOrDefault(entry => entry.Name.Equals(mode, StringComparison.OrdinalIgnoreCase))?.InRotation != false;

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
        public int Version { get; set; } = 15;
        public string WorkshopAddon { get; set; } = "3831211166"; // Workshop IDs (comma separated) of the addons with the menus' panorama files, added to MultiAddonManager's list. Empty = you load them yourself
        public MapSettings Maps { get; set; } = new();
        public VoteSettings Votes { get; set; } = new();
        public MapChooserSettings MapChooser { get; set; } = new();
        public AdminPanelSettings AdminPanel { get; set; } = new();
        public NotificationSettings Notifications { get; set; } = new();
        public GameSettings Settings { get; set; } = new();
        public WarmupSettings Warmup { get; set; } = new();
        public CommandSettings Commands { get; set; } = new();
        public RotationSettings Rotation { get; set; } = new();
        public GameModeSettings GameModes { get; set; } = new();

        // The checks that only need the config itself. tools/ConfigCheck runs these on Kus's config.
        public void Validate(ILogger logger)
        {
            // Maps settings
            if (String.IsNullOrEmpty(Maps.Default))
            {
                logger.LogError("Invalid: Default map must not be empty.");
                throw new Exception("Invalid: Default map must not be empty.");
            }
            if (Maps.Delay < 0)
            {
                logger.LogError("Invalid: Maps Delay cannot be negative.");
                throw new Exception("Invalid: Maps Delay cannot be negative.");
            }

            // Game mode settings
            if (GameModes.List.Count == 0)
            {
                logger.LogError("Undefined: Game modes list cannot be empty.");
                throw new Exception("Undefined: Game modes list cannot be empty.");
            }

            if (GameModes.Default == null)
            {
                logger.LogError("Invalid: Default game mode must not be empty.");
                throw new Exception("Invalid: Default game mode must not be empty.");
            }

            // A duplicate name stops the plugin loading when the modes are added
            HashSet<string> modeNames = new(StringComparer.OrdinalIgnoreCase);
            foreach (ModeEntry mode in GameModes.List)
            {
                if (!modeNames.Add(mode.Name))
                {
                    logger.LogError("Invalid: Game mode \"{Name}\" is listed twice.", mode.Name);
                    throw new Exception($"Invalid: Game mode \"{mode.Name}\" is listed twice.");
                }
            }
            if (!modeNames.Contains(GameModes.Default.Name))
                logger.LogWarning("Default game mode \"{Name}\" isn't in the game modes list.", GameModes.Default.Name);

            // Admin command menus
            if (!AdminPanel.CommandMenus.Equals("default", StringComparison.OrdinalIgnoreCase) && !AdminPanel.CommandMenus.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("AdminPanel.CommandMenus \"{Value}\" isn't \"default\" or \"admin\": using \"default\".", AdminPanel.CommandMenus);
                AdminPanel.CommandMenus = "default";
            }

            // Rotation settings
            if (Rotation.ModeSchedules == true)
            {
                if (Rotation.Schedule.Count <= 0)
                {
                    logger.LogError("Invalid: Schedule cannot be empty");
                    throw new Exception("Invalid: Schedule cannot be empty");
                }
                foreach (ScheduleEntry entry in Rotation.Schedule)
                {
                    if (!DateTime.TryParse(entry.Time, out _))
                    {
                        logger.LogError("Invalid: Schedule time \"{Time}\" isn't a time, like \"15:00\".", entry.Time);
                        throw new Exception($"Invalid: Schedule time \"{entry.Time}\" isn't a time, like \"15:00\".");
                    }
                    if (!modeNames.Contains(entry.Mode))
                        logger.LogWarning("Schedule mode \"{Mode}\" isn't in the game modes list, so {Time} won't change anything.", entry.Mode, entry.Time);
                }

                // Rotating the mode every few maps would move the server off its schedule. Admins can still turn it on from the admin menu.
                if (Rotation.ModeRotation)
                {
                    logger.LogWarning("Rotation ModeRotation and ModeSchedules are both on: using the schedule, and turning ModeRotation off.");
                    Rotation.ModeRotation = false;
                }
            }
            if (Rotation.CustomTimeLimit < 0)
            {
                logger.LogError("Invalid: Rotation CustomTimeLimit cannot be negative.");
                throw new Exception("Invalid: Rotation CustomTimeLimit cannot be negative.");
            }
            if (Rotation.ModeInterval <= 0)
            {
                logger.LogError("Invalid: Rotation ModeInterval must be greater than 0.");
                throw new Exception("Invalid: Rotation ModeInterval must be greater than 0.");
            }

            // Map chooser settings
            if (MapChooser.TriggerSecondsBeforeEnd < 0 || MapChooser.TriggerKillsBeforeEnd < 0)
            {
                logger.LogError("Invalid: MapChooser values cannot be negative.");
                throw new Exception("Invalid: MapChooser values cannot be negative.");
            }

            // Vote settings
            if (!Votes.Style.Equals("menu", StringComparison.OrdinalIgnoreCase) && !Votes.Style.Equals("native", StringComparison.OrdinalIgnoreCase))
            {
                // 1.0.x configs say "center" or "chat" here, and shouldn't stop the plugin loading
                logger.LogWarning("Votes Style \"{Style}\" isn't available: using \"menu\". Use \"menu\" or \"native\".", Votes.Style);
                Votes.Style = "menu";
            }
            if (Votes.Percentage <= 0 || Votes.Percentage > 100)
            {
                logger.LogError("Invalid: Votes Percentage must be between 1 and 100.");
                throw new Exception("Invalid: Votes Percentage must be between 1 and 100.");
            }
            if (Votes.Duration <= 0)
            {
                logger.LogError("Invalid: Votes Duration must be greater than 0.");
                throw new Exception("Invalid: Votes Duration must be greater than 0.");
            }
            if (Votes.MinPlayers < 0 || Votes.Cooldown < 0)
            {
                logger.LogError("Invalid: Votes MinPlayers and Cooldown cannot be negative.");
                throw new Exception("Invalid: Votes MinPlayers and Cooldown cannot be negative.");
            }

            // Warmup settings
            if (Warmup.Time < 0)
            {
                logger.LogError("Invalid: Warmup Time cannot be negative.");
                throw new Exception("Invalid: Warmup Time cannot be negative.");
            }

            // Config version check
            if (Version < 12)
            {
                throw new Exception("Your config file is too old, please backup and remove it from addons/counterstrikesharp/configs/plugins/GameModeManager to recreate it");
            }
        }
    }

    // Define plugin class for parsing config
    public partial class Plugin : IPluginConfig<Config>
    {
        public required Config Config { get; set; }

        // Perform error checking
        public void OnConfigParsed(Config _config)
        {
            _config.Validate(Logger);

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

            // Set config
            Config = _config;

            // Load dependencies
            _dependencyManager.OnConfigParsed(_config);
        }
    }
}