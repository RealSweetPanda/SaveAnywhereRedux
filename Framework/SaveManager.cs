// Decompiled with JetBrains decompiler
// Type: Omegasis.SaveAnywhere.Framework.SaveManager
// Assembly: SaveAnywhere, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: CA1E3B07-AC71-4821-90DC-80822753C1D9
// Assembly location: C:\Users\keren\Desktop\SaveAnywhere1.5\SaveAnywhere.dll

using Microsoft.Xna.Framework;
using Netcode;
using Omegasis.SaveAnywhere.Framework.Models;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Omegasis.SaveAnywhere.Framework
{
  public class SaveManager
  {
    private readonly IReflectionHelper Reflection;
    private readonly Action OnLoaded;
    private readonly IModHelper Helper;
    private bool WaitingToSave;
    private NewSaveGameMenuV2 currentSaveMenu;
    public Dictionary<string, Action> beforeCustomSavingBegins;
    public Dictionary<string, Action> afterCustomSavingCompleted;
    public Dictionary<string, Action> afterSaveLoaded;

    private string RelativeDataPath => Path.Combine("data", Constants.SaveFolderName + ".json");

    public event EventHandler beforeSave;

    public event EventHandler afterSave;

    public event EventHandler afterLoad;

    public SaveManager(IModHelper helper, IReflectionHelper reflection, Action onLoaded)
    {
      this.Helper = helper;
      this.Reflection = reflection;
      this.OnLoaded = onLoaded;
      this.beforeCustomSavingBegins = new Dictionary<string, Action>();
      this.afterCustomSavingCompleted = new Dictionary<string, Action>();
      this.afterSaveLoaded = new Dictionary<string, Action>();
    }

    private void empty(object o, EventArgs args)
    {
    }

    public void Update()
    {
      if (!this.WaitingToSave || Game1.activeClickableMenu != null)
        return;
      this.currentSaveMenu = new NewSaveGameMenuV2();
      this.currentSaveMenu.SaveComplete += new EventHandler(this.CurrentSaveMenu_SaveComplete);
      Game1.activeClickableMenu = (IClickableMenu) this.currentSaveMenu;
      this.WaitingToSave = false;
    }

    private void CurrentSaveMenu_SaveComplete(object sender, EventArgs e)
    {
      this.currentSaveMenu.SaveComplete -= new EventHandler(this.CurrentSaveMenu_SaveComplete);
      this.currentSaveMenu = (NewSaveGameMenuV2) null;
      Omegasis.SaveAnywhere.SaveAnywhere.RestoreMonsters();
      if (this.afterSave != null)
        this.afterSave((object) this, EventArgs.Empty);
      foreach (KeyValuePair<string, Action> keyValuePair in this.afterCustomSavingCompleted)
        keyValuePair.Value();
    }

    public void ClearData()
    {
      if (File.Exists(Path.Combine(this.Helper.DirectoryPath, this.RelativeDataPath)))
        File.Delete(Path.Combine(this.Helper.DirectoryPath, this.RelativeDataPath));
      this.RemoveLegacyDataForThisPlayer();
    }

    public bool saveDataExists() => File.Exists(Path.Combine(this.Helper.DirectoryPath, this.RelativeDataPath));

    public void BeginSaveData()
    {
      if (this.beforeSave != null)
        this.beforeSave((object) this, EventArgs.Empty);
      foreach (KeyValuePair<string, Action> customSavingBegin in this.beforeCustomSavingBegins)
        customSavingBegin.Value();
      Omegasis.SaveAnywhere.SaveAnywhere.Instance.cleanMonsters();
      Farm farm = Game1.getFarm();
      if (farm.getShippingBin(Game1.player) != null)
      {
        Game1.activeClickableMenu = (IClickableMenu) new NewShippingMenuV2((IList<Item>) farm.getShippingBin(Game1.player));
        farm.lastItemShipped = (Item) null;
        this.WaitingToSave = true;
      }
      else
      {
        this.currentSaveMenu = new NewSaveGameMenuV2();
        this.currentSaveMenu.SaveComplete += new EventHandler(this.CurrentSaveMenu_SaveComplete);
        Game1.activeClickableMenu = (IClickableMenu) this.currentSaveMenu;
      }
      this.Helper.Data.WriteJsonFile<PlayerData>(this.RelativeDataPath, new PlayerData()
      {
        Time = Game1.timeOfDay,
        Characters = this.GetPositions().ToArray<CharacterData>(),
        IsCharacterSwimming = ((NetFieldBase<bool, NetBool>) ((Character) Game1.player).swimming).Value
      });
      this.RemoveLegacyDataForThisPlayer();
    }

    public void LoadData()
    {
      PlayerData data = this.Helper.Data.ReadJsonFile<PlayerData>(this.RelativeDataPath);
      if (data == null)
        return;
      Game1.timeOfDay = data.Time;
      this.ResumeSwimming(data);
      this.SetPositions(data.Characters);
      Action onLoaded = this.OnLoaded;
      if (onLoaded != null)
        onLoaded();
      if (this.afterLoad != null)
        this.afterLoad((object) this, EventArgs.Empty);
      foreach (KeyValuePair<string, Action> keyValuePair in this.afterSaveLoaded)
        keyValuePair.Value();
    }

    public void ResumeSwimming(PlayerData data)
    {
      try
      {
        if (!data.IsCharacterSwimming)
          return;
        Game1.player.changeIntoSwimsuit();
        ((NetFieldBase<bool, NetBool>) ((Character) Game1.player).swimming).Value = true;
      }
      catch
      {
      }
    }

    private IEnumerable<CharacterData> GetPositions()
    {
      Farmer player = Game1.player;
      string name1 = ((Character) player).Name;
      string map1 = ((NetFieldBase<string, NetString>) ((Character) player).currentLocation.uniqueName).Value;
      if (string.IsNullOrEmpty(map1))
        map1 = ((Character) player).currentLocation.Name;
      Point tile1 = ((Character) player).getTileLocationPoint();
      int facingDirection1 = ((Character) player).facingDirection;
      yield return new CharacterData(CharacterType.Player, name1, map1, tile1, facingDirection1);
      player = (Farmer) null;
      name1 = (string) null;
      map1 = (string) null;
      tile1 = new Point();
      foreach (NPC allCharacter in Utility.getAllCharacters())
      {
        NPC npc = allCharacter;
        CharacterType? type = this.GetCharacterType(npc);
        if (type.HasValue && ((Character) npc)?.currentLocation != null)
        {
          string name2 = ((Character) npc).Name;
          string map2 = ((Character) npc).currentLocation.Name;
          Point tile2 = ((Character) npc).getTileLocationPoint();
          int facingDirection2 = ((Character) npc).facingDirection;
          yield return new CharacterData(type.Value, name2, map2, tile2, facingDirection2);
          type = new CharacterType?();
          name2 = (string) null;
          map2 = (string) null;
          tile2 = new Point();
          npc = (NPC) null;
        }
      }
    }

    private void SetPositions(CharacterData[] positions)
    {
      CharacterData characterData1 = ((IEnumerable<CharacterData>) positions).FirstOrDefault<CharacterData>((Func<CharacterData, bool>) (p => p.Type == CharacterType.Player && p.Name.Equals(((Character) Game1.player).Name)));
      if (characterData1 != null)
      {
        Game1.player.previousLocationName = ((Character) Game1.player).currentLocation.Name;
        Game1.xLocationAfterWarp = characterData1.X;
        Game1.yLocationAfterWarp = characterData1.Y;
        Game1.facingDirectionAfterWarp = characterData1.FacingDirection;
        Game1.fadeScreenToBlack();
        Game1.warpFarmer(characterData1.Map, characterData1.X, characterData1.Y, false);
        ((Character) Game1.player).faceDirection(characterData1.FacingDirection);
      }
      foreach (NPC allCharacter in Utility.getAllCharacters())
      {
        NPC npc = allCharacter;
        CharacterType? type = this.GetCharacterType(npc);
        if (type.HasValue)
        {
          CharacterData characterData2 = ((IEnumerable<CharacterData>) positions).FirstOrDefault<CharacterData>((Func<CharacterData, bool>) (p =>
          {
            int type1 = (int) p.Type;
            CharacterType? nullable = type;
            int valueOrDefault = (int) nullable.GetValueOrDefault();
            return type1 == valueOrDefault & nullable.HasValue && p.Name == ((Character) npc).Name;
          }));
          if (characterData2 != null)
          {
            Game1.warpCharacter(npc, characterData2.Map, new Point(characterData2.X, characterData2.Y));
            ((Character) npc).faceDirection(characterData2.FacingDirection);
          }
        }
      }
    }

    private CharacterType? GetCharacterType(NPC npc)
    {
      switch (npc)
      {
        case Monster _:
          return new CharacterType?();
        case Horse _:
          return new CharacterType?(CharacterType.Horse);
        case Pet _:
          return new CharacterType?(CharacterType.Pet);
        default:
          return new CharacterType?(CharacterType.Villager);
      }
    }

    private void RemoveLegacyDataForThisPlayer()
    {
      DirectoryInfo directoryInfo1 = new DirectoryInfo(Path.Combine(this.Helper.DirectoryPath, "Save_Data"));
      DirectoryInfo directoryInfo2 = new DirectoryInfo(Path.Combine(directoryInfo1.FullName, ((Character) Game1.player).Name));
      if (directoryInfo2.Exists)
        directoryInfo2.Delete(true);
      if (!directoryInfo1.Exists || directoryInfo1.EnumerateDirectories().Any<DirectoryInfo>())
        return;
      directoryInfo1.Delete(true);
    }
  }
}
