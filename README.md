# Save Anywhere Redux

Save Anywhere Made Much More Stable!

#### Updated for SDV 1.5!

Compatible with Stardew Valley 1.5+ on Linux, Mac, and Windows. Requires SMAPI 3.0 or later.

### Install

[Install the latest version of SMAPI.](https://smapi.io/)

Download this mod and unzip it into Stardew Valley/Mods.

Run the game using SMAPI.

### How To Use

Press K to save anywhere. Edit the config.json file in a text editor to change the key (it will appear once you run the
game), if you have GMCM use it instead.

#### Created by Omegasis, Aredjay, and RealSweetPanda

### Changelog

#### 3.5.0

- **Fixed villagers freezing or walking through walls/water after loading a mid-day save.** The game precomputes each
  NPC's daily route from their morning position and replays it without collision checks; after a mid-day reload NPCs
  were not where those routes expected, which froze them or sent them cutting through walls, water, and map edges
  (and could make them vanish into unreachable spots). Villagers now get a fresh path to their current schedule
  destination computed from the tile they actually stand on, using the game's own schedule pathfinding, and
  villagers who already reached their destination resume their idle behavior (reading, standing at the counter,
  etc.) instead of standing blank.
- **Fixed monster health resetting on reload — this was letting players save-scum out of fights.** The mod was
  stripping every monster from the world before each mid-day save; the game's own save code never touches monsters,
  so this was purely the mod deleting them from the file. Reloading meant any monster you were fighting came back
  at full health or vanished entirely. Vanilla monsters now save and reload with their current health. Monster
  types added by other mods are still excluded from the save file (some aren't safe to serialize), so they behave
  as before.
- **Fixed the player sometimes spawning inside solid rock in mines and the Volcano Dungeon.** Those floors are
  randomly regenerated every time you enter them and were never part of the save file to begin with, so reloading
  mid-mine puts you in a brand-new layout — your saved spot could now be a wall. The mod checks for this and moves
  you to the nearest open tile using the same recovery the game uses for its own stale warp points.
- **Dropped items no longer disappear on reload.** Something you just chopped, looted, or knocked loose that's
  still sitting on the ground used to vanish, because the game doesn't normally expect the ground to have anything
  on it when a save happens. Single-item drops (loot, quest items, tools/weapons, unpicked forage) are now
  preserved. Loose resource chunks from a tool swing (wood/stone/coal not yet walked over) aren't — just re-swing
  the same node.
- **Fixed saving changing tomorrow's weather** (e.g. deleting rain, or corrupting the forecast when saving on a
  festival day).
- **Fixed multiplayer hang:** mid-day saving with farmhands connected froze the game on the saving screen forever;
  the mod now blocks the save with a clear message instead.
- Fixed a crash (`ArgumentException`) on the second mid-day save of a session in some cases.
- Fixed a possible crash on load if the mid-day save data was missing or corrupted.
- Farmhands connecting to a host no longer get warped to the host's saved position.
- Hidden off-map NPCs (e.g. Marlon) are no longer saved/restored.
- Removed a redundant double screen-fade when loading a mid-day save.
- Updated build tooling for current SMAPI (no deprecated APIs).

### Notes

The mod should now work with map extension mods, modded characters, modded items, etc in the newest update. If it
doesn't, report it and we can fix it.

As much as we want to make the mod stable it can always This mod might crash and cause issues. just be aware and if it
occurs just report it and we will fix it!

