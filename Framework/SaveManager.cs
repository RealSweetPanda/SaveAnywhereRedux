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
        private NewSaveGameMenuV2 currentSaveMenu;
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

            Helper.Data.WriteJsonFile(RelativeDataPath, new PlayerData {
                Time = Game1.timeOfDay,
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

        private IEnumerable<CharacterData> GetPositions() {
            var player = Game1.player;
            var name1 = player.Name;
            var map1 = player.currentLocation.uniqueName.Value;
            if (string.IsNullOrEmpty(map1))
                map1 = player.currentLocation.Name;
            var tile1 = player.getTileLocationPoint();
            int facingDirection1 = player.facingDirection;
            yield return new CharacterData(CharacterType.Player, name1, map1, tile1, facingDirection1);
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
                    yield return new CharacterData(type.Value, name2, map2, tile2, facingDirection2);
                    type = new CharacterType?();
                    name2 = null;
                    map2 = null;
                    tile2 = new Point();
                    npc = null;
                }
            }
        }

        private void SetPositions(CharacterData[] positions) {
            var characterData1 = positions.FirstOrDefault((Func<CharacterData, bool>) (p =>
                p.Type == CharacterType.Player && p.Name.Equals(Game1.player.Name)));
            if (characterData1 != null) {
                Game1.player.previousLocationName = Game1.player.currentLocation.Name;
                Game1.xLocationAfterWarp = characterData1.X;
                Game1.yLocationAfterWarp = characterData1.Y;
                Game1.facingDirectionAfterWarp = characterData1.FacingDirection;
                Game1.fadeScreenToBlack();
                Game1.warpFarmer(characterData1.Map, characterData1.X, characterData1.Y, false);
                Game1.player.faceDirection(characterData1.FacingDirection);
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