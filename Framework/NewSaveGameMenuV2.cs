// Decompiled with JetBrains decompiler
// Type: Omegasis.SaveAnywhere.Framework.NewSaveGameMenuV2
// Assembly: SaveAnywhere, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: CA1E3B07-AC71-4821-90DC-80822753C1D9
// Assembly location: C:\Users\keren\Desktop\SaveAnywhere1.5\SaveAnywhere.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using xTile.Dimensions;

namespace Omegasis.SaveAnywhere.Framework
{
  public class NewSaveGameMenuV2 : IClickableMenu
  {
    private int completePause = -1;
    private int margin = 500;
    private StringBuilder _stringBuilder = new StringBuilder();
    private float _ellipsisDelay = 0.5f;
    private Stopwatch stopwatch;
    private IEnumerator<int> loader;
    public bool quit;
    public bool hasDrawn;
    private SparklingText saveText;
    private int _ellipsisCount;
    protected bool _hasSentFarmhandData;
    public Multiplayer multiplayer;

    public event EventHandler SaveComplete;

    public NewSaveGameMenuV2()
    {
      this.saveText = new SparklingText(Game1.dialogueFont, Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGameMenu.cs.11378"), Color.LimeGreen, Color.Black * 0.001F, false, 0.1, 1500, 32, 500, 1f);
      this._hasSentFarmhandData = false;
      this.multiplayer = (Multiplayer) typeof (Game1).GetField(nameof (multiplayer), BindingFlags.Static | BindingFlags.NonPublic).GetValue((object) Program.gamePtr);
    }

    public virtual void receiveRightClick(int x, int y, bool playSound = true)
    {
    }

    public void complete()
    {
      Game1.playSound("money");
      this.completePause = 1500;
      this.loader = (IEnumerator<int>) null;
      Game1.game1.IsSaving = false;
      if (!Game1.IsMasterGame || Game1.newDaySync == null || Game1.newDaySync.hasSaved())
        return;
      Game1.newDaySync.flagSaved();
      this.SaveComplete((object) this, EventArgs.Empty);
    }

    public virtual bool readyToClose() => false;

    public virtual void update(GameTime time)
    {
      if (this.quit)
      {
        if (!((object) Game1.activeClickableMenu).Equals((object) this) || !Game1.PollForEndOfNewDaySync())
          return;
        Game1.exitActiveMenu();
      }
      else
      {
        base.update(time);
        if (Game1.client != null && Game1.client.timedOut)
        {
          this.quit = true;
          if (!((object) Game1.activeClickableMenu).Equals((object) this))
            return;
          Game1.exitActiveMenu();
        }
        else
        {
          this._ellipsisDelay -= (float) time.ElapsedGameTime.TotalSeconds;
          if ((double) this._ellipsisDelay <= 0.0)
          {
            this._ellipsisDelay += 0.75f;
            ++this._ellipsisCount;
            if (this._ellipsisCount > 3)
              this._ellipsisCount = 1;
          }
          if (this.loader != null)
          {
            this.loader.MoveNext();
            if (this.loader.Current >= 100)
            {
              this.margin -= time.ElapsedGameTime.Milliseconds;
              if (this.margin <= 0)
                this.complete();
            }
          }
          else if (this.hasDrawn && this.completePause == -1)
          {
            if (Game1.IsMasterGame)
            {
              if (Game1.saveOnNewDay)
              {
                Game1.player.team.endOfNightStatus.UpdateState("ready");
                if (Game1.newDaySync != null)
                {
                  if (Game1.newDaySync.readyForSave())
                  {
                    this.multiplayer.saveFarmhands();
                    Game1.game1.IsSaving = true;
                    this.loader = SaveGame.Save();
                  }
                }
                else
                {
                  this.multiplayer.saveFarmhands();
                  Game1.game1.IsSaving = true;
                  this.loader = SaveGame.Save();
                }
              }
              else
              {
                this.margin = -1;
                if (Game1.newDaySync != null)
                {
                  if (Game1.newDaySync.readyForSave())
                  {
                    Game1.game1.IsSaving = true;
                    this.complete();
                  }
                }
                else
                  this.complete();
              }
            }
            else
            {
              if (!this._hasSentFarmhandData)
              {
                this._hasSentFarmhandData = true;
                this.multiplayer.sendFarmhand();
              }
              this.multiplayer.UpdateLate(false);
              this.multiplayer.UpdateEarly();
              if (Game1.newDaySync != null)
                Game1.newDaySync.readyForSave();
              Game1.player.team.endOfNightStatus.UpdateState("ready");
              if (Game1.newDaySync != null)
              {
                if (Game1.newDaySync.hasSaved())
                  this.complete();
              }
              else
                this.complete();
            }
          }
          if (this.completePause < 0)
            return;
          this.completePause -= time.ElapsedGameTime.Milliseconds;
          this.saveText.update(time);
          if (this.completePause >= 0)
            return;
          this.quit = true;
          this.completePause = -9999;
        }
      }
    }

    public static void saveClientOptions()
    {
      StartupPreferences startupPreferences = new StartupPreferences();
      startupPreferences.loadPreferences(false, true);
      startupPreferences.clientOptions = Game1.options;
      startupPreferences.savePreferences(false);
    }

    public virtual void draw(SpriteBatch b)
    {
      base.draw(b);
      Vector2 vector2 = Utility.makeSafe(new Vector2(64f, (float)(Game1.viewport.Height - 64)), new Vector2(64f, 64f));
      bool flag = false;
      if (this.completePause >= 0)
      {
        if (Game1.saveOnNewDay)
          this.saveText.draw(b, vector2);
      }
      else if (this.margin < 0 || Game1.IsClient)
      {
        if (Game1.IsMultiplayer)
        {
          this._stringBuilder.Clear();
          this._stringBuilder.Append(Game1.content.LoadString("Strings\\UI:ReadyCheck", (object) Game1.newDaySync.numReadyForSave(), (object) ((IEnumerable<Farmer>) Game1.getOnlineFarmers()).Count<Farmer>()));
          for (int index = 0; index < this._ellipsisCount; ++index)
            this._stringBuilder.Append(".");
          b.DrawString(Game1.dialogueFont, this._stringBuilder, vector2, Color.White);
          flag = true;
        }
      }
      else if (!Game1.IsMultiplayer)
      {
        this._stringBuilder.Clear();
        this._stringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGameMenu.cs.11381"));
        for (int index = 0; index < this._ellipsisCount; ++index)
          this._stringBuilder.Append(".");
        b.DrawString(Game1.dialogueFont, this._stringBuilder, vector2, Color.White);
      }
      else
      {
        this._stringBuilder.Clear();
        this._stringBuilder.Append(Game1.content.LoadString("Strings\\UI:ReadyCheck", (object) Game1.newDaySync.numReadyForSave(), (object) ((IEnumerable<Farmer>) Game1.getOnlineFarmers()).Count<Farmer>()));
        for (int index = 0; index < this._ellipsisCount; ++index)
          this._stringBuilder.Append(".");
        b.DrawString(Game1.dialogueFont, this._stringBuilder, vector2, Color.White);
        flag = true;
      }
      if (this.completePause > 0)
        flag = false;
      if (Game1.newDaySync != null && Game1.newDaySync.hasSaved())
        flag = false;
      if (Game1.IsMultiplayer & flag && Game1.options.showMPEndOfNightReadyStatus)
        Game1.player.team.endOfNightStatus.Draw(b, vector2 + new Vector2(0.0f, -32f), 4f, 0.99f, (PlayerStatusList.HorizontalAlignment) 0, (PlayerStatusList.VerticalAlignment) 1);
      this.hasDrawn = true;
    }

    public void Dispose() => Game1.game1.IsSaving = false;
  }
}
