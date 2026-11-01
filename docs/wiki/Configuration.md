> [!IMPORTANT]
> On the first load, a configuration file will be created in `csgo/addons/counterstrikesharp/configs/plugins/GameModeManager/GameModeManager.json`.
<details>
<summary><b>Click to see Default Values</b></summary>
	
```json
{
  "Version": 13,
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
    "UsePanorama": false
  },
  "MapChooser": {
    "GeneratePools": true,
    "TriggerSecondsBeforeEnd": 60,
    "TriggerKillsBeforeEnd": 13,
    "Rtv": true,
    "ModesInVotes": true
  },
  "AdminPanel": {
    "Enabled": true
  },
  "Settings": {
    "Enabled": true,
    "Folder": "settings"
  },
  "Warmup": {
    "Enabled": true,
    "Time": 60,
    "PerMap": false,
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
      ]
    },
    "MapGroupFile": "gamemodes_server.txt",
    "List": [
      {
        "Name": "45",
        "Config": "45.cfg",
        "DefaultMap": "3276886893",
        "MapGroups": [
          "mg_45"
        ]
      },
      {
        "Name": "1v1",
        "Config": "1v1.cfg",
        "DefaultMap": "3070253400",
        "MapGroups": [
          "mg_1v1"
        ]
      },
      {
        "Name": "Armsrace",
        "Config": "ar.cfg",
        "DefaultMap": "ar_pool_day",
        "MapGroups": [
          "mg_gg"
        ]
      },
      {
        "Name": "Awp",
        "Config": "awp.cfg",
        "DefaultMap": "3142070597",
        "MapGroups": [
          "mg_awp"
        ]
      },
      {
        "Name": "Aim",
        "Config": "aim.cfg",
        "DefaultMap": "3084291314",
        "MapGroups": [
          "mg_aim"
        ]
      },
      {
        "Name": "Battle",
        "Config": "battle.cfg",
        "DefaultMap": "3070253400",
        "MapGroups": [
          "mg_battle"
        ]
      },
      {
        "Name": "Battle Royale",
        "Config": "br.cfg",
        "DefaultMap": "3070253400",
        "MapGroups": [
          "mg_battleroyale"
        ]
      },
      {
        "Name": "Bhop",
        "Config": "bhop.cfg",
        "DefaultMap": "3088973190",
        "MapGroups": [
          "mg_bhop"
        ]
      },
      {
        "Name": "Casual",
        "Config": "casual.cfg",
        "DefaultMap": "de_dust2",
        "MapGroups": [
          "mg_active",
          "mg_comp"
        ]
      },
      {
        "Name": "Casual 1.6",
        "Config": "Casual-1.6.cfg",
        "DefaultMap": "3212419403",
        "MapGroups": [
          "mg_Casual-1.6"
        ]
      },
      {
        "Name": "Competitive",
        "Config": "comp.cfg",
        "DefaultMap": "de_dust2",
        "MapGroups": [
          "mg_active",
          "mg_comp"
        ]
      },
      {
        "Name": "Course",
        "Config": "course.cfg",
        "DefaultMap": "3070455802",
        "MapGroups": [
          "mg_course"
        ]
      },
      {
        "Name": "Deathmatch",
        "Config": "dm.cfg",
        "DefaultMap": "de_mirage",
        "MapGroups": [
          "mg_dm"
        ]
      },
      {
        "Name": "Deathmatch (Valve)",
        "Config": "dm-valve.cfg",
        "DefaultMap": "de_mirage",
        "MapGroups": [
          "mg_dm"
        ]
      },
      {
        "Name": "Deathrun",
        "Config": "deathrun.cfg",
        "DefaultMap": "3164611860",
        "MapGroups": [
          "mg_deathrun"
        ]
      },
      {
        "Name": "Executes",
        "Config": "executes.cfg",
        "DefaultMap": "de_mirage",
        "MapGroups": [
          "mg_comp"
        ]
      },
      {
        "Name": "GG",
        "Config": "gg.cfg",
        "DefaultMap": "ar_pool_day",
        "MapGroups": [
          "mg_gg"
        ]
      },
      {
        "Name": "HE Only",
        "Config": "he.cfg",
        "DefaultMap": "3089842427",
        "MapGroups": [
          "mg_he"
        ]
      },
      {
        "Name": "Hide N Seek",
        "Config": "hns.cfg",
        "DefaultMap": "3097563690",
        "MapGroups": [
          "mg_hns"
        ]
      },
      {
        "Name": "KreedZ",
        "Config": "kz.cfg",
        "DefaultMap": "3086304337",
        "MapGroups": [
          "mg_kz"
        ]
      },
      {
        "Name": "Minigames",
        "Config": "minigames.cfg",
        "DefaultMap": "3082120895",
        "MapGroups": [
          "mg_minigames"
        ]
      },
      {
        "Name": "Practice",
        "Config": "prac.cfg",
        "DefaultMap": "de_dust2",
        "MapGroups": [
          "mg_comp"
        ]
      },
      {
        "Name": "Prefire",
        "Config": "prefire.cfg",
        "DefaultMap": "de_inferno",
        "MapGroups": [
          "mg_comp"
        ]
      },
      {
        "Name": "Retakes",
        "Config": "retake.cfg",
        "DefaultMap": "de_dust2",
        "MapGroups": [
          "mg_comp"
        ]
      },
      {
        "Name": "ScoutzKnivez",
        "Config": "scoutzknivez.cfg",
        "DefaultMap": "3073929825",
        "MapGroups": [
          "mg_scoutzknivez"
        ]
      },
      {
        "Name": "Surf",
        "Config": "surf.cfg",
        "DefaultMap": "3082548297",
        "MapGroups": [
          "mg_surf"
        ]
      },
      {
        "Name": "Soccer",
        "Config": "soccer.cfg",
        "DefaultMap": "3070198374",
        "MapGroups": [
          "mg_soccer"
        ]
      },
      {
        "Name": "Tournament",
        "Config": "tournament.cfg",
        "DefaultMap": "de_dust2",
        "MapGroups": [
          "mg_active"
        ]
      },
      {
        "Name": "Wingman",
        "Config": "wingman.cfg",
        "DefaultMap": "de_memento",
        "MapGroups": [
          "mg_active",
          "mg_comp"
        ]
      }
    ]
  }
}
```
</details>

# General Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Version             | Config version. GameModeManager won't load a config older than version 12.                                                                |
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
| Rtv                 | Lets players use `!rtv`. Added in 2.0.1. Off doesn't affect end-of-map votes or rotations.                                                |
| ModesInVotes        | Offers one random other mode in votes and `!nominate`, reshuffled every map.                                                              |

# Vote Settings
These settings are for player votes (`!changemap`, `!changemode`, `!changesetting`).

| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Enabled             | Enables player votes.                                                                                                                     |
| GameModes           | Enables game mode votes.                                                                                                                  |
| GameSettings        | Enables game setting votes.                                                                                                               |
| Style               | `menu` = PanoramaMenu Yes/No menu, `native` = the game's F1/F2 vote panel.                                                                |
| Percentage          | Percent of eligible players that must vote yes.                                                                                           |
| Duration            | Vote duration in seconds.                                                                                                                 |
| MinPlayers          | Minimum eligible players to start a vote.                                                                                                 |
| Cooldown            | Seconds before the same player can start another vote.                                                                                    |
| AllowSpectators     | Lets spectators vote, and counts them towards the percentage.                                                                             |
| AllowInWarmup       | Allows votes during warmup.                                                                                                               |
| NativeTitleToken    | Native panel title from `platform_english.txt` on the server and every player's PC. Empty = the game's "Change current level to X?".      |
| UsePanorama         | Deprecated. `true` = Style `native`.                                                                                                      |

# Admin Panel Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Enabled             | Adds a **Manage game** section to CS2-SimpleAdmin's menu when it's installed.                                                             |

# Game Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Enabled             | Enables custom game settings.                                                                                                             |
| Folder              | Default settings folder within `/csgo/cfg/`.                                                                                              |

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
| Default             | Default warmup mode.                                                                                                                      |
| List                | A customizable list of warmup modes for your server.                                                                                      |

# Rotation Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Enabled             | Enables rotations. When off, GameModeManager leaves the map alone at the end of a match.                                                  |
| WhenServerEmpty     | Enables rotation on server empty.                                                                                                         |
| CustomTimeLimit     | Custom time limit in seconds for rotations on server empty.                                                                               |
| ModeRotation        | Enables game mode rotation.                                                             |
| ModeInterval        | Changes game mode every x map rotations. (If ModeRotation is enabled)                                                                     |
| ModeSchedules       | Enables mode schedules.                                                                  |
| Schedule            | Schedule for mode rotations (24-hr format in the server's local time)                                                                                    |

# Game Mode Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- | 
| Default             | Default mode on server start (i.e. deathmatch).                                                                                           | 
| MapGroupFile        | Map groups file name in `/csgo/`. The file must be in [VDF Format](https://developer.valvesoftware.com/wiki/VDF).                         | 
| List                | A customizable list of game modes for your server with friendly names for menus.                                                          |  

> [!CAUTION]
> - All configuration files must be within `/csgo/cfg/`.
> - Your mode config files must use `css_gamemode` to cycle the game mode for the plugin.