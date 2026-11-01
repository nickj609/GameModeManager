>[!WARNING]
> Due to the need to parse map groups and settings, you may have difficulties initially configuring the plugin, especially if your **map group file** is not configured properly. All logs associated with this plugin can be found in the below location.
> 
> `csgo/addons/counterstrikesharp/logs`

# Example Log

```log
2024-06-08 22:59:32.827 +00:00 [WARN] plugin:GameModeManager Skipping random_setting.cfg because its missing the correct prefix.
2024-06-08 23:05:24.421 +00:00 [INFO] plugin:GameModeManager Current mode is Deathmatch.
2024-06-08 23:05:24.421 +00:00 [INFO] plugin:GameModeManager New mode is Casual.
2024-11-17 23:05:24.532 +00:00 [INFO] plugin:GameModeManager Warmup mode enabled.
2024-06-08 24:15:47.044 +00:00 [INFO] plugin:GameModeManager Game has ended. Picking random map from current mode...
```

# Common Error Messages

| Error/Warning Message                                              | Description                                                                                                              |
| -------------------------------------------------------------------| ------------------------------------------------------------------------------------------------------------------------ | 
| `Cannot Find`                                                      | Cannot locate the file specified from `GameModeManager.json` config.                                                  | 
| `Incomplete VDF data`                                              | Your `gamemodes_server.txt` file is not formatted properly in [VDF Format](https://developer.valvesoftware.com/wiki/VDF).| 
| `Your config file is too old`                                      | Please backup and remove it from `addons/counterstrikesharp/configs/plugins/GameModeManager` to recreate it.             |
| `The mapgroup property doesn't exist`                              | The "mapgroup" property cannot be found in your `gamemodes_server.txt` file.                                             | 
| `Mapgroup found, but the 'maps' property is missing or incomplete` | The "maps" property cannot be found in your `gamemodes_server.txt` file for one of your map groups.                      | 
