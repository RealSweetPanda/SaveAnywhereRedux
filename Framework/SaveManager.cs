using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using SaveAnywhere.Framework.Model;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Monsters;

namespace SaveAnywhere.Framework {
    public class SaveManager {
        private readonly IModHelper Helper;
        private readonly Action OnLoaded;
        private readonly IReflectionHelper Reflection;
        public Dictionary<string, Action> afterCustomSavingCompleted;
        public Dictionary<string, Action> afterSaveLoaded;
        public Dictionary<string, Action> beforeCustomSavingBegins;
        private bool WaitingToSave;

        public SaveManager(IModHelper helper, IReflectionHelper reflection, Action onLoaded) {
            Helper = helper;
            Reflection = reflection;
            OnLoaded = onLoaded;
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
            WaitingToSave = false;
        }

        private void CurrentSaveMenu_SaveComplete(object sender, EventArgs e) {
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
                Characters = GetPositions().ToArray(),
                IsCharacterSwimming = Game1.player.swimming.Value
            });
            RemoveLegacyDataForThisPlayer();
        }

        public void LoadData() {
            var data = Helper.Data.ReadJsonFile<PlayerData>(RelativeDataPath);
            if (data == null)
                return;
            Game1.timeOfDay = data.Time;
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
            SetPositions(data.Characters);
            var onLoaded = OnLoaded;
            if (onLoaded != null)
                onLoaded();
            if (afterLoad != null)
                afterLoad(this, EventArgs.Empty);
            foreach (var keyValuePair in afterSaveLoaded)
                keyValuePair.Value();
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
        private IEnumerable<CharacterData> GetPositions() {
            var player = Game1.player;
            var name1 = player.Name;
            var map1 = player.currentLocation.uniqueName.Value;
            if (string.IsNullOrEmpty(map1))
                map1 = player.currentLocation.Name;
            var tile1 = player.getTileLocationPoint();
            int facingDirection1 = player.facingDirection;
            yield return new CharacterData(CharacterType.Player, name1, map1, tile1.X, tile1.Y, facingDirection1);
            player = null;
            name1 = null;
            map1 = null;
            tile1 = new Point();
            foreach (var allCharacter in Utility.getAllCharacters()) {
                var npc = allCharacter;
                var type = GetCharacterType(npc);
                if (type.HasValue && npc?.currentLocation != null) {
                    var name2 = npc.Name;
                    var map2 = npc.currentLocation.Name;
                    var tile2 = npc.getTileLocationPoint();
                    int facingDirection2 = npc.facingDirection;
                    yield return new CharacterData(type.Value, name2, map2, tile2.X, tile2.Y, facingDirection2);
                    type = new CharacterType?();
                    name2 = null;
                    map2 = null;
                    tile2 = new Point();
                    npc = null;
                }
            }
        }

        private void SetPositions(CharacterData[] positions) {
            foreach (var playerCharacterData in positions) {
                if (playerCharacterData.Type != CharacterType.Player ||
                    !playerCharacterData.Name.Equals(Game1.player.Name)) continue;
                
                Game1.player.previousLocationName = Game1.player.currentLocation.Name;
                Game1.xLocationAfterWarp = playerCharacterData.X;
                Game1.yLocationAfterWarp = playerCharacterData.Y;
                Game1.facingDirectionAfterWarp = playerCharacterData.FacingDirection;
                Game1.fadeScreenToBlack();
                Game1.warpFarmer(playerCharacterData.Map, playerCharacterData.X, playerCharacterData.Y, false);
                Game1.player.faceDirection(playerCharacterData.FacingDirection);
                
                break;
            }

            foreach (var allCharacter in Utility.getAllCharacters()) {
                var npc = allCharacter;
                var type = GetCharacterType(npc);
                if (type.HasValue) {
                    var characterData2 = positions.FirstOrDefault((Func<CharacterData, bool>) (p => {
                        var type1 = (int) p.Type;
                        var nullable = type;
                        var valueOrDefault = (int) nullable.GetValueOrDefault();
                        return (type1 == valueOrDefault) & nullable.HasValue && p.Name == npc.Name;
                    }));
                    if (characterData2 != null) {
                        Game1.warpCharacter(npc, characterData2.Map, new Point(characterData2.X, characterData2.Y));
                        npc.faceDirection(characterData2.FacingDirection);
                    }
                }
            }
        }

        private CharacterType? GetCharacterType(NPC npc) {
            switch (npc) {
                case Monster _:
                    return new CharacterType?();
                case Horse _:
                    return CharacterType.Horse;
                case Pet _:
                    return CharacterType.Pet;
                default:
                    return CharacterType.Villager;
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
    }
}