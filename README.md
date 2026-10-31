![Copyright Nickj609l](https://img.shields.io/badge/Copyright-Nickj609-red) ![GitHub License](https://img.shields.io/github/license/nickj609/GameModeManager) ![GitHub Release](https://img.shields.io/github/v/release/nickj609/GameModeManager) ![Issues](https://img.shields.io/github/issues/nickj609/GameModeManager) ![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/nickj609/GameModeManager/total) ![NuGet Version](https://img.shields.io/nuget/v/GameModeManager.Shared)

# GameModeManager
A Counter-Strike 2 server plugin to help administrators manage custom game modes, settings, and map rotations. 

Inspired by [CS2 Modded Dedicated Server by Kus](https://github.com/kus/cs2-modded-server) and the [CS2 Rock The Vote plugin by Abnerfs](https://github.com/abnerfs/cs2-rockthevote).

## Description
Tired of manually managing game modes, settings, and maps?

GameModeManager simplifies server administration for Counter-Strike 2 by providing:

- **Admin menus** for modes, maps, settings and warmup. If [CS2-SimpleAdmin](https://github.com/daffyyyy/CS2-SimpleAdmin) is installed, its admin menu gets them too (optional).
- **Customizable rotation** of maps and game modes, with mode schedules.
- **Player voting** for settings and game modes.
- **Per-mode map pools** for [GG1MapChooser](https://github.com/ssypchenko/GG1MapChooser): RTV, nominate and map votes only offer the current mode's maps.
- **Shared API** for cross-plugin development and integration.
- **Customization options** for rotation schedules, commands, voting styles, and languages.

This plugin is perfect for servers with a variety of custom content or those that want to give players more control over their experience.
  
## Enjoying the plugin?
Please drop a ⭐ star in the repository

![image](https://github.com/nickj609/GameModeManager/assets/32173425/4c1bef1e-ef13-4a30-b2eb-b02060535bcb)

## Credits
This plugin utilizes the [GameLoop.Vdf library](https://github.com/shravan2x/Gameloop.Vdf/) (licensed under the [MIT License](https://github.com/shravan2x/Gameloop.Vdf/blob/master/LICENSE)) for parsing the `gamemodes_server.txt` file, which is in [Valve Data Format](https://developer.valvesoftware.com/wiki/VDF).

Player votes (`!changemode`, `!changesetting`) are built in since 2.0.0: a PanoramaMenu Yes/No menu, or the game's own F1/F2 vote panel (`Votes.Style: "native"`, adapted from [GG1MapChooser](https://github.com/ssypchenko/GG1MapChooser)'s PanoramaVote, MIT). Earlier versions used the [CS2-CustomVotes](https://github.com/imi-tat0r/CS2-CustomVotes) plugin, which is no longer needed.

Vote settings (`Votes` in `GameModeManager.json`): `Enabled`, `GameModes`, `GameSettings`, `Style` (`menu` | `native`), `Percentage` (yes votes needed, % of eligible players, default 51), `Duration` (seconds, 30), `MinPlayers` (1), `Cooldown` (seconds before the same player can start another vote, 60), `AllowSpectators` (false), `AllowInWarmup` (false), `NativeDisplayToken`. One vote runs at a time, and none starts while a GG1MapChooser map vote is running.

For creating menus, this plugin utilizes the [PanoramaMenuManager](https://github.com/RRimmer/PanoramaMenuManagerCS2) shared plugin API (`MenuManagerApi`, capability `menu:nfcore`). Versions before 2.0.0 used [MenuManagerAPI](https://github.com/Nickj609/MenuManagerAPI).

Lastly, a special thanks to the [CS2 Rock The Vote plugin by Abnerfs](https://github.com/abnerfs/cs2-rockthevote). The plugin's use of the dependency injection framework served as the core architecture for the development of this plugin.

## GG1MapChooser

Since 2.0.0, [GG1MapChooser](https://github.com/ssypchenko/GG1MapChooser) is **required**: it picks the maps, GameModeManager picks the mode and decides when the map changes. GameModeManager 1.0.x had its own RTV and map menus; stay on 1.0.x if you need those.

- GameModeManager keeps **one GG1 map pool per mode** (the mode's map groups) in `cfg/GGMCmaps.tuning.json`. Whenever the mode changes, it writes that mode's pool to GG1's `cfg/GGMCmaps.json` and runs `reloadmaps`, so RTV, nominate and votes only offer that mode's maps. Tune `weight` / `minplayers` / `maxplayers` in the tuning file; they are kept (`MapChooser.GeneratePools`). GG1's own pool vote can't be used with this.
- **Modes in votes:** a few random other modes are added to the pool, reshuffled every map (`MapChooser.ModesInPool`, default 3; `ModeWeight` for how often they're picked; 0 turns it off). They appear in votes and `!nominate` as `Mode:<name>`. When one wins, GameModeManager changes to that mode. If nobody votes, GG1's random pick can also land on one.
- **Maps of the current mode:** players see them with GG1's `!nominate`, admins with this plugin's `!maps` (GG1's own `css_maps` is removed at start-up).
- GG1 provides `!rtv`, `!nominate`, `!nextmap`, `!timeleft`, the end-of-map vote and map extends. The `!game` menu has entries for them.
- GG1 starts the end-of-map vote on round-limited matches. GameModeManager starts it on time-limited matches (`MapChooser.TriggerSecondsBeforeEnd`, default 60) and arms race (`TriggerKillsBeforeEnd`, 13), and again after a map extend.
- At match end, time-limit end or on an empty server, GameModeManager changes the map after its countdown with GG1's `ggmc_change_nextmap` (the voted map, else a random one from the pool), or rotates the mode when it's due. With `Rotation.Enabled: false` it leaves the map alone.

GG1 is not changed: it's used through its map file, commands and public API. A few things reach further (reading whether a next map was picked, the per-mode vote options, removing its `css_maps`); each logs a warning or does nothing if a GG1 update changes it. The zip's GG1 config already has the settings that let only GameModeManager change the map (`ChangeMapAfterWinDraw`, `ChangeMapAfterTimeLimit`, `RandomMapOnStart` and `LastDisconnectedChangeMap` off) and the `wasd` menus the bridge needs. Keep those if you edit it. To change the map by name, use an admin plugin (for example [CS2-SimpleAdmin](https://github.com/daffyyyy/CS2-SimpleAdmin)'s `!map`) or GG1's `ggmap`.

## CS2-SimpleAdmin (optional)

When [CS2-SimpleAdmin](https://github.com/daffyyyy/CS2-SimpleAdmin) is installed, its admin menu gets a **Game Modes** section with the same options as `!modes`, `!maps`, `!settings` and `!warmup`, using its menus and Back button (`AdminPanel.Enabled` to turn it off). Nothing else changes without it.

## Requirements
You need these on your server already, on Windows or Linux:
- [Metamod:Source](https://github.com/alliedmodders/metamod-source/) 2.0
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) **v1.0.369+** (the with-runtime build; GameModeManager 2.0.0+ targets .NET 10, so use 1.0.x on older servers)
- [MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager), installed and turned on. It sends players the Workshop addon that menus need. GameModeManager adds that addon to its list by itself, and your other addons and its config stay as they are (`WorkshopAddon` in `GameModeManager.json`: comma-separated IDs, or empty if you load the addon yourself).

Everything else is **in the release zip**, so copying it to `/csgo/` and restarting the server fully is enough:

| Included | Version | License | Purpose |
| --- | --- | --- | --- |
| [PanoramaMenuManager](https://github.com/RRimmer/PanoramaMenuManagerCS2) | 1.3.00 | GPL v3 | Menus |
| [GG1MapChooser](https://github.com/ssypchenko/GG1MapChooser) | 1.8.1 | MIT | Map votes, RTV, nominate |
| [PMM_GG1MapChooser](https://github.com/RRimmer/PMM-GG1MapChooser) | 0.1.0 | GPL v3 | Draws votes and nominate on PanoramaMenu |

Their licenses are in their folders, and the links above have their source. Their config files are set up to work together.

> [!WARNING]
> Installing replaces these plugins and their config files: `MenuManagerCore.json`, `MenuManager_Modules_Translation.json`, `GG1MapChooser.json` and `PMM_GG1MapChooser.json`. **Back them up first if you've customized them.**

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
| css_mapvote_auto <true\|false>                | Turns the automatic end-of-map vote on or off for the current mode (for mode cfgs).                      |
| css_mapvote_extend <true\|false>              | Offers "extend map" in map votes or not for the current mode (for mode cfgs).                            || css_mapvote_auto <true|false>               | Turns the automatic end-of-map vote on or off for the current mode (for mode cfgs).                     |
| css_mapvote_extend <true|false>             | Offers "extend map" in map votes or not for the current mode (for mode cfgs).                            |
|false> <seconds>         | Enables or disables a time limit for the map. Optionally, you can specify the time limit in seconds.     |

</details>
	
<details>
<summary>Admin Commands</summary>

<br>

| Command                                   | Description                                                                                                            |
| :---------------------------------------- | :--------------------------------------------------------------------------------------------------------------------- |
| !maps                                     | Displays an admin menu to change to one of the current mode's maps or start a map vote.                                |
| !modes                                    | Displays an admin menu to change the game mode or start a vote on one.                                                 |
| !settings                                 | Displays an admin menu to enable or disable custom game settings, or start a vote on one.                              |   
| !mode <mode name>                         | Changes the game mode to the mode specified. For example, for **mg_surf** you would do **!mode surf**.                 |    
| !warmup                                   | Displays an admin menu to start, schedule or end warmup.                                                               |
| !timelimit <true\|false> <seconds>        | Sets the time limit for the current map.                                                                               |
| !setting <enable\|disable> <setting name> | Enables or disables a custom game setting. For example, for **enable_movement_unlock.cfg** you would do **!setting movement_unlock**. |

</details>

<details>
<summary>Player Commands</summary>

<br>

| Command        | Description                                                              |
| :------------- | :----------------------------------------------------------------------- |
| !game          | Menu of player commands, including GG1MapChooser's RTV, Nominate, Next map, Time left. |
| !currentmap    | Displays the current map.                                                |
| !currentmode   | Displays the current game mode.                                          |
| !changemode    | Menu of modes; picking one starts a yes/no vote to change to it.         |
| !changesetting | Menu of settings; pick one, then Enable or Disable, to start a vote.     |

RTV, nominate (the current mode's maps), next map and time left come from GG1MapChooser: `!rtv`, `!nominate`, `!nextmap`, `!timeleft`.

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
