using System.Collections.Generic;
using System.Linq;
using SaveAnywhere.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Monsters;

namespace SaveAnywhere {
    public class SaveAnywhere : Mod {
        public static SaveAnywhere Instance;
        public static IModHelper ModHelper;
        public static IMonitor ModMonitor;
        private readonly IDictionary<string, string> NpcSchedules = new Dictionary<string, string>();
        private ModConfig Config;
        private bool customMenuOpen;
        private bool firstLoad;
        public bool IsCustomSaving;
        private Dictionary<GameLocation, List<Monster>> monsters;
        public SaveManager SaveManager;
        private bool ShouldResetSchedules;

        public override void Entry(IModHelper helper) {
            Config = helper.ReadConfig<ModConfig>();
            SaveManager = new SaveManager(Helper, Helper.Reflection, () => ShouldResetSchedules = true);
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.ReturnedToTitle += GameLoop_ReturnedToTitle;
            helper.Events.GameLoop.TimeChanged += GameLoop_TimeChanged;
            ModHelper = helper;
            ModMonitor = Monitor;
            customMenuOpen = false;
            Instance = this;
            firstLoad = false;
        }

        private void GameLoop_TimeChanged(object sender, TimeChangedEventArgs e) { }

        private void GameLoop_ReturnedToTitle(object sender, ReturnedToTitleEventArgs e) {
            firstLoad = false;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e) {
            ShouldResetSchedules = false;
            SaveManager.LoadData();
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e) {
            if (Context.IsWorldReady) {
                if (!Game1.player.IsMainPlayer)
                    return;
                SaveManager.Update();
            }
        }

        public void cleanMonsters() {
            monsters = new Dictionary<GameLocation, List<Monster>>();
            foreach (var location in Game1.locations) {
                monsters.Add(location, new List<Monster>());
                foreach (var character in location.characters)
                    if (character is Monster monster) {
                        Monitor.Log(character.Name);
                        monsters[location].Add(monster);
                    }

                foreach (var monster in monsters[location])
                    location.characters.Remove(monster);
            }
        }

        public static void RestoreMonsters() {
            foreach (var monster1 in Instance.monsters)
            foreach (var monster2 in monster1.Value)
                monster1.Key.addCharacter(monster2);
            Instance.monsters.Clear();
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e) {
            if (IsCustomSaving)
                return;
            if (!firstLoad) {
                firstLoad = true;
                if (SaveManager.saveDataExists()) {
                    ShouldResetSchedules = false;
                    ApplySchedules();
                }
            }
            else if (firstLoad) {
                SaveManager.ClearData();
            }
            ShouldResetSchedules = true;
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e) {
            if (!Context.IsPlayerFree || e.Button != Config.SaveKey || Game1.eventUp || Game1.isFestival())
                return;
            if (Game1.client == null) {
                if (Game1.player.currentLocation.getCharacters().OfType<Junimo>().Any()) {
                    Game1.addHUDMessage(new HUDMessage("The spirits don't want you to save here.", 3));
                }
                else {
                    IsCustomSaving = true;
                    SaveManager.BeginSaveData();

                }
            }
            else {
                Game1.addHUDMessage(new HUDMessage("Only server hosts can save anywhere.", 3));
            }
        }

        private void ApplySchedules() {
            if (Game1.weatherIcon == 4 || Game1.isFestival() || Game1.eventUp)
                return;
            foreach (var location in Game1.locations)
            foreach (var character in location.characters)
                if (character.isVillager())
                    character.fillInSchedule();
        }

        public override object GetApi() {
            return new SaveAnywhereAPI();
        }
    }
}