> [!IMPORTANT]
> On the first load, a configuration file will be created in `csgo/addons/counterstrikesharp/configs/plugins/GameModeManager/GameModeManager.json`.
<details>
<summary><b>Click to see Default Values</b></summary>
	
```json
{
  "Version": 17,
  "WorkshopAddon": "3831211166",
  "Maps": {
    "Delay": 5,
    "Default": "de_dust2"
  },
  "Votes": {
    "Enabled": false,
    "GameModes": false,
    "GameSettings": false,
    "Style": "menu",
    "Percentage": 51,
    "Duration": 30,
    "MinPlayers": 1,
    "Cooldown": 60,
    "AllowSpectators": false,
    "AllowInWarmup": false,
    "NativeTitleToken": "",
    "NativeQuestions": false
  },
  "MapChooser": {
    "GeneratePools": true,
    "TriggerSecondsBeforeEnd": 60,
    "TriggerKillsBeforeEnd": 13,
    "Rtv": true,
    "ModesInVotes": true,
    "NextMapMessages": true
  },
  "AdminPanel": {
    "Enabled": true,
    "CommandMenus": "default"
  },
  "Notifications": {
    "Enabled": true,
    "Chat": true
  },
  "Settings": {
    "Enabled": true,
    "Folder": "settings"
  },
  "Actions": {
    "Enabled": true,
    "Folder": "actions",
    "Rounds": true,
    "RoundsPermission": "@css/generic"
  },
  "Warmup": {
    "Enabled": true,
    "Time": 60,
    "PerMap": false,
    "MinPlayers": 0,
    "Ready": false,
    "ReadyPercent": 100,
    "Default": {
      "Name": "Deathmatch",
      "Config": "warmup/dm.cfg"
    },
    "List": [
      {
        "Name": "Deathmatch",
        "Config": "warmup/dm.cfg"
      },
      {
        "Name": "Knives Only",
        "Config": "warmup/knives_only.cfg"
      },
      {
        "Name": "Scoutz Only",
        "Config": "warmup/scoutz_only.cfg"
      }
    ]
  },
  "Commands": {
    "Maps": true,
    "Mode": true,
    "Modes": true,
    "TimeLimit": true
  },
  "Rotation": {
    "Enabled": true,
    "WhenServerEmpty": false,
    "CustomTimeLimit": 600,
    "ModeRotation": false,
    "ModeInterval": 4,
    "ModeVote": "random",
    "CountSkippedMaps": false,
    "RecentModes": 2,
    "ModeSchedules": false,
    "Schedule": [
      {
        "Time": "10:00",
        "Mode": "Casual"
      },
      {
        "Time": "15:00",
        "Mode": "Practice"
      },
      {
        "Time": "17:00",
        "Mode": "Competitive"
      }
    ]
  },
  "GameModes": {
    "Default": {
      "Name": "Casual",
      "Config": "casual.cfg",
      "DefaultMap": null,
      "MapGroups": [
        "mg_active",
        "mg_comp"
      ],
      "InVotes": true,
      "InRotation": true,
      "MinPlayers": 0,
      "MaxPlayers": 0
    },
    "MapGroupFile": "gamemodes_server.txt",
    "List": [
      {
        "Name": "45",
        "Config": "45.cfg",
        "DefaultMap": "3276886893",
        "MapGroups": [
          "mg_45"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "1v1",
        "Config": "1v1.cfg",
        "DefaultMap": "3070253400",
        "MapGroups": [
          "mg_1v1"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Armsrace",
        "Config": "ar.cfg",
        "DefaultMap": "ar_pool_day",
        "MapGroups": [
          "mg_gg"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Awp",
        "Config": "awp.cfg",
        "DefaultMap": "3142070597",
        "MapGroups": [
          "mg_awp"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Aim",
        "Config": "aim.cfg",
        "DefaultMap": "3084291314",
        "MapGroups": [
          "mg_aim"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Battle",
        "Config": "battle.cfg",
        "DefaultMap": "3070253400",
        "MapGroups": [
          "mg_battle"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Battle Royale",
        "Config": "br.cfg",
        "DefaultMap": "3070253400",
        "MapGroups": [
          "mg_battleroyale"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Bhop",
        "Config": "bhop.cfg",
        "DefaultMap": "3088973190",
        "MapGroups": [
          "mg_bhop"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Casual",
        "Config": "casual.cfg",
        "DefaultMap": "de_dust2",
        "MapGroups": [
          "mg_active",
          "mg_comp"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Casual 1.6",
        "Config": "Casual-1.6.cfg",
        "DefaultMap": "3212419403",
        "MapGroups": [
          "mg_Casual-1.6"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Competitive",
        "Config": "comp.cfg",
        "DefaultMap": "de_dust2",
        "MapGroups": [
          "mg_active",
          "mg_comp"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Course",
        "Config": "course.cfg",
        "DefaultMap": "3070455802",
        "MapGroups": [
          "mg_course"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Deathmatch",
        "Config": "dm.cfg",
        "DefaultMap": "de_mirage",
        "MapGroups": [
          "mg_dm"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Deathmatch (Valve)",
        "Config": "dm-valve.cfg",
        "DefaultMap": "de_mirage",
        "MapGroups": [
          "mg_dm"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Deathrun",
        "Config": "deathrun.cfg",
        "DefaultMap": "3164611860",
        "MapGroups": [
          "mg_deathrun"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Executes",
        "Config": "executes.cfg",
        "DefaultMap": "de_mirage",
        "MapGroups": [
          "mg_comp"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "GG",
        "Config": "gg.cfg",
        "DefaultMap": "ar_pool_day",
        "MapGroups": [
          "mg_gg"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "HE Only",
        "Config": "he.cfg",
        "DefaultMap": "3089842427",
        "MapGroups": [
          "mg_he"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Hide N Seek",
        "Config": "hns.cfg",
        "DefaultMap": "3097563690",
        "MapGroups": [
          "mg_hns"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "KreedZ",
        "Config": "kz.cfg",
        "DefaultMap": "3086304337",
        "MapGroups": [
          "mg_kz"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Minigames",
        "Config": "minigames.cfg",
        "DefaultMap": "3082120895",
        "MapGroups": [
          "mg_minigames"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Practice",
        "Config": "prac.cfg",
        "DefaultMap": "de_dust2",
        "MapGroups": [
          "mg_comp"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Prefire",
        "Config": "prefire.cfg",
        "DefaultMap": "de_inferno",
        "MapGroups": [
          "mg_comp"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Retakes",
        "Config": "retake.cfg",
        "DefaultMap": "de_dust2",
        "MapGroups": [
          "mg_comp"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "ScoutzKnivez",
        "Config": "scoutzknivez.cfg",
        "DefaultMap": "3073929825",
        "MapGroups": [
          "mg_scoutzknivez"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Surf",
        "Config": "surf.cfg",
        "DefaultMap": "3082548297",
        "MapGroups": [
          "mg_surf"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Soccer",
        "Config": "soccer.cfg",
        "DefaultMap": "3070198374",
        "MapGroups": [
          "mg_soccer"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Tournament",
        "Config": "tournament.cfg",
        "DefaultMap": "de_dust2",
        "MapGroups": [
          "mg_active"
        ],
        "InVotes": true,
        "InRotation": true
      },
      {
        "Name": "Wingman",
        "Config": "wingman.cfg",
        "DefaultMap": "de_memento",
        "MapGroups": [
          "mg_active",
          "mg_comp"
        ],
        "InVotes": true,
        "InRotation": true
      }
    ]
  }
}
```
</details>

# General Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Version             | Config version. GameModeManager won't load a config older than version 12. On load, it adds settings from newer versions with their defaults and sets this to the current version, keeping the previous file as `GameModeManager.json.v<old version>.bak` (the updated file has no comments). Values already set are never changed. |
| WorkshopAddon       | Workshop IDs (comma separated) of the addons with the menu files, added to MultiAddonManager's list. Empty = you load them yourself.      |

# Map Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Delay               | Map change delay in seconds.                                                                                                              |
| Default             | Default map on server start (i.e. de_dust2).                                                                                              |

# Map Chooser Settings
These settings control how GameModeManager works with [GG1MapChooser](https://github.com/ssypchenko/GG1MapChooser).

| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| GeneratePools       | Writes `cfg/GGMCmaps.json`: one GG1MapChooser map pool per mode from its map groups.                                                      |
| TriggerSecondsBeforeEnd | Time-limited modes: starts the end-of-map vote this many seconds before the end.                                                      |
| TriggerKillsBeforeEnd | Arms race: starts the end-of-map vote when the leader has this many kills.                                                              |
| Rtv                 | Lets players use `!rtv` and `!nominate`. Added in 2.1.0. Off doesn't affect end-of-map votes or rotations.                               |
| ModesInVotes        | Offers one random other mode in votes and `!nominate`, reshuffled every map.                                                              |
| NextMapMessages     | GameModeManager announces the next map or mode as a notification and a chat line, with its display name, instead of GG1MapChooser's center message and chat line. GG1MapChooser's `!nextmap` reply is unchanged. Added in config version 16. |

# Vote Settings
These settings are for player votes (`!changemap`, `!changemode`, `!changesetting`).

| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Enabled             | Enables player votes.                                                                                                                     |
| GameModes           | Enables game mode votes.                                                                                                                  |
| GameSettings        | Enables game setting votes.                                                                                                               |
| Style               | `menu` = PanoramaMenu Yes/No menu, `native` = the game's F1/F2 vote panel for map and mode votes (it only shows its own texts, so it asks "Change current level to …?"). Setting votes use the menu, unless NativeTitleToken is set. |
| Percentage          | Percent of eligible players that must vote yes.                                                                                           |
| Duration            | Vote duration in seconds.                                                                                                                 |
| MinPlayers          | Minimum eligible players to start a vote.                                                                                                 |
| Cooldown            | Seconds before the same player can start another vote.                                                                                    |
| AllowSpectators     | Lets spectators vote, and counts them towards the percentage.                                                                             |
| AllowInWarmup       | Allows votes during warmup.                                                                                                               |
| NativeTitleToken    | Puts setting votes on the F1/F2 panel with their own question: set it to `#SFUI_vote_panorama_vote_default`. Empty = setting votes use the menu. Map and mode votes always use the game's own "Change current level to X?", so they work for every player. Needs the file below on every player's PC. |
| NativeQuestions     | `true` puts `!vote` questions on the F1/F2 panel too, when NativeTitleToken is set. `false` (default) keeps them on the menu.            |

**Custom vote panel text needs a file on every player's PC.** The game looks the panel's text up on each player's computer, not on the server, and it opens no panel at all for a text it doesn't know. With `NativeTitleToken` set:

1. Every player downloads [`platform_english.txt`](https://github.com/nickj609/GameModeManager/blob/main/bundle/csgo/resource/platform_english.txt) (the same file GG1MapChooser ships in `Resources/`) to `Steam\steamapps\common\Counter-Strike Global Offensive\game\csgo\resource\` on their PC. If a `platform_english.txt` is already there, add the `SFUI_vote_panorama_vote_*` lines to it.
2. They restart CS2. The game reads the file only at startup.

A player without the file sees no panel for setting votes or questions and can't vote on them (map and mode votes still show), and Steam's "Verify integrity of game files" can remove it. The menu addon (`tools/build-menu-addon.ps1`) also carries the file, but the game may read it only at startup, before the addon loads, so don't count on that. Keep `NativeTitleToken` empty on public servers.

# Admin Panel Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Enabled             | Adds a **Manage game** section to CS2-SimpleAdmin's menu when it's installed.                                                             |
| CommandMenus        | What `!maps`, `!modes`, `!settings` and `!warmup` open. `"default"`: GameModeManager's own menus. `"admin"`: the same pages in CS2-SimpleAdmin's admin panel, so admin tools always look like it. Falls back to `"default"` when SimpleAdmin or the Manage game section isn't loaded. Added in config version 15. |

# Notification Settings
Messages meant for one player also show as a PanoramaMenuManager notification, in the player's language. Added in config version 15:
- **The player's own action:** language set, vote cast (on the menu: the game's F1/F2 panel shows it itself), why a vote can't start, map not in the pool.
- **What they asked for:** current map and mode, and, from the `!game` menu, the time, rounds or wins left and the next map (a warning when there's no limit or no next map yet).
- **An admin's own change:** map, mode, setting, rotation option or warmup. Everyone still sees the change announced in chat.

Messages to everyone stay in chat.

| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Enabled             | Shows the notification. Needs `"Notifications": true` in `MenuManagerCore.json`; players can turn them off in `!menu`.                    |
| Chat                | Also prints the message in chat. PanoramaMenuManager can't tell GameModeManager when a notification wasn't shown, so keep this on unless every player sees them. |

# Game Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Enabled             | Enables custom game settings.                                                                                                             |
| Folder              | Default settings folder within `/csgo/cfg/`.                                                                                              |

# Action Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Enabled             | Enables `!actions` and the admin panel's Actions page. Added in config version 17.                                                      |
| Folder              | Actions folder within `/csgo/cfg/`. Each cfg in it is an action, and each subfolder a group of them. See the FAQ, "How do I add actions?" |
| Rounds              | Adds a Rounds page to the Actions menus: **Pause match** (one row that shows "Cancel pending pause" while CS2 waits for freeze time, and "Unpause match" once paused, however it was paused; greyed out during a timeout), **CT timeout** and **T timeout** (greyed out while a pause or timeout is running or waiting). While MatchZy is loaded, these rows are replaced by a greyed-out "Pause with .pause": MatchZy runs pauses and timeouts itself, and **Replay this round**, **Replay last round** and **Go back to round** from the game's round backups (`mp_backup_round_auto 1`). Going back restores the round, both scores and every player's money, and the game pauses the match after. Added in config version 17. |
| RoundsPermission    | The flag the Rounds page needs. Default `@css/generic`.                                                                                 |

# Command Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Maps                | Enables or disables the !maps admin command.                                                                                              |
| Mode                | Enables or disables the !mode admin command.                                                                                              |
| Modes               | Enables or disables the !modes admin command.                                                                                             |
| TimeLimit           | Enables or disables the !timelimit admin command.                                                                                         |

# Warmup Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Enabled             | Enables or disables warmup.                                                                                                               |
| Time                | Warmup length in seconds. 0 = whatever `mp_warmuptime` is.                                                                                |
| PerMap              | Enables or disables per map warmup (i.e. map configs).                                                                                    |
| MinPlayers          | Warmup waits for this many players on a team. Until then the game's own warmup countdown is paused (`mp_warmup_pausetimer`), and carries on once they're there. `0` = it doesn't wait. A mode or map cfg can set its own with `css_warmup_minplayers <n>`. Added in config version 17. |
| Ready               | Ready-up: players type `!ready` (and `!unready`), the countdown stays paused, and the warmup ends once `ReadyPercent` of the players on a team are ready. A mode or map cfg can turn it on or off with `css_warmup_ready <true|false> [percent]`. Added in config version 17. |
| ReadyPercent        | Share of players who must be ready, from 1 to 100. Default 100.                                                                           |
| Default             | Default warmup mode. `"Random"` picks a different one each time a warmup starts.                                                          |
| List                | Warmup modes with their names. Every other cfg in `cfg/warmup/` is a warmup mode too, named by its `// name: ...` header (else its file name), so a new warmup only needs its cfg. |

# Rotation Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Enabled             | Enables rotations. When off, GameModeManager leaves the map alone at the end of a match.                                                  |
| WhenServerEmpty     | Enables rotation on server empty.                                                                                                         |
| CustomTimeLimit     | Custom time limit in seconds for rotations on server empty.                                                                               |
| ModeRotation        | Enables game mode rotation. Turned off at load if ModeSchedules is on, because the schedule wins.                                         |
| ModeInterval        | Changes game mode every x map rotations. (If ModeRotation is enabled)                                                                     |
| ModeVote            | What happens on the map that ends with a mode change. `"random"`: no map vote, and a random mode at the end. `"vote"`: every vote on that map (end of map, `!rtv`, `!nominate`) lists only the modes in rotation, and the winner is the next mode (a random one when nobody votes). Players are told when the match starts. Added in config version 16. |
| CountSkippedMaps    | Maps left early (RTV, an admin's map change) count toward ModeInterval too, so "every 4 maps" means any 4 maps. Off: only maps played to the end count. An early change to another mode on the map that was due counts either way, so the mode doesn't change twice. Added in config version 16. |
| RecentModes         | Random mode picks (rotation, `!mode random`), the mode put in map votes and mode votes skip the last this many modes played before the current one, when others are left. `0` = only the current one is skipped. Added in config version 17. |
| ModeSchedules       | Enables mode schedules. Wins over ModeRotation.                                                                                           |
| Schedule            | Schedule for mode rotations (24-hr format in the server's local time)                                                                                    |

# Game Mode Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- | 
| Default             | Default mode on server start (i.e. deathmatch).                                                                                           | 
| MapGroupFile        | Map groups file name in `/csgo/`. The file must be in [VDF Format](https://developer.valvesoftware.com/wiki/VDF).                         | 
| List                | The modes on your server. Each has a Name, Config, DefaultMap and MapGroups, plus the two settings below.                                 |
| InVotes             | A mode's setting. `false` keeps it out of votes, `!nominate`, `!changemode` and random picks. Admins can still choose it. Added in 2.1.0. |
| InRotation          | A mode's setting. `false` keeps it out of mode rotation. Schedules can still use it. Added in 2.1.0.                                      |
| MinPlayers          | A mode's setting, off by default. Player votes (`!changemode`, map votes with a mode in them) and `!nominate` only offer the mode with at least this many players (bots and spectating HLTV don't count). Player menus show it greyed out with the players it's for. `0` = no limit. Random picks (rotation, `!mode random`) prefer modes that fit, and take any mode when none does. Choosing a mode by name in admin commands and menus, and rotation's mode vote, ignore it. Added in config version 17. |
| MaxPlayers          | A mode's setting: the same, with at most this many players. `0` = no limit. Added in config version 17. |
| List                | A customizable list of game modes for your server with friendly names for menus.                                                          |  

> [!CAUTION]
> - All configuration files must be within `/csgo/cfg/`.
> - Your mode config files must use `css_gamemode` to cycle the game mode for the plugin.