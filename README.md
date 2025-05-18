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

- `css_gamemode <mode>` - Sets the current mode.

- `css_rtv_extend <true|false>` - Enables or disables extending map.

- `css_rtv_enabled <true|false>` - Enables or disables RTV.

- `css_rtv_duration <seconds>` - Sets the RTV vote duration.

- `css_rtv_max_extends <extends>` - Sets the max number of map extends.

- `css_rtv_end_of_map_vote <true|false>` - Enables or disables end of map vote.

- `css_rtv_rounds_before_end <rounds>` - Sets the rounds before the end of map vote.

- `css_rtv_seconds_before_end <seconds>` - Sets the seconds before the end of map vote.

- `css_rtv_start_vote <duration> <true|false>` - Starts rtv vote, where true or false determines if map or mode changes immediately. 

- `css_warmupmode <mode>` - Schedules and sets the warmup mode.

- `css_endwarmup` - Ends the custom warmup mode.

- `css_startwarmup <mode>` - Starts the custom warmup mode.

- `css_timelimit <true|false> optional: <seconds>` - Enables or disables time limit.

</details>
	
<details>
<summary>Admin Commands</summary>

- `!timelimit <true|false|> optional: <seconds>` - Sets the time limit for the current map.

   ![Screenshot 2024-09-24 171240](https://github.com/user-attachments/assets/1d91ad0e-cf4e-4c87-b221-b36806c0ffe5) 

- `!maps` - Displays an admin menu for changing the map. 

   > _Depending on map mode, it shows maps for the current game mode or all modes._

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

- `!rtv` - Rocks the vote!

  ![Untitled3](https://github.com/user-attachments/assets/e2515257-517a-48c9-b17c-071a8c14fc98)


- `!nominate <map|mode>` - Nominates a map or game mode for the RTV vote.

   ![Untitled](https://github.com/user-attachments/assets/7cf31114-d36f-44bb-bc9a-ffea6fbebd77)

- `!game` - Displays a **dynamic** menu of all player commands.

   ![image](https://github.com/user-attachments/assets/5f45876d-7c4b-45a7-95fe-fc96f0dae57f)

- `!nextmap` - Displays the next map. 

- `!nextmode` - Displays the next mode. 

- `!currentmap` - Displays the current map. 

   ![Screenshot 2024-06-15 202240](https://github.com/nickj609/GameModeManager/assets/32173425/1d9f9e12-2320-4ab8-a021-c4a5477e533a)

- `!changemap` - Displays a **dynamic** menu of all per-map votes that can be created.

  > _Depending on map mode, it shows maps for the current game mode or all modes._

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
1. Install [Metamod:Source](https://github.com/alliedmodders/metamod-source/) and [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp).
2. Copy `addons` and `cfg` folders to `/csgo/`.
3. Make sure your `gamemodes_server.txt` or custom map group file is in [VDF Format](https://developer.valvesoftware.com/wiki/VDF) and contains a list of map groups.
4. Update each game mode configuration file (i.e. comp.cfg) to include `css_gamemode <mode>`.
5. After the first run, update the configuration file `GameModeManager.json`.

For more information about the configuration of this plugin and the use of the [Shared API](https://github.com/nickj609/GameModeManager/wiki/Shared-API), see our [Wiki](https://github.com/nickj609/GameModeManager/wiki).

# Need Help?
If you have a question, check out our [FAQ](https://github.com/nickj609/GameModeManager/wiki/FAQ-(Frequently-Asked-Questions)) and if you still need help, [create a new issue](https://github.com/nickj609/GameModeManager/issues/new/choose). 