using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using SaveAnywhere.Framework.Model;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace SaveAnywhere.Framework
{
    public class SaveManager
    {
        public static event EventHandler SaveComplete;
        private readonly IModHelper _helper;
        private readonly Action _onLoaded;
        public readonly Dictionary<string, Action> AfterCustomSavingCompleted;
        public readonly Dictionary<string, Action> AfterSaveLoaded;
        public readonly Dictionary<string, Action> BeforeCustomSavingBegins;
        private static SaveGameMenu _currentSaveMenu;
        private bool _waitingToSave;
        private static bool _middaysaving;
        private bool _sawSaveInProgress;

        public bool IsSaving => _waitingToSave || _middaysaving;

        public SaveManager(IModHelper helper, Action onLoaded)
        {
            _helper = helper;
            _onLoaded = onLoaded;
            BeforeCustomSavingBegins = new Dictionary<string, Action>();
            AfterCustomSavingCompleted = new Dictionary<string, Action>();
            AfterSaveLoaded = new Dictionary<string, Action>();
        }

        private string RelativeDataPath => Path.Combine("data", Constants.SaveFolderName + ".json");

        public event EventHandler BeforeSave;

        public event EventHandler AfterSave;

        public event EventHandler AfterLoad;


        public void Update()
        {
            // SaveGameMenu holds a ~1.5s "has been saved" sparkle-text animation
            // after the actual file write finishes — it's vanilla's end-of-night
            // screen, and looks like a full day transition even though the clock
            // never advances for a mid-day save. The real write is done the moment
            // Game1.game1.IsSaving flips back to false (SaveGameMenu sets that
            // itself); at that point we set its public `quit` flag early — the
            // same externally-settable field vanilla itself uses to close the menu
            // when a client times out — so its own update() closes it on the very
            // next tick instead of waiting out the decorative hold. This doesn't
            // touch the actual save/cleanup logic at all, just skips the wait.
            if (_currentSaveMenu != null)
            {
                if (Game1.game1.IsSaving)
                    _sawSaveInProgress = true;
                else if (_sawSaveInProgress)
                {
                    _sawSaveInProgress = false;
                    _currentSaveMenu.quit = true;
                }
            }

            if (!_waitingToSave || Game1.activeClickableMenu != null)
                return;
            Game1.newDaySync = new NewDaySynchronizer();
            Game1.newDaySync.start();
            // NOTE: do NOT touch Game1.weatherForTomorrow here. It was already rolled
            // by last night's real day transition; getWeatherModificationsForDate is
            // only valid during that transition (when Date is the day being entered).
            // Calling it with today's date forced tomorrow to Sun on the 1st of a
            // month / early game (deleting rain forecasts) and to "Festival" on
            // festival days.
            _currentSaveMenu = new SaveGameMenu();
            SaveComplete += CurrentSaveMenu_SaveComplete;
            Game1.activeClickableMenu = _currentSaveMenu;
            _waitingToSave = false;
        }

        private void CurrentSaveMenu_SaveComplete(object sender, EventArgs e)
        {
            SaveComplete -= CurrentSaveMenu_SaveComplete;
            _currentSaveMenu = null;
            _sawSaveInProgress = false;
            SaveAnywhere.Instance.RestoreMonsters();
            AfterSave?.Invoke(this, EventArgs.Empty);
            foreach (var keyValuePair in AfterCustomSavingCompleted)
                keyValuePair.Value?.Invoke();
        }


        public void ClearData()
        {
            if (File.Exists(Path.Combine(_helper.DirectoryPath, RelativeDataPath)))
                File.Delete(Path.Combine(_helper.DirectoryPath, RelativeDataPath));
            _helper.Data.WriteSaveData<PlayerData>("midday-save", null);
            RemoveLegacyDataForThisPlayer();
        }

        public bool saveDataExists()
        {
            return File.Exists(Path.Combine(_helper.DirectoryPath, RelativeDataPath)) ||
                   _helper.Data.ReadSaveData<PlayerData>("midday-save") != null;
        }

        public void BeginSaveData()
        {
            if (IsSaving) return; // guard re-entrancy

            BeforeSave?.Invoke(this, EventArgs.Empty);
            foreach (var customSavingBegin in BeforeCustomSavingBegins)
                customSavingBegin.Value?.Invoke();


            var farm = Game1.getFarm();
            // hnnnnnnnnnnnnnnnnnnn
            // var drink = Game1.buffsDisplay.GetSortedBuffs();
            // BuffData data = null;
            //
            // if (drink != null)
            //     drinkdata = new BuffData(drink.displaySource, drink.source, drink.millisecondsDuration,
            //         drink.buffAttributes);

            // var food = Game1.buffsDisplay.food;
            BuffData drinkdata = null;

            BuffData fooddata = null;
            // if (food != null)
            //     fooddata = new BuffData(food.displaySource, food.source, food.millisecondsDuration,
            //         food.buffAttributes);

            _helper.Data.WriteSaveData("midday-save", new PlayerData
            {
                Time = Game1.timeOfDay,
                OtherBuffs = null,
                DrinkBuff = drinkdata,
                FoodBuff = fooddata,
                Position = GetPosition().ToArray(),
                IsCharacterSwimming = Game1.player.swimming.Value,
                Debris = GetDebris()
            });
            var tempShippingBin = new Chest(true, new Vector2(-100, -100));
            foreach (var item in farm.getShippingBin(Game1.player))
            {
                item.modData["farmerSelling"] = Game1.player.UniqueMultiplayerID.ToString();
                if (item == Game1.getFarm().lastItemShipped)
                    item.modData["last"] = "true";
                else
                    item.modData["last"] = "false";
                tempShippingBin.addItem(item);
            }

            SaveAnywhere.Instance.cleanMonsters();
            Game1.getFarm().setObject(new Vector2(-100, -100), tempShippingBin);
            Game1.getFarm().getObjectAtTile(-100, -100).modData["Pathoschild.ChestsAnywhere/IsIgnored"] = "true";
            Game1.activeClickableMenu = new ShippingMenu(Game1.getFarm().getShippingBin(Game1.player));
            _waitingToSave = true;
            _middaysaving = true;
            RemoveLegacyDataForThisPlayer();
        }

        public void LoadData()
        {
            var data = _helper.Data.ReadSaveData<PlayerData>("midday-save") ??
                       _helper.Data.ReadJsonFile<PlayerData>(RelativeDataPath);
            if (data == null)
            {
                ClearData();
                return;
            }

            // Restore the shipping bin from the temp chest (if it survived the save).
            var tempShippingBin = Game1.getFarm().getObjectAtTile(-100, -100) as Chest;
            if (tempShippingBin != null)
            {
                foreach (var item in tempShippingBin.Items)
                {
                    if (item == null) continue;
                    if (item.modData["farmerSelling"] == Game1.player.UniqueMultiplayerID.ToString())
                    {
                        if (item.modData["last"] == "true")
                            Game1.getFarm().lastItemShipped = item;
                        Game1.getFarm().getShippingBin(Game1.player).Add(item);
                    }
                }
                Game1.getFarm().removeObject(new Vector2(-100, -100), false);
            }

            SetPositions(data.Position, data.Time);
            RestoreDebris(data.Debris);
            // if (data.OtherBuffs != null)
            // foreach (var buff in data.OtherBuffs)
            // {
            //     var atts = buff.Attributes;
            //     Game1.buffsDisplay.addOtherBuff(new Buff(atts[0],
            //         atts[1],
            //         atts[2],
            //         atts[3],
            //         atts[4],
            //         atts[5],
            //         atts[6],
            //         atts[7],
            //         atts[8],
            //         atts[9],
            //         atts[10],
            //         atts[11],
            //         buff.MillisecondsDuration * 10 / 7000,
            //         buff.Source,
            //         buff.DisplaySource));
            // }

            var datadrink = data.DrinkBuff;
            var datafood = data.FoodBuff;

            // if (datadrink != null)
            // {
            //     var atts = datadrink.Attributes;
            //     Game1.buffsDisplay.tryToAddDrinkBuff(new Buff(atts[0],
            //         atts[1],
            //         atts[2],
            //         atts[3],
            //         atts[4],
            //         atts[5],
            //         atts[6],
            //         atts[7],
            //         atts[8],
            //         atts[9],
            //         atts[10],
            //         atts[11],
            //         datadrink.MillisecondsDuration * 10 / 7000,
            //         datadrink.Source,
            //         datadrink.DisplaySource));
            // }
            //
            // if (datafood != null)
            // {
            //     var atts = datafood.Attributes;
            //     Game1.buffsDisplay.tryToAddFoodBuff(new Buff(atts[0],
            //         atts[1],
            //         atts[2],
            //         atts[3],
            //         atts[4],
            //         atts[5],
            //         atts[6],
            //         atts[7],
            //         atts[8],
            //         atts[9],
            //         atts[10],
            //         atts[11],
            //         datafood.MillisecondsDuration * 10 / 7000,
            //         datafood.Source,
            //         datafood.DisplaySource), datafood.MillisecondsDuration);
            // }

            ResumeSwimming(data);
            _onLoaded?.Invoke();
            AfterLoad?.Invoke(this, EventArgs.Empty);
            foreach (var keyValuePair in AfterSaveLoaded)
                keyValuePair.Value?.Invoke();
        }

        private void ResumeSwimming(PlayerData data)
        {
            try
            {
                if (!data.IsCharacterSwimming)
                    return;
                Game1.player.changeIntoSwimsuit();
                Game1.player.swimming.Value = true;
            }
            catch
            {
                // ignored
            }
        }

        private IEnumerable<BuffData> GetotherBuffs()
        {
            // new BuffEffects().
            // foreach (var buff in Game1.player.buffs.AppliedBuffs)
            //     yield return new BuffData(buff., buff.source, buff.millisecondsDuration,
            //         buff.);
            return null;
        }

        private IEnumerable<PositionData> GetPosition()
        {
            var player = Game1.player;
            var name1 = player.Name + "FARMERTAGSA";
            var map1 = player.currentLocation.uniqueName.Value;
            if (string.IsNullOrEmpty(map1))
                map1 = player.currentLocation.Name;
            var tile1 = player.TilePoint;
            int facingDirection1 = player.FacingDirection;
            yield return new PositionData(name1, map1, tile1.X, tile1.Y, facingDirection1);

            foreach (var npc in Utility.getAllCharacters())
            {
                if (npc?.currentLocation == null) continue;
                // The game parks some hidden NPCs off-map (e.g. Marlon at -42,-42);
                // don't record or restore those.
                if (npc.TilePoint.X < 0 || npc.TilePoint.Y < 0) continue;

                var npcMap = npc.currentLocation.uniqueName.Value;
                if (string.IsNullOrEmpty(npcMap))
                    npcMap = npc.currentLocation.Name;

                yield return new PositionData(npc.Name, npcMap,
                    npc.TilePoint.X, npc.TilePoint.Y, npc.FacingDirection);
            }
        }

        private void SetPositions(PositionData[] position, int time)
        {
            if (position == null || position.Length == 0) return;

            var log = SaveAnywhere.Instance.Monitor;

            // Restore player first (warpFarmer handles its own screen transition).
            var playerPos = position[0];
            int px = playerPos.X, py = playerPos.Y;

            // Mine/Volcano Dungeon levels are never part of the save file (they
            // live only in a runtime cache, never added to Game1.locations), so
            // reloading into one always regenerates a brand-new random layout.
            // The saved tile can be solid rock in the new layout. Resolve the
            // target location BEFORE warping (warpFarmer's location change is
            // applied on a later tick, so currentLocation can't be trusted right
            // after the call) and nudge to the nearest open tile if needed — the
            // same tool the game itself uses to recover a stale mail/warp target.
            // Short-circuits instantly when the saved tile is already open, so
            // it's cheap to run unconditionally, not just for regenerated levels.
            var targetLoc = Game1.getLocationFromName(playerPos.Map);
            if (targetLoc != null)
            {
                var open = Utility.recursiveFindOpenTileForCharacter(
                    Game1.player, targetLoc, new Vector2(px, py), 20, allowOffMap: false);
                if (open != Vector2.Zero)
                {
                    px = (int)open.X;
                    py = (int)open.Y;
                }
                else
                {
                    log.Log($"[SA] No open tile near ({playerPos.X},{playerPos.Y}) in {playerPos.Map}; using saved tile",
                            LogLevel.Warn);
                }
            }

            Game1.warpFarmer(playerPos.Map, px, py, playerPos.FacingDirection);

            // Restore each villager's saved tile and clear stale movement state from
            // the vanilla morning load BEFORE the clock moves. checkSchedule() runs on
            // every 10-minute tick and fires the day's precomputed routes; those route
            // points assume the NPC walked there from its morning position, and an NPC
            // standing anywhere else beelines to the first point straight through
            // walls (PathFindController does no collision checks). ignoreScheduleToday
            // makes checkSchedule a no-op while SafelySetTime fast-forwards.
            var restored = new List<NPC>();
            foreach (var npc in Utility.getAllCharacters())
            {
                var pos = position.FirstOrDefault(p => p.Name == npc.Name);
                if (pos == null) continue;

                try
                {
                    Game1.warpCharacter(npc, pos.Map, new Point(pos.X, pos.Y));
                    npc.faceDirection(pos.FacingDirection);

                    if (!npc.IsVillager) continue;

                    npc.Halt();
                    npc.controller = null;
                    npc.temporaryController = null;
                    npc.queuedSchedulePaths?.Clear();
                    npc.ignoreScheduleToday = true;
                    restored.Add(npc);
                }
                catch (Exception ex)
                {
                    log.Log($"[SA] Exception restoring {npc?.Name}: {ex.Message}", LogLevel.Error);
                }
            }

            // Advance the clock; lights, music and location state update naturally
            // while villager schedules stay suppressed.
            SafelySetTime(time);

            // Give each villager a route to its CURRENT schedule destination computed
            // fresh from the tile it actually stands on (collision-valid and cross-map,
            // same pathfinding the engine uses when parsing schedules), then let
            // checkSchedule() take over as if the route were the engine's own.
            foreach (var npc in restored)
            {
                try
                {
                    npc.ignoreScheduleToday = false;
                    npc.followSchedule = true;
                    if (npc.Schedule == null) continue;

                    // Last schedule entry that should have started by savedTime.
                    int lastPassed = 0;
                    foreach (var k in npc.Schedule.Keys.OrderBy(x => x))
                    {
                        if (k > time) break;
                        lastPassed = k;
                    }

                    // Past entries are handled below; stop the engine replaying them.
                    npc.lastAttemptedSchedule = time;

                    if (lastPassed == 0) continue; // schedule hasn't started yet today

                    var seg = npc.Schedule[lastPassed];

                    // Destination = the segment's target tile; fall back to the canned
                    // route's final point (targetTile can be left at -1,-1).
                    Point dest = seg.targetTile;
                    if (dest.X <= 0 && dest.Y <= 0 && seg.route != null && seg.route.Count > 0)
                        dest = seg.route.Last();
                    if (dest.X <= 0 && dest.Y <= 0) continue;

                    bool atDest = npc.currentLocation?.Name == seg.targetLocationName
                                  && npc.TilePoint.X == dest.X && npc.TilePoint.Y == dest.Y;

                    var fresh = npc.pathfindToNextScheduleLocation(
                        npc.ScheduleKey,
                        npc.currentLocation.Name, npc.TilePoint.X, npc.TilePoint.Y,
                        seg.targetLocationName, dest.X, dest.Y,
                        seg.facingDirection, seg.endOfRouteBehavior, seg.endOfRouteMessage);
                    fresh.time = lastPassed;

                    if (!atDest && (fresh.route == null || fresh.route.Count == 0))
                    {
                        // No walkable route from here (unreachable interior, missing
                        // warp path): snap to the destination so the NEXT schedule
                        // entry starts from the tile its canned route expects.
                        Game1.warpCharacter(npc, seg.targetLocationName, new Point(dest.X, dest.Y));
                        npc.faceDirection(seg.facingDirection);
                        npc.previousEndPoint = dest;
                        continue;
                    }

                    // Queue it and fire through the engine so endOfRouteBehavior,
                    // previousEndPoint and facing are wired exactly like a normal
                    // schedule move. An empty route (already at destination) makes
                    // checkSchedule run the end-of-route behavior immediately.
                    npc.queuedSchedulePaths?.Clear();
                    npc.queuedSchedulePaths?.Add(fresh);
                    npc.checkSchedule(Game1.timeOfDay);
                }
                catch (Exception ex)
                {
                    log.Log($"[SA] Exception scheduling {npc?.Name}: {ex.Message}", LogLevel.Error);
                }
            }

            Utility.fixAllAnimals();
        }

        // ── Dropped items ────────────────────────────────────────────────────
        // GameLocation.debris is intentionally excluded from the vanilla save
        // (normal saves only happen after the ground is already clear at
        // night), so a mid-day save loses anything not yet picked up. Only
        // single-item drops (a real Item, not a loose resource chunk from a
        // tool swing — those are re-swingable, not worth the complexity of
        // modeling per-chunk physics) are preserved here and respawned on
        // load via the game's own item factory.

        private static DebrisData[] GetDebris()
        {
            var log = SaveAnywhere.Instance.Monitor;
            var list = new List<DebrisData>();

            // Game1.locations alone does NOT include building interiors (farmhouse,
            // sheds, coops, barns, cabins, etc.) — those are nested under each
            // Building's own .indoors, which is exactly why Utility.ForEachLocation
            // exists (includeInteriors: true walks them too). Iterating Game1.locations
            // directly here silently missed every debris dropped indoors.
            // includeGenerated also covers a currently-active mine/volcano floor.
            Utility.ForEachLocation(delegate(GameLocation location)
            {
                try
                {
                    var mapName = !string.IsNullOrEmpty(location.uniqueName.Value)
                        ? location.uniqueName.Value : location.Name;

                    foreach (var d in location.debris)
                    {
                        try
                        {
                            if (d?.item == null) continue;
                            if (d.debrisType.Value != Debris.DebrisType.OBJECT
                                && d.debrisType.Value != Debris.DebrisType.ARCHAEOLOGY) continue;

                            var chunk = d.Chunks.FirstOrDefault();
                            if (chunk == null) continue;
                            var tile = chunk.position.Value / 64f;

                            int quality = (d.item as StardewValley.Object)?.Quality ?? 0;
                            list.Add(new DebrisData(mapName, (int)tile.X, (int)tile.Y,
                                d.item.QualifiedItemId, d.item.Stack, quality));
                        }
                        catch (Exception ex)
                        {
                            // One bad debris item shouldn't cost every other item on
                            // the ground; skip just this one and keep going.
                            log.Log($"[SA] Skipped one debris item in {mapName}: {ex.Message}", LogLevel.Trace);
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Log($"[SA] Skipped location {location?.Name} while scanning debris: {ex.Message}", LogLevel.Trace);
                }
                return true; // keep iterating remaining locations
            }, includeInteriors: true, includeGenerated: true);

            log.Log($"[SA] Captured {list.Count} debris item(s) for mid-day save", LogLevel.Trace);
            return list.ToArray();
        }

        private static void RestoreDebris(DebrisData[] debrisDatas)
        {
            if (debrisDatas == null || debrisDatas.Length == 0) return;
            var log = SaveAnywhere.Instance.Monitor;

            // Game1.getLocationFromName(name) alone doesn't reliably resolve building
            // interiors (same gap as GetDebris reading Game1.locations directly), so
            // build the lookup the same way the data was captured — via
            // Utility.ForEachLocation — to guarantee symmetry between save and load.
            var byName = new Dictionary<string, GameLocation>();
            Utility.ForEachLocation(delegate(GameLocation location)
            {
                var name = !string.IsNullOrEmpty(location.uniqueName.Value)
                    ? location.uniqueName.Value : location.Name;
                if (!string.IsNullOrEmpty(name))
                    byName[name] = location;
                return true;
            }, includeInteriors: true, includeGenerated: true);

            int restored = 0;
            foreach (var dd in debrisDatas)
            {
                try
                {
                    if (!byName.TryGetValue(dd.Map, out var location))
                    {
                        log.Log($"[SA] Debris location '{dd.Map}' not found on load; skipping {dd.QualifiedItemId}", LogLevel.Trace);
                        continue;
                    }

                    var item = ItemRegistry.Create(dd.QualifiedItemId, dd.Stack, dd.Quality, allowNull: true);
                    if (item == null)
                    {
                        log.Log($"[SA] Debris item '{dd.QualifiedItemId}' no longer exists; skipping", LogLevel.Trace);
                        continue;
                    }

                    var origin = new Vector2(dd.X * 64f + 32f, dd.Y * 64f + 32f);
                    location.debris.Add(new Debris(item, origin));
                    restored++;
                }
                catch (Exception ex)
                {
                    // One bad entry shouldn't cost every other item; skip just this one.
                    log.Log($"[SA] Exception restoring debris '{dd.QualifiedItemId}': {ex.Message}", LogLevel.Trace);
                }
            }

            log.Log($"[SA] Restored {restored}/{debrisDatas.Length} debris item(s) after mid-day load", LogLevel.Trace);
        }


        private void RemoveLegacyDataForThisPlayer()
        {
            var directoryInfo1 = new DirectoryInfo(Path.Combine(_helper.DirectoryPath, "Save_Data"));
            var directoryInfo2 = new DirectoryInfo(Path.Combine(directoryInfo1.FullName, Game1.player.Name));
            if (directoryInfo2.Exists)
                directoryInfo2.Delete(true);
            if (!directoryInfo1.Exists || directoryInfo1.EnumerateDirectories().Any())
                return;
            directoryInfo1.Delete(true);
        }

        private void SafelySetTime(int time)
        {
            // transition to new time
            var intervals = Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, time) / 10;
            if (intervals > 0)
                for (var i = 0; i < intervals; i++)
                    Game1.performTenMinuteClockUpdate();
            else if (intervals < 0)
                for (var i = 0; i > intervals; i--)
                {
                    Game1.timeOfDay =
                        Utility.ModifyTime(Game1.timeOfDay, -20); // offset 20 mins so game updates to next interval
                    Game1.performTenMinuteClockUpdate();
                }

            // reset ambient light
            // White is the default non-raining color. If it's raining or dark out, UpdateGameClock
            // below will update it automatically.
            Game1.outdoorLight = Color.White;
            Game1.ambientLight = Color.White;

            // run clock update (to correct lighting, etc)
            Game1.gameTimeInterval = 0;
            Game1.UpdateGameClock(Game1.currentGameTime);
        }

        internal void RunAfterSave(object sender, SavedEventArgs e)
        {
            if (_middaysaving)
            {
                SaveComplete?.Invoke("", EventArgs.Empty);
                _middaysaving = false;
            }
        }
    }
}