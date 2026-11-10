<details>
<summary>How do I add custom settings?</summary>
<br>

To add custom settings, create two configuration files with the `enable_` and `disable_ prefix` (i.e. **enable_autobhop.cfg**, **disable_autobhop.cfg**). Then, put those files in the `/csgo/cfg/settings/` folder. This is the default settings folder. You can change this folder in the configuration settings.

The menus show each setting as on or off. GameModeManager works that out from what the two files set, compared with the server right now:

- **ConVars** like `sv_autobunnyhopping 1`. A ConVar both files set to the same value doesn't count.
- **Plugins** loaded or unloaded with `css_plugins load "plugins/disabled/RollTheDice/RollTheDice.dll"` or `css_plugins unload "Roll The Dice"`.
- Lines inside `exec other.cfg` and `exec_after_delay <seconds> <command>` count too.

Lines it can't check, like `say` and `echo`, are skipped. When neither file matches fully (a disable cfg often also runs shared cfgs the current mode sets its own way), the closer match wins, and a tie shows as off. Only a setting whose files set nothing it can check shows Enable and Disable instead. `css_gmm_status` lists those.

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
      "Config": "ar.cfg",
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
        "mg_Casual-1.6"
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
<summary>Something isn't working. What should I check first?</summary>
<br>

Run `css_gmm_status` in the server console (2.1.0 and later). It shows the plugins GameModeManager found, the current map list, the Workshop addon and the main settings. Anything in capitals, or a line saying a plugin no longer has something, is the place to start. Please include the output in bug reports.

</details>

<details>
<summary>Why are game mode and map rotations not working?</summary>
<br>

Check that `Rotation.Enabled` is on, because GameModeManager leaves the map alone at the end of a match when it's off. Mode rotation also needs `Rotation.ModeRotation`. Admins can check and change both on the **Rotation** page of CS2-SimpleAdmin's menu, if it's installed.

</details>

<details>
<summary>How are friendly names generated for settings?</summary>
<br>

Friendly names for settings are generated by removing the extension and underscores, and capitalizing the first letter of each word. For example, **enable_movement_unlock.cfg** turns into **Movement Unlock**.

</details>

<details>
<summary>How are vote commands created?</summary>
<br>

`!changemap`, `!changemode` and `!changesetting` open a menu to pick from. Add a name to vote on it straight away, like `!changemode Gun Game`.

</details>

<details>
<summary>Can I use friendly names for maps?</summary>
<br>

Yes! You can use friendly names for maps by using the following syntax:

```vdf
"mg_active"
{
	"imagename"			"mapgroup-active"
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
<br>

> Can't find an answer to your question? <br>
> [Create a New Issue](https://github.com/nickj609/GameModeManager/issues/new/choose)
