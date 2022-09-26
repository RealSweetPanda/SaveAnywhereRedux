using Omegasis.SaveAnywhere.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Monsters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Omegasis.SaveAnywhere
{
  public class SaveAnywhere : Mod
  {
    public static Omegasis.SaveAnywhere.SaveAnywhere Instance;
    private ModConfig Config;
    public SaveManager SaveManager;
    private readonly IDictionary<string, string> NpcSchedules = (IDictionary<string, string>) new Dictionary<string, string>();
    private bool ShouldResetSchedules;
    public bool IsCustomSaving;
    public static IModHelper ModHelper;
    public static IMonitor ModMonitor;
    private Dictionary<GameLocation, List<Monster>> monsters;
    private bool customMenuOpen;
    private bool firstLoad;

    public override void Entry(IModHelper helper)
    {
      this.Config = helper.ReadConfig<ModConfig>();
      this.SaveManager = new SaveManager(this.Helper, this.Helper.Reflection, (Action) (() => this.ShouldResetSchedules = true));
      helper.Events.GameLoop.SaveLoaded += new EventHandler<SaveLoadedEventArgs>(this.OnSaveLoaded);
      helper.Events.GameLoop.UpdateTicked += new EventHandler<UpdateTickedEventArgs>(this.OnUpdateTicked);
      helper.Events.GameLoop.DayStarted += new EventHandler<DayStartedEventArgs>(this.OnDayStarted);
      helper.Events.Input.ButtonPressed += new EventHandler<ButtonPressedEventArgs>(this.OnButtonPressed);
      helper.Events.GameLoop.ReturnedToTitle += new EventHandler<ReturnedToTitleEventArgs>(this.GameLoop_ReturnedToTitle);
      helper.Events.GameLoop.TimeChanged += new EventHandler<TimeChangedEventArgs>(this.GameLoop_TimeChanged);
      Omegasis.SaveAnywhere.SaveAnywhere.ModHelper = helper;
      Omegasis.SaveAnywhere.SaveAnywhere.ModMonitor = this.Monitor;
      this.customMenuOpen = false;
      Omegasis.SaveAnywhere.SaveAnywhere.Instance = this;
      this.firstLoad = false;
    }

    private void GameLoop_TimeChanged(object sender, TimeChangedEventArgs e)
    {
    }

    private void GameLoop_ReturnedToTitle(object sender, ReturnedToTitleEventArgs e) => this.firstLoad = false;

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {
      this.ShouldResetSchedules = false;
      this.SaveManager.LoadData();
    }

    private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
      if (Context.IsWorldReady)
      {
        if (!Game1.player.IsMainPlayer)
          return;
        this.SaveManager.Update();
      }
      if (Game1.activeClickableMenu == null && Context.IsWorldReady)
        this.IsCustomSaving = false;
      if (Game1.activeClickableMenu == null && !this.customMenuOpen)
        return;
      if (Game1.activeClickableMenu == null && this.customMenuOpen)
      {
        this.customMenuOpen = false;
      }
      else
      {
        if (Game1.activeClickableMenu == null || !(((object) Game1.activeClickableMenu).GetType() == typeof (NewSaveGameMenuV2)))
          return;
        this.customMenuOpen = true;
      }
    }

    public void cleanMonsters()
    {
      this.monsters = new Dictionary<GameLocation, List<Monster>>();
      foreach (GameLocation location in (IEnumerable<GameLocation>) Game1.locations)
      {
        this.monsters.Add(location, new List<Monster>());
        foreach (NPC character in location.characters)
        {
          if (character is Monster monster)
          {
            this.Monitor.Log(((Character) character).Name, (LogLevel) 0);
            this.monsters[location].Add(monster);
          }
        }
        foreach (Monster monster in this.monsters[location])
          location.characters.Remove((NPC) monster);
      }
    }

    public static void RestoreMonsters()
    {
      foreach (KeyValuePair<GameLocation, List<Monster>> monster1 in Omegasis.SaveAnywhere.SaveAnywhere.Instance.monsters)
      {
        foreach (Monster monster2 in monster1.Value)
          monster1.Key.addCharacter((NPC) monster2);
      }
      Omegasis.SaveAnywhere.SaveAnywhere.Instance.monsters.Clear();
    }

    private void OnDayStarted(object sender, DayStartedEventArgs e)
    {
      if (this.IsCustomSaving)
        return;
      if (!this.firstLoad)
      {
        this.firstLoad = true;
        if (this.SaveManager.saveDataExists())
        {
          this.ShouldResetSchedules = false;
          this.ApplySchedules();
        }
      }
      else if (this.firstLoad)
        this.SaveManager.ClearData();
      this.ShouldResetSchedules = true;
    }

    private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
    {
      if (!Context.IsPlayerFree || e.Button != this.Config.SaveKey || Game1.eventUp || Game1.isFestival())
        return;
      if (Game1.client == null)
      {
        if (((IEnumerable) ((Character) Game1.player).currentLocation.getCharacters()).OfType<Junimo>().Any<Junimo>())
        {
          Game1.addHUDMessage(new HUDMessage("The spirits don't want you to save here.", 3));
        }
        else
        {
          this.IsCustomSaving = true;
          this.SaveManager.BeginSaveData();
        }
      }
      else
        Game1.addHUDMessage(new HUDMessage("Only server hosts can save anywhere.", 3));
    }

    private void ApplySchedules()
    {
      if (Game1.weatherIcon == 4 || Game1.isFestival() || Game1.eventUp)
        return;
      foreach (GameLocation location in (IEnumerable<GameLocation>) Game1.locations)
      {
        foreach (NPC character in location.characters)
        {
          if (character.isVillager())
            character.fillInSchedule();
        }
      }
    }

    public virtual object GetApi() => (object) new SaveAnywhereAPI();
  }
}
