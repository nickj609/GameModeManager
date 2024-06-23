![Copyright Nickj609l](https://img.shields.io/badge/Copyright-Nickj609-red) ![GitHub License](https://img.shields.io/github/license/nickj609/GameModeManager) ![Issues](https://img.shields.io/github/issues/nickj609/GameModeManager) ![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/nickj609/GameModeManager/total) ![GitHub commits since latest release](https://img.shields.io/github/commits-since/nickj609/GameModeManager/latest)

# GameModeManager
A Counter-Strike 2 server plugin to help administrators manage custom game modes, settings, and map rotations. 

Inspired by [CS2 Modded Dedicated Server by Kus](https://github.com/kus/cs2-modded-server) and the [CS2 Rock The Vote plugin by Abnerfs](https://github.com/abnerfs/cs2-rockthevote).

## Description
Tired of manually managing game modes, settings, and maps?

GameModeManager simplifies server administration for Counter-Strike 2 by providing:

- **Admin menus** for modes, maps, and settings.
- **Automatic rotation** for maps and game modes.
- **Player voting** for maps, settings, and game modes.
- **Seamless integration** with your existing Rock the Vote (RTV) plugin.
- **Dynamic map list and menu updates** based on the current game mode.
- **Customization options** for settings folders, voting styles, and languages.

This plugin is perfect for servers with a variety of custom content or those that want to give players more control over their experience.
  
## Enjoying the plugin?
Please drop a ⭐ star in the repository

![image](https://github.com/nickj609/GameModeManager/assets/32173425/4c1bef1e-ef13-4a30-b2eb-b02060535bcb)

## Credits
This plugin utilizes the [GameLoop.Vdf library](https://github.com/shravan2x/Gameloop.Vdf/) (licensed under the [MIT License](https://github.com/shravan2x/Gameloop.Vdf/blob/master/LICENSE)) for parsing the `gamemodes_server.txt` file, which is in [Valve Data Format](https://developer.valvesoftware.com/wiki/VDF).

For creating custom votes, this plugin utilizes the [CS2-CustomVotes](https://github.com/imi-tat0r/CS2-CustomVotes) shared plugin API (licensed under the [MIT License](https://github.com/imi-tat0r/CS2-CustomVotes?tab=MIT-1-ov-file)). 

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

</details>
	
<details>
<summary>Admin Commands</summary>

- `!allmaps` - Displays an admin menu for changing the map to any map from any game mode. 

- `!maps` - Displays an admin menu for changing the map. 

   > _It only shows maps for the current game mode._

   ![Screenshot 2024-06-15 214126](https://github.com/nickj609/GameModeManager/assets/32173425/6da6d946-c876-4182-b7d2-8bb4e8d7341f)
  
   ![Screenshot 2024-06-15 215052](https://github.com/nickj609/GameModeManager/assets/32173425/a3d701c6-bba5-446f-90b4-fe849b901a84)

- `!map <map name> <workshop id>` - Changes the map to the map specified.
  
   > _The worksop ID is *optional* and only required for maps that aren't explicitly set for a given map group._
   
- `!modes` - Displays an admin menu for changing the game mode.

   ![Screenshot 2024-06-15 214427](https://github.com/nickj609/GameModeManager/assets/32173425/2c6448e7-b101-423b-a5d0-f93d7775e71f)
  
   ![image](https://github.com/nickj609/GameModeManager/assets/32173425/3f517755-d3cf-48fd-a331-d0332cfd48b3)

- `!mode <mode name>` - Changes the game mode to the mode specified.
  
   > _For example, for **mg_surf** you would do **!mode surf**._

- `!settings` - Displays an admin menu for enabling or disabling custom game settings.

   ![image](https://github.com/nickj609/GameModeManager/assets/32173425/d0481582-2eb5-4cc9-99cf-51c0dcec2acf)
   ![Screenshot 2024-06-15 215321](https://github.com/nickj609/GameModeManager/assets/32173425/882da0f6-36f4-4bc1-b70b-096535526a78)

- `!setting <enable|disable> <setting name>` - Enables or disables a custom game setting.
  
   > _For example, for **enable_movement_unlock.cfg** you would do **!setting movement_unlock**._

</details>

<details>
<summary>Player Commands</summary>

- `!game` - Displays a **dynamic** menu of all game mode manager commands.

   ![Screenshot 2024-06-15 202045](https://github.com/nickj609/GameModeManager/assets/32173425/51451ff1-df41-4b51-881a-a8727d5cbffd)

- `!currentmap` - Displays the current map. 

   ![Screenshot 2024-06-15 202240](https://github.com/nickj609/GameModeManager/assets/32173425/1d9f9e12-2320-4ab8-a021-c4a5477e533a)

- `!showmaps` - Displays a **dynamic** menu of all per-map votes that can be created.

  > _It only shows maps for the current game mode._

   ![image](https://github.com/nickj609/GameModeManager/assets/32173425/1fe0914e-0a48-477c-b5a7-6f57abc391ba)

   ![image](https://github.com/nickj609/GameModeManager/assets/32173425/793e6d64-2b89-4875-8410-cef3982ee8aa)

- `!showallmaps` - Displays a menu of all per-map votes that can be created for all games modes. 

- `!currentmode` - Displays the current game mode.

   ![Screenshot 2024-06-15 202302](https://github.com/nickj609/GameModeManager/assets/32173425/b546851b-6e2d-4e3a-a012-be8b4223a8cb)

- `!changemode` - Creates a vote to change the game mode (all modes).

   ![Screenshot 2024-06-08 212539](https://github.com/nickj609/GameModeManager/assets/32173425/f5e3d915-4c01-45d5-95a2-a40b693e17bb)

   ![image](https://github.com/nickj609/GameModeManager/assets/32173425/d516adef-5ead-445e-9fa3-30a275d80e17)

- `!showmodes` - Displays a menu of all per-mode votes that can be created.

   ![Screenshot 2024-06-08 212831](https://github.com/nickj609/GameModeManager/assets/32173425/8fd7e73f-c2e9-459d-bf33-9878de227f55)

   ![image](https://github.com/nickj609/GameModeManager/assets/32173425/bbca59d5-7ac8-4f16-bcb9-b42941899546)

- `!showsettings` - Displays a menu of all per-setting votes that can be created.

   ![Screenshot 2024-06-08 212803](https://github.com/nickj609/GameModeManager/assets/32173425/16a907d1-3bd9-4416-bda6-4d6cc4c55030)
   
   ![Screenshot 2024-06-08 213008](https://github.com/nickj609/GameModeManager/assets/32173425/b6a34ec1-277f-4361-bd1c-0e405b20834f)

  
</details>

## Installation
1. Install Metamod:Source and Counter Strike Sharp.
2. Copy `addons` and `cfg` folders to `/csgo/`.
3. Make sure your `gamemodes_server.txt` or custom map group file is in [VDF Format](https://developer.valvesoftware.com/wiki/VDF) and contains a list of map groups.
5. If needed, update each game mode configuration file (i.e. comp.cfg) to include `css_gamemode <mode>`.
6. After the first run, update the configuration file `GameModeManager.json` as detailed below.

## Configuration
> [!IMPORTANT]
> On the first load, a configuration file will be created in `csgo/addons/counterstrikesharp/configs/plugins/GameModeManager/GameModeManager.json`.

### RTV Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- | 
| Enabled             | Enables RTV Compatibility.                                                                                                                | 
| Plugin              | Default path for the desired RTV plugin.                                                                                                  | 
| MapListFile         | Default path for the maplist.txt file to update when the map group or game mode changes.                                                  | 
| DefaultMapFormat    | Enables the default format for adding maps to the map list file: `ws:{workshopid}`. When disabled: `{mapname}:{workshopid}`.              |

### Game Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- | 
| Enabled             | Enables custom game settings.                                                                                                             | 
| Folder              | Default settings folder within `/csgo/cfg/`.                                                                                              | 
| Style               | Changes setting menu type (i.e. "chat" or "center").                                                                                      |

### Map Group Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Delay               | Map change change delay in seconds.                                                                                                       | 
| Default             | Default map group on server start (i.e. mg_active).                                                                                       | 
| File                | Map groups file name in `/csgo/`. The file must be in [VDF Format](https://developer.valvesoftware.com/wiki/VDF).                         |     

### Game Mode Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- | 
| Rotation            | Enables game mode rotation.                                                                                                               |  
| Interval            | Changes game mode every x map rotations.                                                                                                  | 
| Delay               | Delay for changing game modes in seconds.                                                                                                 | 
| DefaultMode         | Default mode on server start (i.e. deathmatch).                                                                                           | 
| Style               | Changes setting menu type (i.e. "chat" or "center").                                                                                      |
| List                | A customizable list of game modes for your server with friendly names for menus.                                                          |  

### Vote Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- | 
| Enabled             | Enables voting.                                                                                                                           | 
| Map                 | Enables map votes.                                                                                                                        | 
| AllMap              | Enables all map votes.                                                                                                                    |
| GameMode            | Enables game mode votes.                                                                                                                  |
| GameSetting         | Enables game setting votes.                                                                                                               |
| Style               | Changes vote menu type (i.e. "chat" or "center").                                                                                         |

> [!CAUTION]
> - All configuration files must be within `/csgo/cfg/`.
> - Your mode config files must use `css_gamemode` to cycle the game mode for the plugin.

<details>
<summary><b>Click to see Default Values</b></summary>
	
```
// This configuration was automatically generated by CounterStrikeSharp for plugin 'GameModeManager', at 2024/06/23 02:51:06
{
  "Version": 4,
  "RTV": {
    "Enabled": false,
    "Plugin": "addons/counterstrikesharp/plugins/RockTheVote/RockTheVote.dll",
    "MapListFile": "addons/counterstrikesharp/plugins/RockTheVote/maplist.txt",
    "DefaultMapFormat": false
  },
  "Votes": {
    "Enabled": true,
    "Map": true,
    "AllMap": true,
    "GameMode": true,
    "GameSetting": true,
    "Style": "center"
  },
  "Settings": {
    "Enabled": true,
    "Folder": "settings",
    "Style": "center"
  },
  "Commands": {
    "Map": true
  },
  "MapGroups": {
    "Delay": 2,
    "Default": "mg_active",
    "DefaultMap": "de_dust2",
    "Style": "center",
    "File": "gamemodes_server.txt"
  },
  "GameModes": {
    "Rotation": true,
    "Interval": 4,
    "DefaultMode": "Casual",
    "Delay": 2,
    "Style": "center",
    "List": {
      "Casual": {
        "casual.cfg": [
          "mg_active",
          "mg_hostage"
        ]
      },
      "Competitive": {
        "comp.cfg": [
          "mg_active",
          "mg_hostage"
        ]
      },
      "Wingman": {
        "wingman.cfg": [
          "mg_wingman"
        ]
      },
      "Deathmatch": {
        "dm.cfg": [
          "mg_dm"
        ]
      },
      "Deathmatch Multicfg": {
        "dm-multicfg.cfg": [
          "mg_dm"
        ]
      },
      "ArmsRace": {
        "ar.cfg": [
          "mg_armsrace"
        ]
      },
      "GunGame": {
        "gg.cfg": [
          "mg_armsrace"
        ]
      },
      "1v1": {
        "1v1.cfg": [
          "mg_1v1"
        ]
      },
      "Aim": {
        "aim.cfg": [
          "mg_aim"
        ]
      },
      "Bhop": {
        "bhop.cfg": [
          "mg_bhop"
        ]
      },
      "Surf": {
        "surf.cfg": [
          "mg_surf"
        ]
      },
      "Awp": {
        "awp.cfg": [
          "mg_awp"
        ]
      },
      "ScoutzKnivez": {
        "scoutzknivez.cfg": [
          "mg_scoutzknivez"
        ]
      },
      "Soccer": {
        "soccer.cfg": [
          "mg_soccer"
        ]
      },
      "HE Only": {
        "minigames.cfg": [
          "mg_he"
        ]
      }
    }
  }
}
```
</details>

### Languages
This plugin will display all in-game menus and messaging based on the player's preferred language. Below is an example language configuration file you can customize to your liking. The [CS2-CustomVotes](https://github.com/imi-tat0r/CS2-CustomVotes) plugin also has additional language files you can configure. 

<details>
<summary>Example Lang</summary>
	
```
{
  "plugin.prefix": "[{GREEN}Server{DEFAULT}]",
  "game.menu-title": "Game Commands",
  "currentmap.message": "{RED}[Current Map]{DEFAULT} {0}",
  "currentmode.message": "{GREEN}[Current Mode]{DEFAULT} {0}",
  "changemap.message": "{LIGHTRED}{0}{DEFAULT} has changed the map to {LIGHTRED}{1}{DEFAULT}.",
  "changemode.message": "Admin {LIGHTRED}{0}{DEFAULT} has changed the game mode to {LIGHTRED}{1}{DEFAULT}.",
  "enable.changesetting.message": "Admin {LIGHTRED}{0}{DEFAULT} has {LIGHTRED}Enabled{DEFAULT} setting {LIGHTRED}{1}{DEFAULT}.",
  "disable.changesetting.message": "Admin {LIGHTRED}{0}{DEFAULT} has {LIGHTRED}Disabled{DEFAULT} setting {LIGHTRED}{1}{DEFAULT}.",
  "menu.yes": "Yes",
  "menu.no": "No",
  "menu.enable": "Enable",
  "menu.disable": "Disable",
  "mode.show.menu-response": "Say {GREEN}!{0}{DEFAULT} to create a vote.",
  "mode.vote.menu-title": "Change game mode to {GOLD}{0}{DEFAULT}?",
  "modes.menu-title": "Game Mode List",
  "modes.vote.menu-title": "Change game mode?",
  "map.vote.menu-title": "Change map to {0}?",
  "maps.menu-title": "Map List",
  "maps.show.menu-response": "Say {GREEN}!{0}{DEFAULT} to create a vote.",
  "setting.vote.menu-title": "Change setting {GOLD}{0}{DEFAULT}?",
  "setting.show.menu-response": "Say {GREEN}!{0}{DEFAULT} to create a vote.",
  "settings.menu-actions": "Setting Actions",
  "settings.menu-title": "Setting List"
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

```
2024-06-08 22:59:32.805 +00:00 [INFO] plugin:GameModeManager Loading map groups...
2024-06-08 22:59:32.821 +00:00 [INFO] plugin:GameModeManager Creating game modes...
2024-06-08 22:59:32.823 +00:00 [INFO] plugin:GameModeManager Loading settings...
2024-06-08 22:59:32.827 +00:00 [WARN] plugin:GameModeManager Skipping random_setting.cfg because its missing the correct prefix.
2024-06-08 22:59:32.835 +00:00 [INFO] plugin:GameModeManager Enabling game mode and map rotations...
2024-06-08 22:59:34.096 +00:00 [INFO] plugin:GameModeManager Registering custom votes...
2024-06-08 23:01:12.832 +00:00 [WARN] plugin:GameModeManager New map group could not be found. Setting default map group.
2024-06-08 23:05:24.421 +00:00 [INFO] plugin:GameModeManager Current map group is mg_active.
2024-06-08 23:05:24.421 +00:00 [INFO] plugin:GameModeManager New map group is mg_active.
2024-06-08 24:15:47.044 +00:00 [INFO] plugin:GameModeManager Game has ended. Picking random map from current map group...
2024-06-08 24:17:76.044 +00:00 [INFO] plugin:GameModeManager Deregistering custom votes...
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

```
"GameModes": 
{
  "Rotation": true,
  "Interval": 4,
  "DefaultMode": "Casual",
  "Delay": 2,
  "Style": "center",
  "List": {
    "Casual": {
      "casual.cfg": [
        "mg_active",
        "mg_hostage"
      ]
    },
    "Competitive": {
      "comp.cfg": [
        "mg_active",
        "mg_hostage"
      ]
    },
    "Wingman": {
      "wingman.cfg": [
        "mg_wingman"
      ]
    },
    "Deathmatch": {
      "dm.cfg": [
        "mg_dm"
      ]
    },
    "Deathmatch Multicfg": {
      "dm-multicfg.cfg": [
        "mg_dm"
      ]
    },
    "ArmsRace": {
      "ar.cfg": [
        "mg_armsrace"
      ]
    },
    "GunGame": {
      "gg.cfg": [
        "mg_armsrace"
      ]
    },
    "1v1": {
      "1v1.cfg": [
        "mg_1v1"
      ]
    },
    "Aim": {
      "aim.cfg": [
        "mg_aim"
      ]
    },
    "Bhop": {
      "bhop.cfg": [
        "mg_bhop"
      ]
    },
    "Surf": {
      "surf.cfg": [
        "mg_surf"
      ]
    },
    "Awp": {
      "awp.cfg": [
        "mg_awp"
      ]
    },
    "ScoutzKnivez": {
      "scoutzknivez.cfg": [
        "mg_scoutzknivez"
      ]
    },
    "Soccer": {
      "soccer.cfg": [
        "mg_soccer"
      ]
    },
    "HE Only": {
      "minigames.cfg": [
        "mg_he"
      ]
    }
  }
}
```

If you have `ListEnabled` set to `false`, game modes are generated based on the map groups in your map group file (default is `gamemodes_server.txt`).

</details>

<details>
<summary>What are game mode and map rotations not working?</summary>
<br>

Game mode and map rotations do not work if RTV compatibility is enabled. Game mode and map rotations are only counted when handled by the plugin's game event handler. 

</details>

<details>
<summary>Why is RTV not working? </summary>
<br>

You need to install your own supported RTV plugin and update the JSON configuration file. Any RTV plugin with a `maplist.txt` file is supported. 

```
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
