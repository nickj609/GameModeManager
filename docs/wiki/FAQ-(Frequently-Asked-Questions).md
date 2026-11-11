<details>
<summary>How do I add custom settings?</summary>
<br>

To add custom settings, create two configuration files with the `enable_` and `disable_ prefix` (i.e. **enable_autobhop.cfg**, **disable_autobhop.cfg**). Then, put those files in the `/csgo/cfg/settings/` folder. This is the default settings folder. You can change this folder in the configuration settings.

The menus show each setting as on or off. GameModeManager works that out from what the two files set, compared with the server right now:

- **ConVars** like `sv_autobunnyhopping 1`. A ConVar both files set to the same value doesn't count.
- **Plugins** loaded or unloaded with `css_plugins load "plugins/disabled/RollTheDice/RollTheDice.dll"` or `css_plugins unload "Roll The Dice"`.
- Lines inside `exec other.cfg` and `exec_after_delay <seconds> <command>` count too.

Lines it can't check, like `say` and `echo`, are skipped. When neither file matches fully (a disable cfg often also runs shared cfgs the current mode sets its own way), the closer match wins, and a tie shows as off. Only a setting whose files set nothing it can check shows Enable and Disable instead. `css_gmm_status` lists those.


**Optional header.** Comment lines at the top of a setting's enable file (or its disable file when there's no enable file) can change how it shows. The game skips comments, so the file still works as a normal cfg:

```
// name: Bunny hop
// permission: @css/root
// vote: false
sv_autobunnyhopping 1
```

- `name`: the name in menus, instead of one made from the file name.
- `permission`: the flag needed to change it (default `@css/cvar`, like `!settings`).
- `vote: false`: players can't vote on it. Admins can still change it.
- `group: Match`: puts it on a page of its own in the settings menus, with the other settings of that group. Settings without a group are on the first page.
- `applies: next round`, `applies: restart` or `applies: map change`: for a setting the game only reads at that point. Chat says so when it changes, and "Now in effect: ..." when it does. Once the admin closes the menu, so several settings can be changed first, they're asked whether to apply the changes now, unless they already took effect in the meantime. For `restart`: "Changes need a match restart", with Restart now or Restart later. For `next round`: "Changes apply next round", with Replay round (when this round has a backup and the admin may use round backups; it puts money, kills and the timer back to the round's start), End round (ends it as a draw: nobody scores, players keep what they have, and it counts as a round played; not offered in warmup, on the last round, or in one-round modes like surf where it would end the map), Restart match, and Next round. With both kinds changed, only the restart question is asked: a restart covers both. A player vote on it asks "... and restart the match?" and restarts when it passes.

**Settings that leave sv_cheats on.** A setting whose enable cfg ends with `sv_cheats 1` (for a practice or fun setting) gets a confirmation before an admin turns it on, because every player can then use cheat commands like noclip and god mode, and it's kept out of player votes. GameModeManager finds these by reading the cfg; no header is needed. A cfg doesn't need `sv_cheats 1` ... `sv_cheats 0` around ConVars that aren't cheats (most aren't), and a cheat ConVar set that way doesn't stay on: CS2 resets it when `sv_cheats` goes back off.

**Settings with a choice of values.** For a setting like round time, make one cfg per value, without the `enable_`/`disable_` prefix, and give each the same `setting:` name and its own `value:`:

```
// setting: Round time
// value: 5 minutes
mp_roundtime 5
```

Files that share a `setting:` name are one row with a dropdown of their values, in file name order, so name them to sort the way you want them listed (`roundtime_02.cfg`, `roundtime_05.cfg`, `roundtime_10.cfg`). The current value is the file the server matches best. Any of the files can have `permission:` or `vote: false`. A setting needs at least two values. Set one from chat with `!setting set "Round time" "5 minutes"`.

Files in the settings folder without a prefix or a `setting:` header are helpers that other cfgs run, and aren't shown.

**Settings that come with GameModeManager.** The release has these in `cfg/settings/`, each with a header. A server keeps its own files, so updating only adds the ones it doesn't have:

- **Match:** Friendly fire, Overtime, Halftime side swap, Team balance, Respawn, Kick idle players, and the values Round time, Max rounds and Freeze time
- **Economy:** Free armor, Buy anywhere, Buying, and the values Buy time, Start money and Max money
- **Players:** Alltalk, Knife drop, Team collision, Free for all, Spectate enemies, and the value Gravity
- **Movement:** Auto bunny hop, Movement unlock
- **Bots:** Bots, and the values Bot difficulty and Bot count
- **Practice:** Grenade trajectory
- **Fun:** Scoutz

</details>

<details>
<summary>How do I add actions?</summary>
<br>

Actions are cfgs that run once and leave nothing to turn off, like adding a bot or swapping teams. Put them in `/csgo/cfg/actions/` (`Actions.Folder` in the config). Each file is a row in `!actions` and in the admin panel's Actions page, and each subfolder is a group with its own page:

```
cfg/actions/swap_teams.cfg
cfg/actions/bots/add_ct.cfg
cfg/actions/bots/add_t.cfg
cfg/actions/bots/kick_all.cfg
```

The same optional header works here:

```
// name: Add bot CT
// permission: @css/generic
bot_add_ct
```

- `name`: the row's name, instead of one made from the file name.
- `permission`: the flag needed to run it (default `@css/cvar`).
- `vote: true`: players without that flag can start a vote on it, like "Run Swap Teams?".
- `group: Bots`: puts it on a page of its own in `!actions`, with the other actions of that group. A subfolder works too: `actions/bots/add_ct.cfg` is in the Bots group unless its header says otherwise.

**Actions that come with GameModeManager**, in `cfg/actions/`: Swap teams and Scramble teams (Teams); Add bot CT, Add bot T, Kick CT bots, Kick T bots and Kick all bots (Bots). Restarting a round or the game is in CS2-SimpleAdmin's Server Manage menu, so it isn't repeated here.

The page stays open after an action runs, so you can run it several times in a row. Players only see the actions they can run or vote on.

The Actions pages also have a built-in Rounds page, for admins with `@css/generic` (`Actions.RoundsPermission`):

- **Pause match**: one row that shows the match's state. CS2 only stops a match in freeze time, so mid-round it says "Pause at next round" (pick it again to cancel), then "Unpause match".
- **CT timeout** and **T timeout**, greyed out while a pause or a timeout is running or waiting, so they can't be stacked.
- **Replay this round**, **Replay last round** and **Go back to round** (this map's rounds). They use the game's round backups, so the match goes back to the start of that round with its scores and everyone's money, and the game pauses it until an admin unpauses.

Turn the page off with `Actions.Rounds: false`.

A setting whose files set no ConVar and load no plugin has no on/off state, so GameModeManager logs a warning for it at startup: if it runs something once, it belongs in the actions folder.
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
