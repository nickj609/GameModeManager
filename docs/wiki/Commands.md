GameModeManager has several server-side and client-side commands available.

> [!NOTE]
> Some commands listed below might be disabled according to your [Configuration](https://github.com/nickj609/GameModeManager/wiki/Configuration) settings.
>


# Server Commands
The commands below can be run by the server, such as within your mode configuration files. 

| Command                                       | Description                                                                                              |
| :-------------------------------------------- | :------------------------------------------------------------------------------------------------------- |
| css_gamemode <mode>                           | Sets the current mode.                                                                                   |
| css_warmupmode <mode>                         | Schedules and sets the game mode to be used during the warmup period.                                    |
| css_endwarmup                                 | Ends the custom warmup mode, transitioning to the scheduled or default game mode.                        |
| css_startwarmup <mode>                        | Immediately starts a custom warmup mode with the specified mode.                                         |
| css_timelimit <true\|false> <seconds>         | Enables or disables a time limit for the map. Optionally, you can specify the time limit in seconds.     |
| css_mapvote_auto <true\|false>                | Turns the automatic end-of-map vote on or off for the current mode.                                      |
| css_mapvote_extend <true\|false>              | Offers "extend map" in map votes or not for the current mode.                                            |
| css_mapvote_rtv <true\|false>                 | Turns `!rtv` and `!nominate` on or off for the current mode. Put it in the mode's `<mode>_settings.cfg`. A new mode starts with it on. Added in 2.1.0. |
| css_rtv_end_of_map_vote <true\|false>         | Same as css_mapvote_auto, for mode configs written for 1.0.x.                                            |
| css_rtv_extend <true\|false>                  | Same as css_mapvote_extend, for mode configs written for 1.0.x.                                          |
| css_rtv_start_vote                            | Starts the end-of-map vote.                                                                              |
| css_gmm_status                                | Shows what GameModeManager found on the server: plugins, map list, Workshop addon and settings. Include it in bug reports. Added in 2.1.0. |
	
# Admin Commands
`!maps`, `!mode` and `!modes` need _@css/changemap_ permissions. The others need _@css/cvar_.

| Command                                   | Description                                                                                                            |
| :---------------------------------------- | :--------------------------------------------------------------------------------------------------------------------- |
| !maps                                     | Displays an admin menu to change to one of the current mode's maps or start a map vote.                                |
| !modes                                    | Displays an admin menu to change the game mode or start a vote on one.                                                 |
| !settings                                 | Displays an admin menu of custom game settings, each shown as on or off: pick one to turn it the other way, or start a vote on that.                              |
| !mode <mode name> [map]                   | Changes the game mode to the mode specified. For example, for **mg_surf** you would do **!mode surf**. Add a map from that mode's list to start on it, like **!mode surf surf_beginner**. |
| !warmup                                   | Displays an admin menu to start, schedule or end warmup.                                                               |
| !timelimit <true\|false> <seconds>        | Sets the time limit for the current map.                                                                               |
| !setting <enable\
| !setting set <setting name> <value> | Sets a value setting, like **!setting set "Round time" "5 minutes"**. See the FAQ, "How do I add custom settings?" ||disable> <setting name> | Enables or disables a custom game setting. For example, for **enable_movement_unlock.cfg** you would do **!setting movement_unlock**. |

To change to any map by name, use an admin plugin like [CS2-SimpleAdmin](https://github.com/daffyyyy/CS2-SimpleAdmin)'s `!map`, or GG1MapChooser's `ggmap`.

With CS2-SimpleAdmin installed, its admin menu also has a **Manage game** section. Its **Rotation** page turns map rotation, mode rotation, and RTV and nominate on or off until the server restarts.

# Player Commands
Player commands are commands that standard players with no administrative rights can use. 

| Command        | Description                                                                                  |
| :------------- | :------------------------------------------------------------------------------------------- |
| !game          | Displays a menu of player commands. Includes Language, to pick the language every plugin shows you in (like !lang). |
| !menu          | Chooses your menu style.                                                                     |
| !currentmap    | Displays the current map.                                                                    |
| !currentmode   | Displays the current game mode.                                                              |
| !changemap     | Displays a menu of the current mode's maps. Picking one starts a yes/no vote to change to it. |
| !changemode    | Displays a menu of modes. Picking one starts a yes/no vote to change to it.                  |
| !changesetting | Displays a menu of settings, each shown as on or off. Pick one to start a vote on turning it the other way.              |
| !actions | Menu of actions: replaying a round, and the cfgs in `cfg/actions/`, like adding a bot. Admins run them; players see only the ones they can run or vote on. |

These come from [GG1MapChooser](https://github.com/ssypchenko/GG1MapChooser), which is included with GameModeManager:

| Command        | Description                                                                                  |
| :------------- | :------------------------------------------------------------------------------------------- |
| !rtv           | Rocks the vote! `MapChooser.Rtv` turns this and `!nominate` off.                                           |
| !nominate      | Nominates a map or game mode for the next vote.                                              |
| !nextmap       | Displays the next map.                                                                       |
| !timeleft      | Displays the time left in the current map.                                                   |
