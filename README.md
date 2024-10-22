![Copyright Nickj609l](https://img.shields.io/badge/Copyright-Nickj609-red) ![GitHub License](https://img.shields.io/github/license/nickj609/GameModeManager) ![Issues](https://img.shields.io/github/issues/nickj609/GameModeManager) ![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/nickj609/GameModeManager/total) ![GitHub commits since latest release](https://img.shields.io/github/commits-since/nickj609/GameModeManager/latest)

# GameModeManager
A Counter-Strike 2 server plugin to help administrators manage custom game modes, settings, and map rotations. 

Inspired by [CS2 Modded Dedicated Server by Kus](https://github.com/kus/cs2-modded-server) and the [CS2 Rock The Vote plugin by Abnerfs](https://github.com/abnerfs/cs2-rockthevote).

## Description
Tired of manually managing game modes, settings, and maps?

GameModeManager simplifies server administration for Counter-Strike 2 by providing:

- **Admin menus** for modes, maps, and settings.
- **Customizable rotation** of maps and game modes.
- **Player voting** for maps, settings, and game modes.
- **Seamless integration** with your existing Rock the Vote (RTV) plugin.
- **Dynamic map lists and menus** based on the current game mode or all game modes.
- **Customization options** for rotation schedules, commands, voting styles, and languages.

This plugin is perfect for servers with a variety of custom content or those that want to give players more control over their experience.
  
## Enjoying the plugin?
Please drop a ⭐ star in the repository

![image](https://github.com/nickj609/GameModeManager/assets/32173425/4c1bef1e-ef13-4a30-b2eb-b02060535bcb)

## Credits
This plugin utilizes the [GameLoop.Vdf library](https://github.com/shravan2x/Gameloop.Vdf/) (licensed under the [MIT License](https://github.com/shravan2x/Gameloop.Vdf/blob/master/LICENSE)) for parsing the `gamemodes_server.txt` file, which is in [Valve Data Format](https://developer.valvesoftware.com/wiki/VDF).

For creating custom votes, this plugin utilizes the [CS2-CustomVotes](https://github.com/imi-tat0r/CS2-CustomVotes) shared plugin API (licensed under the [MIT License](https://github.com/imi-tat0r/CS2-CustomVotes?tab=MIT-1-ov-file)). 

For creating WASD menus, this plugin utilizes a custom fork of [WASDMenuAPI](https://github.com/Interesting-exe/WASDMenuAPI) shared plugin API (licensed under the [MIT License](https://github.com/Interesting-exe/WASDMenuAPI?tab=MIT-1-ov-file)). 

## Requirements
- [Counter-Strike 2](https://www.counter-strike.net/cs2)
- [Metamod:Source](https://github.com/alliedmodders/metamod-source/) (v1282+)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) (v.197+)

## RTV Plugin Compatibility
This plugin is compatible with any RTV plugin using a `maplist.txt` file.

![Screenshot 2024-03-21 161846](https://github.com/nickj609/GameModeManager/assets/32173425/1e291efb-fe7f-4f0d-bb2c-e21d042bd153)

## Commands
<details>
<summary>Server Commands</summary>

- `css_gamemode <mode>` - Sets the current mode.

- `css_rtv_enabled <true|false>` - Enables or disables RTV.

- `css_warmupmode <mode>` - Schedules and sets the warmup mode.

- `css_timelimit <true|false> optional: <seconds>` - Enables or disables time limit.


</details>
	
<details>
<summary>Admin Commands</summary>

- `!timelimit <true|false|> optional: <seconds>` - Sets the time limit for the current map.

   ![Screenshot 2024-09-24 171240](https://github.com/user-attachments/assets/1d91ad0e-cf4e-4c87-b221-b36806c0ffe5) 

- `!maps` - Displays an admin menu for changing the map. 

   > _By default, it only shows maps for the current game mode unless AllMaps is set to true._

   ![Screenshot 2024-10-20 205255](https://github.com/user-attachments/assets/3bf9e64d-bd2c-4a9c-8075-7c288852fe83)
   ![Screenshot 2024-06-15 215052](https://github.com/nickj609/GameModeManager/assets/32173425/a3d701c6-bba5-446f-90b4-fe849b901a84)

- `!map <map name> <workshop id>` - Changes the map to the map specified.
  
   > _The workshop ID is *optional* and only required for maps that aren't explicitly set for a given map group._
   
- `!modes` - Displays an admin menu for changing the game mode.

   ![Screenshot 2024-10-20 205327](https://github.com/user-attachments/assets/706ab1f9-74fa-4ffe-a4e4-925fdcfd7716)
   ![image](https://github.com/nickj609/GameModeManager/assets/32173425/3f517755-d3cf-48fd-a331-d0332cfd48b3)

- `!mode <mode name>` - Changes the game mode to the mode specified.
  
   > _For example, for **mg_surf** you would do **!mode surf**._

- `!settings` - Displays an admin menu for enabling or disabling custom game settings.

   ![Screenshot 2024-10-20 205406](https://github.com/user-attachments/assets/ebb042bf-d423-4047-95f8-33a762c5a9da)
   ![Screenshot 2024-06-15 215321](https://github.com/nickj609/GameModeManager/assets/32173425/882da0f6-36f4-4bc1-b70b-096535526a78)

- `!setting <enable|disable> <setting name>` - Enables or disables a custom game setting.
  
   > _For example, for **enable_movement_unlock.cfg** you would do **!setting movement_unlock**._

</details>

<details>
<summary>Player Commands</summary>

- `!game` - Displays a **dynamic** menu of all player commands.

   ![image](https://github.com/user-attachments/assets/5f45876d-7c4b-45a7-95fe-fc96f0dae57f)

- `!currentmap` - Displays the current map. 

   ![Screenshot 2024-06-15 202240](https://github.com/nickj609/GameModeManager/assets/32173425/1d9f9e12-2320-4ab8-a021-c4a5477e533a)

- `!changemap` - Displays a **dynamic** menu of all per-map votes that can be created.

  > _By default, it only shows maps for the current game mode unless AllMaps is set to true._

   ![Screenshot 2024-10-20 205255](https://github.com/user-attachments/assets/5f5ba46d-72b6-4873-b5b2-f367155543c6)

- `!timeleft` - Displays the timeleft in the current map.

   ![Screenshot 2024-09-24 171203](https://github.com/user-attachments/assets/a5aabd36-1a59-4a0d-a7aa-42d40ee1ea4f)


- `!currentmode` - Displays the current game mode.

   ![Screenshot 2024-06-15 202302](https://github.com/nickj609/GameModeManager/assets/32173425/b546851b-6e2d-4e3a-a012-be8b4223a8cb)

- `!changemode` - Displays a menu of all per-mode votes that can be created.

   ![Screenshot 2024-10-20 205327](https://github.com/user-attachments/assets/706ab1f9-74fa-4ffe-a4e4-925fdcfd7716)

- `!changesetting` - Displays a menu of all per-setting votes that can be created.

   ![Screenshot 2024-10-20 215225](https://github.com/user-attachments/assets/93769872-a4d2-4bee-9470-bffca7796136)

</details>

## Installation
1. Install Metamod:Source and Counter Strike Sharp.
2. Copy `addons` and `cfg` folders to `/csgo/`.
3. Make sure your `gamemodes_server.txt` or custom map group file is in [VDF Format](https://developer.valvesoftware.com/wiki/VDF) and contains a list of map groups.
4. Update each game mode configuration file (i.e. comp.cfg) to include `css_gamemode <mode>`.
6. After the first run, update the configuration file `GameModeManager.json` as detailed below.

## Configuration
> [!IMPORTANT]
> On the first load, a configuration file will be created in `csgo/addons/counterstrikesharp/configs/plugins/GameModeManager/GameModeManager.json`.

### RTV Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- | 
| Enabled             | Enables RTV compatibility.                                                                                                                |
| Mode                | Changes the RTV mode. (0 = maps from current game mode, 1 = maps from all game modes.)                                                    | 
| Plugin              | Default path for the desired RTV plugin (i.e. `/addons/counterstrikesharp/plugins/plugin`)                                                | 
| MapList             | Default path for the maplist.txt file to update when the map group or game mode changes.                                                  | 
| MapFormat           | Enables the default format for adding maps to the map list file: `ws:{workshopid}`. When disabled: `{mapname}:{workshopid}`.              |

### Map Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Delay               | Map change change delay in seconds.                                                                                                       | 
| Default             | Default map group on server start (i.e. mg_active).                                                                                       | 
| Style               | Changes vote menu type (i.e. "chat", "center" or "wasd").                                                                                         |

### Vote Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- | 
| Enabled             | Enables voting.                                                                                                                           | 
| Maps                | Enables map votes.                                                                                                                        | 
| AllMaps             | Enables all maps votes.                                                                                                                    |
| GameModes           | Enables game mode votes.                                                                                                                  |
| GameSettings        | Enables game setting votes.                                                                                                               |
| Style               | Changes vote menu type (i.e. "chat", "center" or "wasd").                                                                                         |

### Game Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- | 
| Enabled             | Enables custom game settings.                                                                                                             | 
| Folder              | Default settings folder within `/csgo/cfg/`.                                                                                              | 
| Style               | Changes setting menu type (i.e. "chat", "center" or "wasd").                                                                                      | 

### Command Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Map                 | Enables or disables the !map admin command.                                                                                               | 
| Maps                | Enables or disables the !maps admin command.                                                                                              | 
| AllMaps             | Enables or disables the !allmaps admin command.                                                                                           |
| TimeLeft            | Enables or disables the !timeleft command.                                                                                           |

### Warmup Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Time                | Default warmup time                                                                                                                       | 
| PerMap              | Enables ot disables per map warmup (i.e. map configs)                                                                                     | 
| Default             | Default warmup mode.                                                                                                                      |
| List                | A customizable list of warmup modes for your server.                                                                                      |

### Rotation Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- | 
| Enabled             | Enables rotations. (Cannot be enabled when RTV is enabled)                                                                                | 
| WhenServerEmpty     | Enables rotation on server empty.                                                                                                         | 
| CustomTimeLimit     | Custom time limit for rotations on server empty.                                                                                          | 
| Cycle               | Changes the rotation cycle. (0 = maps from current mode, 1 = maps from all modes, 2, maps from specific map groups)                       |
| MapGroups           | Mapgroups to use for rotation cycle 2.                                                                                                    |
| ModeRotation        | Enables game mode rotation. (Cannot be enabled when ModeSchedule is enabled)                                                              | 
| ModeInterval        | Changes game mode every x map rotations. (If ModeRotation is enabled)                                                                     | 
| ModeSchedule        | Enables mode schedules. (Cannot be enabled when ModeRotations is enabled)                                                                 | 
| Schedule            | Schedule for mode rotations (24-hr format in UTC time)                                                                                    | 

### Game Mode Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- | 
| Default             | Default mode on server start (i.e. deathmatch).                                                                                           | 
| Style               | Changes setting menu type (i.e. "chat", "center" or "wasd").                                                                                      |
| MapGroupFile        | Map groups file name in `/csgo/`. The file must be in [VDF Format](https://developer.valvesoftware.com/wiki/VDF).                         | 
| List                | A customizable list of game modes for your server with friendly names for menus.                                                          |  

> [!CAUTION]
> - All configuration files must be within `/csgo/cfg/`.
> - Your mode config files must use `css_gamemode` to cycle the game mode for the plugin.

<details>
<summary><b>Click to see Default Values</b></summary>
	
```json
// This configuration was automatically generated by CounterStrikeSharp for plugin 'GameModeManager', at 2024/09/23 12:52:21
{
  "Version": 7,
  "RTV": {
    "Enabled": false,
    "Mode": 0,
    "MapFormat": false,
    "Plugin": "addons/counterstrikesharp/plugins/RockTheVote/RockTheVote.dll",
    "MapList": "addons/counterstrikesharp/plugins/RockTheVote/maplist.txt"
  },
  "Maps": {
    "Delay": 5,
    "Style": "center",
    "Default": "de_dust2"
  },
  "Votes": {
    "Enabled": false,
    "Maps": false,
    "AllMaps": false,
    "GameModes": false,
    "GameSettings": false,
    "Style": "center"
  },
  "Settings": {
    "Enabled": true,
    "Style": "center",
    "Folder": "settings"
  },
  "Warmup": {
    "Time": 60,
    "PerMap": false,
    "Default": {
      "Name": "Knives Only",
      "Config": "warmup/knives_only.cfg",
      "DefaultMap": null,
      "MapGroups": []
    },
    "List": [
      {
        "Name": "Deathmatch",
        "Config": "warmup/dm.cfg",
        "DefaultMap": null,
        "MapGroups": []
      },
      {
        "Name": "Knives Only",
        "Config": "warmup/knives_only.cfg",
        "DefaultMap": null,
        "MapGroups": []
      }
    ]
  },
  "Commands": {
    "Map": true,
    "Maps": true,
    "AllMaps": false,
    "TimeLeft": true
  },
  "Rotation": {
    "Enabled": true,
    "WhenServerEmpty": false,
    "CustomTimeLimit": 600,
    "Cycle": 0,
    "MapGroups": [
      "mg_active",
      "mg_comp"
    ],
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
    "Style": "center",
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
        "Name": "Casual",
        "Config": "casual.cfg",
        "DefaultMap": "de_dust2",
        "MapGroups": [
          "mg_active",
          "mg_comp"
        ]
      },
      {
        "Name": "Deathmatch",
        "Config": "dm.cfg",
        "DefaultMap": "de_assembly",
        "MapGroups": [
          "mg_dm"
        ]
      },
      {
        "Name": "ArmsRace",
        "Config": "armsrace.cfg",
        "DefaultMap": "ar_pool_day",
        "MapGroups": [
          "mg_gg"
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
        "Name": "Wingman",
        "Config": "wingman.cfg",
        "DefaultMap": "de_memento",
        "MapGroups": [
          "mg_active",
          "mg_comp"
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
        "Name": "Executes",
        "Config": "executes.cfg",
        "DefaultMap": "de_mirage",
        "MapGroups": [
          "mg_comp"
        ]
      },
      {
        "Name": "Casual 1.6",
        "Config": "Casual-1.6.cfg",
        "DefaultMap": "3212419403",
        "MapGroups": [
          "mg_active",
          "mg_comp"
        ]
      },
      {
        "Name": "Deathmatch Multicfg",
        "Config": "dm-multicfg.cfg",
        "DefaultMap": "de_mirage",
        "MapGroups": [
          "mg_dm"
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
        "Name": "45",
        "Config": "45.cfg",
        "DefaultMap": "3276886893",
        "MapGroups": [
          "mg_45"
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
        "Name": "1v1",
        "Config": "1v1.cfg",
        "DefaultMap": "3070253400",
        "MapGroups": [
          "mg_1v1"
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
        "Name": "Bhop",
        "Config": "bhop.cfg",
        "DefaultMap": "3088973190",
        "MapGroups": [
          "mg_bhop"
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
        "Name": "KreedZ",
        "Config": "kz.cfg",
        "DefaultMap": "3086304337",
        "MapGroups": [
          "mg_kz"
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
        "Name": "Soccer",
        "Config": "soccer.cfg",
        "DefaultMap": "3070198374",
        "MapGroups": [
          "mg_soccer"
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
        "Name": "Deathrun",
        "Config": "deathrun.cfg",
        "DefaultMap": "3164611860",
        "MapGroups": [
          "mg_deathrun"
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
        "Name": "ScoutzKnivez",
        "Config": "scoutzknivez.cfg",
        "DefaultMap": "3073929825",
        "MapGroups": [
          "mg_scoutzknivez"
        ]
      }
    ]
  }
}
```
</details>

### Languages
This plugin will display all in-game menus and messaging based on the player's preferred language. Below is an example language configuration file you can customize to your liking. The [CS2-CustomVotes](https://github.com/imi-tat0r/CS2-CustomVotes) plugin also has additional language files you can configure. 

<details>
<summary>Example Lang</summary>
	
```json
{
  "plugin.prefix": "[{GREEN}Server{DEFAULT}]",
  "game.menu-title": "Game Commands",
  "currentmap.message": "{RED}[Current Map]{DEFAULT} {0}",
  "currentmode.message": "{GREEN}[Current Mode]{DEFAULT} {0}",
  "changemap.message": "Admin {LIGHTRED}{0}{DEFAULT} has changed the map to {LIGHTRED}{1}{DEFAULT}.",
  "changemode.message": "Admin {LIGHTRED}{0}{DEFAULT} has changed the game mode to {LIGHTRED}{1}{DEFAULT}.",
  "enable.changesetting.message": "Admin {LIGHTRED}{0}{DEFAULT} has {LIGHTRED}Enabled{DEFAULT} setting {LIGHTRED}{1}{DEFAULT}.",
  "disable.changesetting.message": "Admin {LIGHTRED}{0}{DEFAULT} has {LIGHTRED}Disabled{DEFAULT} setting {LIGHTRED}{1}{DEFAULT}.",
  "menu.yes": "Yes",
  "menu.no": "No",
  "menu.enable": "Enable",
  "menu.disable": "Disable",
  "warmup.start.message": "Warmup mode {GREEN}{0}{DEFAULT} started.",
  "warmup.end.message": "Game mode {GREEN}{0}{DEFAULT} started.",
  "mode.vote.menu-title": "Change game mode to {GOLD}{0}{DEFAULT}?",
  "modes.menu-title": "Game Mode List",
  "modes.vote.menu-title": "Change game mode?",
  "map.vote.menu-title": "Change map to {0}?",
  "maps.menu-title": "Map List",
  "setting.vote.menu-title": "Change setting {GOLD}{0}{DEFAULT}?",
  "settings.menu-actions": "Setting Actions",
  "settings.menu-title": "Setting List",
  "timeleft.prefix": "{RED}[Timeleft]{DEFAULT}",
  "timeleft.remaining-rounds": "{0} round(s) remaining",
  "timeleft.remaining-time-hour": "Remaining time {0}:{1}:{2}",
  "timeleft.remaining-time-minute": "Remaining time {0} minute(s) and {1} second(s)",
  "timeleft.remaining-time-second": "Remaining time {0} second(s)",
  "timeleft.no-time-limit": "There is no time limit",
  "timeleft.remaining-last-round": "This is the last round",
  "timeleft.remaining-time-over": "Time is over, this is the last round",
  "timeleft.warmup": "Command disabled during warmup.",
  "timelimit.enabled": "Time limit {GREEN}Enabled{DEFAULT}.",
  "timelimit.disabled": "Time limit {RED}Disabled{DEFAULT}.",
  "timelimit.enabled-error": "Time limit is already {GREEN}Enabled{DEFAULT}.",
  "timelimit.disabled-error": "Time limit is already {RED}Disabled{DEFAULT}.",
  "timelimit.value-error": "Invalid time limit. Please enter a valid integer.",
  "timelimit.prefix": "{RED}[Timelimit]{DEFAULT}"
}
```

</details>

### Shared API
GameModeManager provides a shared API for other plugins to manage game modes. Add a reference to GameModeManager.Shared to your project to get started!
<details>
<summary><b>API Interface</b></summary>
	
```csharp
// Declare namespace
namespace GameModeManager.Shared;

public interface IGameModeApi
{
    // Define globals
    string WarmupMode { get; }
    string CurrentMap { get; }
    string CurrentMode { get; }  

    // Update map menus api handler
    public void UpdateMapMenus();

    // Trigger rotation api handler
    public void TriggerRotation();

    // Enable RTV compatibility api handler
    public void EnableRTV(bool enabled);

    // Change mode api handlers
    public void ChangeMode(string modeName);

    // Change map api handlers
    public void ChangeMap(string mapName);
    public void ChangeMap(string mapName, int delay);

    // Schedule warmup api handlers
    public bool isWarmupScheduled();
    public bool ScheduleWarmup(string modeName);

    // Enforce time limit api handlers
    public void EnableTimeLimit();
    public void EnableTimeLimit(int delay);

}
```
</details>

<details>
<summary><b>Example Usage</b></summary>

```csproj
<ItemGroup>
    <Reference Include="CS2-GameModeManager.Shared">
      <HintPath>..\GameModeManagger.Shared\GameModeManagger.Shared.dll</HintPath>
    </Reference>
</ItemGroup>
```

```csharp
// Included libraries
using GameModeManager.Shared;
using CounterStrikeSharp.API.Core.Capabilities;

// Declare namespace
namespace Plugin

public class Plugin : BasePlugin
{
	public PluginCapability<ICustomVoteApi> GameModeApi { get; } = new("game_mode:api");

	GameModeApi.ChangeMap(mapName);
	GameModeApi.ChangeMode(modeName);
}
```
</details>

## Logging
>[!WARNING]
> Due to the need to parse map groups and settings, you may have difficulties initially configuring the plugin, especially if your **map group file** is not configured properly. All logs associated with this plugin can be found in the below location.
> 
> `csgo/addons/counterstrikesharp/logs`

<details>
<summary><b>Example Log</b></summary>

```log
2024-06-08 22:59:32.827 +00:00 [WARN] plugin:GameModeManager Skipping random_setting.cfg because its missing the correct prefix.
2024-06-08 23:05:24.421 +00:00 [INFO] plugin:GameModeManager Current mode is Casual.
2024-06-08 23:05:24.421 +00:00 [INFO] plugin:GameModeManager New mode is Deathmatch.
2024-06-08 23:05:24.947 +00:00 [INFO] plugin:GameModeManager Regenerating map menus...
2024-06-08 24:15:47.044 +00:00 [INFO] plugin:GameModeManager Game has ended. Picking random map from current mode...
```
</details>

<details>
<summary><b>Common Error Messages</b></summary>

| Error/Warning Message                                              | Description                                                                                                              |
| -------------------------------------------------------------------| ------------------------------------------------------------------------------------------------------------------------ | 
| `Cannot Find`                                                      | Unable to locate the file specified from `GameModeManager.json` config.                                                  | 
| `Incomplete VDF data`                                              | Your `gamemodes_server.txt` file is not formatted properly in [VDF Format](https://developer.valvesoftware.com/wiki/VDF).| 
| `Your config file is too old`                                      | Please backup and remove it from `addons/counterstrikesharp/configs/plugins/GameModeManager` to recreate it.             |
| `The mapgroup property doesn't exist`                              | The "mapgroup" property cannot be found in your `gamemodes_server.txt` file.                                             | 
| `Mapgroup found, but the 'maps' property is missing or incomplete` | The "maps" property cannot be found in your `gamemodes_server.txt` file for one of your map groups.                      | 

</details>

## FAQ (Frequently Asked Questions)

<details>
<summary>How do I add custom settings?</summary>
<br>

To add custom settings, create two configuration files with the `enable_` and `disable_ prefix` (i.e. **enable_autobhop.cfg**, **disable_autobhop.cfg**). Then, put those files in the `/csgo/cfg/settings/` folder. This is the default settings folder. You can change this folder in the configuration settings.

</details>

<details>
<summary>How do I add game modes?</summary>
<br>

To add game modes, update the JSON key pairs in the configuration file (`csgo/addons/counterstrikesharp/configs/plugins/GameModeManager/GameModeManager.json`).

```json
"GameModes":
{
  "Style": "center",
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
      "Name": "Casual",
      "Config": "casual.cfg",
      "DefaultMap": "de_dust2",
      "MapGroups": [
        "mg_active",
        "mg_comp"
      ]
    },
    {
      "Name": "Deathmatch",
      "Config": "dm.cfg",
      "DefaultMap": "de_assembly",
      "MapGroups": [
        "mg_dm"
      ]
    },
    {
      "Name": "ArmsRace",
      "Config": "armsrace.cfg",
      "DefaultMap": "ar_pool_day",
      "MapGroups": [
        "mg_gg"
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
      "Name": "Wingman",
      "Config": "wingman.cfg",
      "DefaultMap": "de_memento",
      "MapGroups": [
        "mg_active",
        "mg_comp"
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
      "Name": "Executes",
      "Config": "executes.cfg",
      "DefaultMap": "de_mirage",
      "MapGroups": [
        "mg_comp"
      ]
    },
    {
      "Name": "Casual 1.6",
      "Config": "Casual-1.6.cfg",
      "DefaultMap": "3212419403",
      "MapGroups": [
        "mg_active",
        "mg_comp"
      ]
    },
    {
      "Name": "Deathmatch Multicfg",
      "Config": "dm-multicfg.cfg",
      "DefaultMap": "de_mirage",
      "MapGroups": [
        "mg_dm"
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
      "Name": "45",
      "Config": "45.cfg",
      "DefaultMap": "3276886893",
      "MapGroups": [
        "mg_45"
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
      "Name": "1v1",
      "Config": "1v1.cfg",
      "DefaultMap": "3070253400",
      "MapGroups": [
        "mg_1v1"
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
      "Name": "Bhop",
      "Config": "bhop.cfg",
      "DefaultMap": "3088973190",
      "MapGroups": [
        "mg_bhop"
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
      "Name": "KreedZ",
      "Config": "kz.cfg",
      "DefaultMap": "3086304337",
      "MapGroups": [
        "mg_kz"
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
      "Name": "Soccer",
      "Config": "soccer.cfg",
      "DefaultMap": "3070198374",
      "MapGroups": [
        "mg_soccer"
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
      "Name": "Deathrun",
      "Config": "deathrun.cfg",
      "DefaultMap": "3164611860",
      "MapGroups": [
        "mg_deathrun"
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
      "Name": "ScoutzKnivez",
      "Config": "scoutzknivez.cfg",
      "DefaultMap": "3073929825",
      "MapGroups": [
        "mg_scoutzknivez"
      ]
    }
  ]
}
```

</details>

<details>
<summary>Why are game mode and map rotations not working?</summary>
<br>

Game mode and map rotations do not work if RTV compatibility is enabled. Game mode and map rotations are only counted when handled by the plugin's game event handler. 

</details>

<details>
<summary>Why is RTV not working? </summary>
<br>

You need to install your own supported RTV plugin and update the JSON configuration file. Any RTV plugin with a `maplist.txt` file is supported. 

```json
"RTV": {
  "Enabled": false,
  "Plugin": "addons/counterstrikesharp/plugins/RockTheVote/RockTheVote.dll",
  "MapListFile": "addons/counterstrikesharp/plugins/RockTheVote/maplist.txt",
  "DefaultMapFormat": false
},
``` 

</details>

<details>
<summary>How are friendly names generated for settings?</summary>
<br>

Friendly names for settings are generated by removing the extension and underscores, and capitalizing the first letter of each word. For example, **enable_movement_unlock.cfg** turns into **Movement Unlock**.

</details>

<details>
<summary>How are vote commands created?</summary>
<br>

Vote commands for categories are set manually, such as `!changemode`. This may be configurable in the future, with the addition of aliases. Per-map commands are generated based on the map name (i.e. de_dust2). Per-mode commands are generated from the game mode list or the map group name (without the mg_ prefix).

</details>

<details>
<summary>Can I use friendly names for maps?</summary>
<br>

Yes! You can use friendly names for maps by using the following syntax:

```vdf
"mg_active"
{
	"imagename"			"mapgroup-active"
	"displayname"			"Casual"
	"nameID"			"#SFUI_Mapgroup_active"
	"tooltipID"			"#SFUI_MapGroup_Tooltip_Desc_Active"
	"tooltipMaps"			""
	"name"				"mg_active"
	"grouptype"			"active"
	"icon_image_path"		"map_icons/mapgroup_icon_active"
	"maps"
	{
		"de_dust2"		"Dust 2"
		"de_ancient"		"Ancient"
		"de_anubis"		"Anubis"
		"de_inferno"		"Inferno"
		"de_mirage"		"Mirage"
		"de_nuke"		"Nuke"
		"de_vertigo"		"Vertigo"
	}
}

```

</details>
