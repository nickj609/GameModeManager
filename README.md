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
- **Shared API** for cross-plugin development and integration.
- **Built-in RTV** that can be customized to include modes and maps.
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

Lastly, a special thanks to the [CS2 Rock The Vote plugin by Abnerfs](https://github.com/abnerfs/cs2-rockthevote). The plugin's use of the dependency injection framework served as the core architecture for the development of this plugin.

## Requirements
- [Counter-Strike 2](https://www.counter-strike.net/cs2)
- [Metamod:Source](https://github.com/alliedmodders/metamod-source/) (v1282+)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) (v.197+)

## Built-in RTV Plugin
This plugin comes with a built-in RTV plugin that can be customized to include maps and modes. The built-in RTV plugin is enabled by default.

![Untitled2](https://github.com/user-attachments/assets/e02c37b6-eadf-4a14-ba4f-6958f1d44a7e)

## Commands
<details>
<summary>Server Commands</summary>
	
<br>

| Command                                       | Description                                                                                              |
| :-------------------------------------------- | :------------------------------------------------------------------------------------------------------- |
| css_gamemode <mode>                           | Sets the current mode.                                                                                   |
| css_warmupmode <mode>                         | Schedules and sets the game mode to be used during the warmup period.                                    |
| css_endwarmup                                 | Ends the custom warmup mode, transitioning to the scheduled or default game mode.                        |
| css_startwarmup <mode>                        | Immediately starts a custom warmup mode with the specified mode.                                         |
| css_timelimit <true\|false> <seconds>         | Enables or disables a time limit for the map. Optionally, you can specify the time limit in seconds.     |
| css_rtv_extend <true\|false>                  | Enables or disables extending the current map.                                                           |
| css_rtv_enabled <true\|false>                 | Enables or disables the Rock the Vote (RTV) system.                                                      |
| css_rtv_duration <seconds>                    | Sets the duration of an RTV vote in seconds.                                                             |
| css_rtv_max_extends <extends>                 | Sets the maximum number of times a map can be extended via RTV.                                          |
| css_rtv_end_of_map_vote <true\|false>         | Enables or disables a vote to change the map at the end of the current map.                              |
| css_rtv_rounds_before_end <rounds>            | Sets the number of rounds remaining before an end-of-map vote can be initiated.                          |
| css_rtv_seconds_before_end <seconds>          | Sets the number of seconds remaining before an end-of-map vote can be initiated.                         |
| css_rtv_start_vote <duration> <true\|false>   | Starts an RTV vote with a specified duration. You can immediately change the map or game mode.           |

</details>
	
<details>
<summary>Admin Commands</summary>

<br>

| Command                                   | Description                                                                                                            |
| :---------------------------------------- | :--------------------------------------------------------------------------------------------------------------------- |
| !maps                                     | Displays an admin menu for changing the map.                                                                           |
| !modes                                    | Displays an admin menu for changing the game mode.                                                                     |
| !settings                                 | Displays an admin menu for enabling or disabling custom game settings.                                                 |   
| !mode <mode name>                         | Changes the game mode to the mode specified. For example, for **mg_surf** you would do **!mode surf**.                 |    
| !map <map name> <workshop id>             | Changes the map to the map specified. The workshop ID is optional.                                                     |                                                                                                                                         
| !timelimit <true\|false> <seconds>        | Sets the time limit for the current map.                                                                               |
| !setting <enable\|disable> <setting name> | Enables or disables a custom game setting. For example, for **enable_movement_unlock.cfg** you would do **!setting movement_unlock**. |

</details>

<details>
<summary>Player Commands</summary>

<br>

| Command        | Description                                                              |
| :------------- | :----------------------------------------------------------------------- |
| !rtv           | Rocks the vote!                                                          |
| !game          | Displays a **dynamic** menu of all player commands.                      |
| !nominate      | Nominates a map or game mode for the RTV vote.                           |
| !nextmap       | Displays the next map.                                                   |
| !nextmode      | Displays the next mode.                                                  |
| !timeleft      | Displays the time left in the current map.                               |
| !currentmap    | Displays the current map.                                                |
| !changemap     | Displays a **dynamic** menu of all per-map votes that can be created.    |
| !currentmode   | Displays the current game mode.                                          |
| !changemode    | Displays a menu of all per-mode votes that can be created.               |
| !changesetting | Displays a menu of all per-setting votes that can be created.            |

</details>

# Getting Started
> [!IMPORTANT]
> If this is your first time setting up a modded server, I highly suggest checking out the [CS2-Modded-Server Repo by Kus](https://github.com/kus/cs2-modded-server). It's a great resource for understanding how to set up custom game modes, which aren't included with this plugin. Additionally, it includes a preconfigured version of this plugin for beginners.

To get started, make sure all of the following prerequisites are met:

- All of your mode config files are located in `/csgo/cfg/`.
- Both [Metamode:Source](https://github.com/alliedmodders/metamod-source/) and [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) are installed.
- Your `gamemodes_server.txt` or custom map group file is in [VDF Format](https://developer.valvesoftware.com/wiki/VDF) and contains a list of map groups.

## Installation
1. Download the [Latest Release](https://github.com/nickj609/GameModeManager/releases/latest). 
2. Copy the `addons` and `cfg` folders to `/csgo/`.
3. Update each game mode configuration file (i.e. comp.cfg) to include `css_gamemode <mode>`.
4. After the first run, update the configuration file `GameModeManager.json`.

For more information about the configuration of this plugin and the use of the [Shared API](https://github.com/nickj609/GameModeManager/wiki/Shared-API), see our [Wiki](https://github.com/nickj609/GameModeManager/wiki).

# Need Help?
If you have a question, check out our [FAQ](https://github.com/nickj609/GameModeManager/wiki/FAQ-(Frequently-Asked-Questions)) and if you still need help, [create a new issue](https://github.com/nickj609/GameModeManager/issues/new/choose). 
