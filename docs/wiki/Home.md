> [!IMPORTANT]
> If this is your first time setting up a modded server, I highly suggest checking out the [CS2-Modded-Server Repo by Kus](https://github.com/kus/cs2-modded-server). It's a great resource for understanding how to set up custom game modes, which aren't included with this plugin. Additionally, it includes a preconfigured version of this plugin for beginners.

# Getting Started
To get started, make sure all of the following prerequsuites are met:

- All of your mode config files are located in `/csgo/cfg/`.
- Both [Metamode:Source](https://github.com/alliedmodders/metamod-source/) and [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) are installed.
- Your `gamemodes_server.txt` or custom map group file is in [VDF Format](https://developer.valvesoftware.com/wiki/VDF) and contains a list of map groups.
   
## Installation
1. Extract and copy `addons` and `cfg` folders to `/csgo/`.
2. Update each game mode configuration file (i.e. comp.cfg) to include `css_gamemode <mode>`.
3. After the first run, update the configuration file `GameModeManager.json`.

For more information regarding the configuration of this plugin, see [Configuration](https://github.com/nickj609/GameModeManager/wiki/Configuration), [Languages](https://github.com/nickj609/GameModeManager/wiki/Languages), and [Shared API](https://github.com/nickj609/GameModeManager/wiki/Shared-API).

# Need Help?
If you have a question, check out our [FAQ](https://github.com/nickj609/GameModeManager/wiki/FAQ-(Frequently-Asked-Questions)) and if you still need help, [create a new issue](https://github.com/nickj609/GameModeManager/issues/new/choose). 