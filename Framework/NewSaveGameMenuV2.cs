using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace SaveAnywhere.Framework {
    public class NewSaveGameMenuV2 : IClickableMenu {
        private int _ellipsisCount;
        private float _ellipsisDelay = 0.5f;
        protected bool _hasSentFarmhandData;
        private readonly StringBuilder _stringBuilder = new();
        private int completePause = -1;
        public bool hasDrawn;
        private IEnumerator<int> loader;
        private int margin = 500;
        public Multiplayer multiplayer;
        public bool quit;
        private readonly SparklingText saveText;
        private Stopwatch stopwatch;

        public NewSaveGameMenuV2() {
            saveText = new SparklingText(Game1.dialogueFont,
                Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGameMenu.cs.11378"), Color.LimeGreen,
                Color.Black * 0.001F, false, 0.1, 1500, 32);
            _hasSentFarmhandData = false;
            multiplayer = (Multiplayer) typeof(Game1)
                .GetField(nameof(multiplayer), BindingFlags.Static | BindingFlags.NonPublic).GetValue(Program.gamePtr);
        }

        public event EventHandler SaveComplete;

        public override void receiveRightClick(int x, int y, bool playSound = true) { }

        public void complete() {
            Game1.playSound("money");
            completePause = 1500;
            loader = null;
            Game1.game1.IsSaving = false;
            if (!Game1.IsMasterGame || Game1.newDaySync == null || Game1.newDaySync.hasSaved())
                return;
            Game1.newDaySync.flagSaved();
            SaveComplete(this, EventArgs.Empty);
        }

        public override bool readyToClose() {
            return false;
        }

        public override void update(GameTime time) {
            if (quit) {
                if (!Game1.activeClickableMenu.Equals(this) || !Game1.PollForEndOfNewDaySync())
                    return;
                Game1.exitActiveMenu();
            }
            else {
                base.update(time);
                if (Game1.client != null && Game1.client.timedOut) {
                    quit = true;
                    if (!Game1.activeClickableMenu.Equals(this))
                        return;
                    Game1.exitActiveMenu();
                }
                else {
                    _ellipsisDelay -= (float) time.ElapsedGameTime.TotalSeconds;
                    if (_ellipsisDelay <= 0.0) {
                        _ellipsisDelay += 0.75f;
                        ++_ellipsisCount;
                        if (_ellipsisCount > 3)
                            _ellipsisCount = 1;
                    }

                    if (loader != null) {
                        loader.MoveNext();
                        if (loader.Current >= 100) {
                            margin -= time.ElapsedGameTime.Milliseconds;
                            if (margin <= 0)
                                complete();
                        }
                    }
                    else if (hasDrawn && completePause == -1) {
                        if (Game1.IsMasterGame) {
                            if (Game1.saveOnNewDay) {
                                Game1.player.team.endOfNightStatus.UpdateState("ready");
                                if (Game1.newDaySync != null) {
                                    if (Game1.newDaySync.readyForSave()) {
                                        multiplayer.saveFarmhands();
                                        Game1.game1.IsSaving = true;
                                        loader = SaveGame.Save();
                                        
                                    }
                                }
                                else {
                                    multiplayer.saveFarmhands();
                                    Game1.game1.IsSaving = true;
                                    loader = SaveGame.Save();
                                }
                            }
                            else {
                                margin = -1;
                                if (Game1.newDaySync != null) {
                                    if (Game1.newDaySync.readyForSave()) {
                                        Game1.game1.IsSaving = true;
                                        complete();
                                    }
                                }
                                else {
                                    complete();
                                }
                            }
                        }
                        else {
                            if (!_hasSentFarmhandData) {
                                _hasSentFarmhandData = true;
                                multiplayer.sendFarmhand();
                            }

                            multiplayer.UpdateLate();
                            multiplayer.UpdateEarly();
                            if (Game1.newDaySync != null)
                                Game1.newDaySync.readyForSave();
                            Game1.player.team.endOfNightStatus.UpdateState("ready");
                            if (Game1.newDaySync != null) {
                                if (Game1.newDaySync.hasSaved())
                                    complete();
                            }
                            else {
                                complete();
                            }
                        }
                    }

                    if (completePause < 0)
                        return;
                    completePause -= time.ElapsedGameTime.Milliseconds;
                    saveText.update(time);
                    if (completePause >= 0)
                        return;
                    quit = true;
                    completePause = -9999;
                }
            }
        }

        public static void saveClientOptions() {
            var startupPreferences = new StartupPreferences();
            startupPreferences.loadPreferences(false, true);
            startupPreferences.clientOptions = Game1.options;
            startupPreferences.savePreferences(false);
        }

        public override void draw(SpriteBatch b) {
            base.draw(b);
            var vector2 = Utility.makeSafe(new Vector2(64f, Game1.viewport.Height - 64), new Vector2(64f, 64f));
            var flag = false;
            if (completePause >= 0) {
                if (Game1.saveOnNewDay)
                    saveText.draw(b, vector2);
            }
            else if (margin < 0 || Game1.IsClient) {
                if (Game1.IsMultiplayer) {
                    _stringBuilder.Clear();
                    _stringBuilder.Append(Game1.content.LoadString("Strings\\UI:ReadyCheck",
                        Game1.newDaySync.numReadyForSave(), Game1.getOnlineFarmers().Count()));
                    for (var index = 0; index < _ellipsisCount; ++index)
                        _stringBuilder.Append(".");
                    b.DrawString(Game1.dialogueFont, _stringBuilder, vector2, Color.White);
                    flag = true;
                }
            }
            else if (!Game1.IsMultiplayer) {
                _stringBuilder.Clear();
                _stringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGameMenu.cs.11381"));
                for (var index = 0; index < _ellipsisCount; ++index)
                    _stringBuilder.Append(".");
                b.DrawString(Game1.dialogueFont, _stringBuilder, vector2, Color.White);
            }
            else {
                _stringBuilder.Clear();
                _stringBuilder.Append(Game1.content.LoadString("Strings\\UI:ReadyCheck",
                    Game1.newDaySync.numReadyForSave(), Game1.getOnlineFarmers().Count()));
                for (var index = 0; index < _ellipsisCount; ++index)
                    _stringBuilder.Append(".");
                b.DrawString(Game1.dialogueFont, _stringBuilder, vector2, Color.White);
                flag = true;
            }

            if (completePause > 0)
                flag = false;
            if (Game1.newDaySync != null && Game1.newDaySync.hasSaved())
                flag = false;
            if (Game1.IsMultiplayer & flag && Game1.options.showMPEndOfNightReadyStatus)
                Game1.player.team.endOfNightStatus.Draw(b, vector2 + new Vector2(0.0f, -32f), 4f, 0.99f, 0,
                    (PlayerStatusList.VerticalAlignment) 1);
            hasDrawn = true;
        }

        public void Dispose() {
            Game1.game1.IsSaving = false;
        }
    }
}