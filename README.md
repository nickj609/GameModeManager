![Copyright Nickj609l](https://img.shields.io/badge/Copyright-Nickj609-red) ![GitHub License](https://img.shields.io/github/license/nickj609/GameModeManager) ![Issues](https://img.shields.io/github/issues/nickj609/GameModeManager) ![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/nickj609/GameModeManager/total) ![GitHub commits since latest release](https://img.shields.io/github/commits-since/nickj609/GameModeManager/latest)

# GameModeManager
A Counter-Strike 2 server plugin to help administrators manage custom game modes, settings, and map rotations. 

Inspired by [CS2 Modded Dedicated Server by Kus](https://github.com/kus/cs2-modded-server) and the [CS2 Rock The Vote plugin by Abnerfs](https://github.com/abnerfs/cs2-rockthevote).

## Description
GameModeManager streamlines server administration with these features:

- **Admin Game Mode Menu:** Switch between game modes.
- **Admin Setting Menu:** Enable or disable custom settings.
- **Admin Map List Menu:** Switch between maps within the current game mode.
- **Player Voting:** Voting for custom game modes, game settings, and maps.
- **Default Map Cycles:** Automatically changes the map to a random map within the current map group.
- **Game Mode Rotations:** Specify how often you want the game mode to change.
- **RTV Compatibility:** Works seamlessly with your chosen RTV plugin, ensuring smooth rock-the-vote functionality.
  
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
This plugin is compatible with any RTV plugin using a maplist.txt file.

![Screenshot 2024-03-21 161846](https://github.com/nickj609/GameModeManager/assets/32173425/1e291efb-fe7f-4f0d-bb2c-e21d042bd153)

## Commands
<details>
<summary>Server Commands</summary>

- `css_rtv <true|false>` - Enables or disables RTV. (_Comming Soon_)

- `css_mapgroup <mg_name>` - Sets the map group and updates the map list and menus.

</details>
	
<details>
<summary>Admin Commands</summary>

- `!map <map name> <id>` - Changes the map to the map specified.
  
  > _The map ID is *optional* and only required for maps that aren't explicitly set for a given map group._

- `!maps (css_maps)` - Displays an admin menu for changing the map. 

   > _It only shows maps for the current game mode/map group._
   
- `!mode <mode name> (css_mode)` - Changes the game mode to the mode specified.
  
  > _For example, for **mg_surf** you would do **!mode surf**._ 
  
- `!modes (css_modes)` - Displays an admin menu for changing the game mode.

- `!setting <enable|disable> <setting name> (css_setting)` - Enables or disables a custom game setting.
  
  > _For example, for **enable_movement_unlock.cfg** you would do **!setting movement_unlock**._ 
  
- `!settings (css_settings)` - Displays an admin menu for enabling or disabling custom game settings.

</details>

<details>
<summary>Player Commands</summary>
	
- `!changemode` - Creates a vote to change the game mode (all modes).

   ![Screenshot 2024-06-08 212539](https://github.com/nickj609/GameModeManager/assets/32173425/f5e3d915-4c01-45d5-95a2-a40b693e17bb)

   ![Screenshot 2024-06-08 212613](https://github.com/nickj609/GameModeManager/assets/32173425/fa6473c6-5372-4c4b-afb6-2ef5087ea550)

- `!showmodes` - Menu to display all per mode votes that can be created.

   ![Screenshot 2024-06-08 212831](https://github.com/nickj609/GameModeManager/assets/32173425/8fd7e73f-c2e9-459d-bf33-9878de227f55)

   ![Screenshot 2024-06-08 213033](https://github.com/nickj609/GameModeManager/assets/32173425/4b252eb5-69ef-4973-89b8-b48c2f6f7019)

- `!showsettings` - Menu to display all per setting votes that can be created.

   ![Screenshot 2024-06-08 212803](https://github.com/nickj609/GameModeManager/assets/32173425/16a907d1-3bd9-4416-bda6-4d6cc4c55030)
   
   ![Screenshot 2024-06-08 213008](https://github.com/nickj609/GameModeManager/assets/32173425/b6a34ec1-277f-4361-bd1c-0e405b20834f)

- `!showmaps` - Menu to display all per map votes that can be created. This only shows maps from the current map group/game mode. 

  ![Screenshot 2024-06-08 212858](https://github.com/nickj609/GameModeManager/assets/32173425/1ba2a65a-8867-420c-9576-5549fa5e5469)

  ![Screenshot 2024-06-08 212923](https://github.com/nickj609/GameModeManager/assets/32173425/eb6a198a-a2cf-477b-ba02-ca6469bd38fc)
  
</details>

## Installation
1. Install Metamod:Source and Counter Strike Sharp.
2. Copy `addons` and `cfg` folders to `/csgo/`.
3. Make sure your `gamemodes_server.txt` or custom map group file is in [VDF Format](https://developer.valvesoftware.com/wiki/VDF) and contains a list of map groups.

   If you are not using the JSON configuration file for specifying game modes, include the "displayname" property within your `gamemodes_server.txt` or custom map group file for each map group.

   <details>
   <summary>Example</summary>
   
   ```
   "mg_dm"
	{
		"imagename"				"mapgroup-bomb"
		"displayname"				"Deathmatch"
		"nameID"				"#SFUI_Mapgroup_allclassic"
		"tooltipID"				"#SFUI_MapGroup_Tooltip_Desc_DeathMatch"
		"name"					"mg_dm"
		"icon_image_path"			"map_icons/mapgroup_icon_deathmatch"
		"maps"
		{
			"ar_shoots"						""
			"ar_baggage"						""
			"workshop/3070550406/de_safehouse"			""
			"workshop/3070563536/de_lake"				""
			"workshop/3070581293/de_bank"				""
			"workshop/3070923343/fy_pool_day"			""
			"workshop/3070238628/fy_iceworld"			""
		}
	}
   ```
   
   </details>
  
5. If needed, update each game mode configuration file (i.e. comp.cfg) to include `css_mapgroup <map group>`.
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
| Folder              | Default settings folder within `/csgo/cfg/`.                                                                                                  | 
| Style               | Changes setting menu type (i.e. "chat" or "center").                                                                                      |

### Map Group Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Delay               | Map change change delay in seconds.                                                                                                       | 
| Default             | Default map group on server start (i.e. mg_active).                                                                                       | 
| File                | Map groups file name. The file must be in [VDF Format](https://developer.valvesoftware.com/wiki/VDF) and WITHINin `/csgo/`.               |     

### Game Mode Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- | 
| Rotation            | Enables game mode rotation.                                                                                                               |  
| Interval            | Changes game mode every x map rotations.                                                                                                  | 
| Delay               | Delay for changing game modes in seconds.                                                                                                 | 
| Style               | Changes setting menu type (i.e. "chat" or "center").                                                                                      |
| ListEnabled         | Uses the game mode list in the config. Otherwise, the list is generated from map groups.                                                  |
| List                | A customizable list of game modes for your server with friendly names for menus.                                                                    |  

### Vote Settings
| Setting             | Description                                                                                                                               |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- | 
| Enabled             | Enables voting.                                                                                                                           | 
| Map                 | Enables map vote.                                                                                                                         | 
| GameMode            | Enables game mode votes.                                                                                                                  |
| GameSetting         | Enables game setting votes.                                                                                                               |
| Style               | Changes vote menu type (i.e. "chat" or "center").                                                                                         |

> [!CAUTION]
> - All configuration files must be within `/csgo/cfg/`.
> - Your mapgroup file must use `css_mapgroup` to cycle the current map group.

<details>
<summary><b>Click to see Default Values</b></summary>
	
```
// This configuration was automatically generated by CounterStrikeSharp for plugin 'GameModeManager', at 2024/06/08 09:52:11
{
  "Version": 2,
  "RTV": {
    "Enabled": false,
    "Plugin": "addons/counterstrikesharp/plugins/RockTheVote/RockTheVote.dll",
    "MapListFile": "addons/counterstrikesharp/plugins/RockTheVote/maplist.txt",
    "DefaultMapFormat": false
  },
  "MapGroup": {
    "Delay": 5,
    "Default": "mg_active",
    "File": "gamemodes_server.txt"
  },
  "Settings": {
    "Enabled": true,
    "Folder": "settings",
    "Style": "center"
  },
  "GameMode": {
    "Rotation": true,
    "Interval": 4,
    "Delay": 5,
    "Style": "center",
    "ListEnabled": true,
    "List": {
      "comp": "Competitive",
      "1v1": "1 vs 1",
      "aim": "Aim",
      "awp": "AWP Only",
      "scoutzknivez": "ScoutzKnives",
      "wingman": "Wingman",
      "gungame": "Gun Game",
      "surf": "Surf",
      "dm": "Deathmatch",
      "dm-multicfg": "Deathmatch Multicfg",
      "course": "Course",
      "hns": "Hide N Seek",
      "kz": "Kreedz",
      "minigames": "Mini Games"
    }
  },
  "Votes": {
    "Enabled": false,
    "Map": false,
    "GameMode": false,
    "GameSetting": false,
    "Style": "center"
  },
  "ConfigVersion": 2
}
```
</details>

### Languages
This plugin will display all in-game menus and messaging based on the player's preferred language. Below is an example language configuration file you can customize to your liking. The [CS2-CustomVotes](https://github.com/imi-tat0r/CS2-CustomVotes) plugin also has additional language files you can configure. 

## Logging
>[!WARNING]
> Due to the need to parse map groups and settings, you may have difficulties initially configuring the plugin, especially if your gamemodes_server.txt is not configured properly. All logs associated with this plugin can be found in the below location.
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
To add custom settings, create two configuration files with the enable_ and disable_ prefix (i.e. enable_autobhop.cfg, disable_autobhop.cfg). Then, put those files in the `/csgo/cfg/settings/` folder. This is the default settings folder. You can change this folder in the configuration settings.
</details>
