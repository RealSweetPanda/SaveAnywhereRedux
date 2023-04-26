using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using SaveAnywhere.Framework.Model;
using StardewModdingAPI;
using StardewValley;

namespace SaveAnywhere.Framework {
    public class SaveManager {
        private readonly IModHelper Helper;
        private readonly Action OnLoaded;
        private readonly IReflectionHelper Reflection;
        public Dictionary<string, Action> afterCustomSavingCompleted;
        public Dictionary<string, Action> afterSaveLoaded;
        public Dictionary<string, Action> beforeCustomSavingBegins;
        private NewSaveGameMenuV2 currentSaveMenu;
        private bool WaitingToSave;

        public SaveManager(IModHelper helper, IReflectionHelper reflection, Action onLoaded) {
            Helper = helper;
            Reflection = reflection;
            // OnLoaded = onLoaded;
            beforeCustomSavingBegins = new Dictionary<string, Action>();
            afterCustomSavingCompleted = new Dictionary<string, Action>();
            afterSaveLoaded = new Dictionary<string, Action>();
        }

        private string RelativeDataPath => Path.Combine("data", Constants.SaveFolderName + ".json");

        public event EventHandler beforeSave;

        public event EventHandler afterSave;

        public event EventHandler afterLoad;

        private void empty(object o, EventArgs args) { }

        public void Update() {
            if (!WaitingToSave || Game1.activeClickableMenu != null)
                return;
            currentSaveMenu = new NewSaveGameMenuV2();
            currentSaveMenu.SaveComplete += CurrentSaveMenu_SaveComplete;
            Game1.activeClickableMenu = currentSaveMenu;
            WaitingToSave = false;
        }

        private void CurrentSaveMenu_SaveComplete(object sender, EventArgs e) {
            currentSaveMenu.SaveComplete -= CurrentSaveMenu_SaveComplete;
            currentSaveMenu = null;
            SaveAnywhere.RestoreMonsters();
            if (afterSave != null)
                afterSave(this, EventArgs.Empty);
            foreach (var keyValuePair in afterCustomSavingCompleted)
                keyValuePair.Value();
        }

        public void ClearData() {
            if (File.Exists(Path.Combine(Helper.DirectoryPath, RelativeDataPath)))
                File.Delete(Path.Combine(Helper.DirectoryPath, RelativeDataPath));
            RemoveLegacyDataForThisPlayer();
        }

        public bool saveDataExists() {
            return File.Exists(Path.Combine(Helper.DirectoryPath, RelativeDataPath));
        }

        public void BeginSaveData() {
            if (beforeSave != null)
                beforeSave(this, EventArgs.Empty);
            foreach (var customSavingBegin in beforeCustomSavingBegins)
                customSavingBegin.Value();
            SaveAnywhere.Instance.cleanMonsters();
            var farm = Game1.getFarm();
            if (farm.getShippingBin(Game1.player) != null) {
                Game1.activeClickableMenu = new NewShippingMenuV2(farm.getShippingBin(Game1.player));
                farm.lastItemShipped = null;
                WaitingToSave = true;
            }
            else {
                currentSaveMenu = new NewSaveGameMenuV2();
                currentSaveMenu.SaveComplete += CurrentSaveMenu_SaveComplete;
                Game1.activeClickableMenu = currentSaveMenu;
            }
            var drink = Game1.buffsDisplay.drink;
            BuffData drinkdata = null;

            if (drink != null)
            {
                drinkdata = new BuffData(
                    drink.displaySource,
                    drink.source,
                    drink.millisecondsDuration,
                    drink.buffAttributes
                );
            }
            var food = Game1.buffsDisplay.food;
            BuffData fooddata = null;
            if (food != null)
            {
                fooddata = new BuffData(
                    food.displaySource,
                    food.source,
                    food.millisecondsDuration,
                    food.buffAttributes
                );
            }
            Helper.Data.WriteJsonFile(RelativeDataPath, new PlayerData {
                Time = Game1.timeOfDay,
                otherBuffs = GetotherBuffs().ToArray(),
                drinkBuff = drinkdata,
                foodBuff = fooddata,
                Position = GetPosition(),
                IsCharacterSwimming = Game1.player.swimming.Value
            });
            RemoveLegacyDataForThisPlayer();
        }

        public void LoadData() {
            var data = Helper.Data.ReadJsonFile<PlayerData>(RelativeDataPath);
            if (data == null)
                return;
            SetPositions(data.Position);
            if (data.otherBuffs != null)
            {
                foreach (var buff in data.otherBuffs)
                {
                    var atts = buff.Attributes;
                    Game1.buffsDisplay.addOtherBuff(new Buff(atts[0],
                        atts[1],
                        atts[2],
                        atts[3],
                        atts[4],
                        atts[5],
                        atts[6],
                        atts[7],
                        atts[8],
                        atts[9],
                        atts[10],
                        atts[11],
                        buff.MillisecondsDuration * 10 / 7000,
                        buff.Source,
                        buff.DisplaySource));
                }
            }
            var datadrink = data.drinkBuff;
            var datafood = data.foodBuff;
            
            if (datadrink != null)
            {
                var atts = datadrink.Attributes;
                Game1.buffsDisplay.tryToAddDrinkBuff(new Buff(atts[0],
                    atts[1],
                    atts[2],
                    atts[3],
                    atts[4],
                    atts[5],
                    atts[6],
                    atts[7],
                    atts[8],
                    atts[9],
                    atts[10],
                    atts[11],
                    datadrink.MillisecondsDuration * 10 / 7000,
                    datadrink.Source,
                    datadrink.DisplaySource));
            }
            if (datafood != null)
            {
                var atts = datafood.Attributes;
                Game1.buffsDisplay.tryToAddFoodBuff(new Buff(atts[0],
                    atts[1],
                    atts[2],
                    atts[3],
                    atts[4],
                    atts[5],
                    atts[6],
                    atts[7],
                    atts[8],
                    atts[9],
                    atts[10],
                    atts[11],
                    datafood.MillisecondsDuration * 10 / 7000,
                    datafood.Source,
                    datafood.DisplaySource),datafood.MillisecondsDuration);
            }
            ResumeSwimming(data);
            var onLoaded = OnLoaded;
            if (onLoaded != null)
                onLoaded();
            if (afterLoad != null)
                afterLoad(this, EventArgs.Empty);
            foreach (var keyValuePair in afterSaveLoaded)
                keyValuePair.Value();
            SafelySetTime(data.Time);
        }

        public void ResumeSwimming(PlayerData data) {
            try {
                if (!data.IsCharacterSwimming)
                    return;
                Game1.player.changeIntoSwimsuit();
                Game1.player.swimming.Value = true;
            }
            catch { }
        }
        private IEnumerable<BuffData> GetotherBuffs() {
            foreach (var buff in Game1.buffsDisplay.otherBuffs) {
                yield return new BuffData(
                    buff.displaySource,
                    buff.source, 
                    buff.millisecondsDuration,
                    buff.buffAttributes
                );
            }
        }
        private PositionData GetPosition() {
            var player = Game1.player;
            var name1 = player.Name;
            var map1 = player.currentLocation.uniqueName.Value;
            if (string.IsNullOrEmpty(map1))
                map1 = player.currentLocation.Name;
            var tile1 = player.getTileLocationPoint();
            int facingDirection1 = player.facingDirection;
            return new PositionData(name1, map1, tile1.X, tile1.Y, facingDirection1);

        }

        private void SetPositions(PositionData position) {

                Game1.player.previousLocationName = Game1.player.currentLocation.Name;
                Game1.xLocationAfterWarp = position.X;
                Game1.yLocationAfterWarp = position.Y;
                Game1.facingDirectionAfterWarp = position.FacingDirection;
                Game1.fadeScreenToBlack();
                Game1.warpFarmer(position.Map, position.X, position.Y, false);
                Game1.player.faceDirection(position.FacingDirection);
            

        foreach (var allCharacter in Utility.getAllCharacters()) {
            allCharacter.dayUpdate(Game1.dayOfMonth);
        }
        }



        private void RemoveLegacyDataForThisPlayer() {
            var directoryInfo1 = new DirectoryInfo(Path.Combine(Helper.DirectoryPath, "Save_Data"));
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
            int intervals = Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, time) / 10;
            if (intervals > 0)
            {
                for (int i = 0; i < intervals; i++)
                    Game1.performTenMinuteClockUpdate();
            }
            else if (intervals < 0)
            {
                for (int i = 0; i > intervals; i--)
                {
                    Game1.timeOfDay = Utility.ModifyTime(Game1.timeOfDay, -20); // offset 20 mins so game updates to next interval
                    Game1.performTenMinuteClockUpdate();
                }
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
    }
}